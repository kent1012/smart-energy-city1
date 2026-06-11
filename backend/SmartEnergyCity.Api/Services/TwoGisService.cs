using System.Text.Json;
using SmartEnergyCity.Api.DTOs;

namespace SmartEnergyCity.Api.Services;

public interface ITwoGisService
{
    Task<IReadOnlyCollection<DistrictDto>> GetDistrictsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<(string ExternalId, string Name, string Type, double Lat, double Lng, double Demand)>>
        GetBuildingsAsync(string districtName, int districtId, CancellationToken cancellationToken = default);
}

public sealed class TwoGisService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TwoGisService> logger) : ITwoGisService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyCollection<DistrictDto> FallbackDistricts =
    [
        new(1, "esil", "Есиль", "Есіл", 51.1694, 71.4491, [new(51.175, 71.436), new(51.179, 71.46), new(51.163, 71.467), new(51.158, 71.438)]),
        new(2, "akmol", "Акмол", "Ақмол", 51.1400, 71.5200, [new(51.147, 71.505), new(51.149, 71.535), new(51.134, 71.536), new(51.132, 71.508)]),
        new(3, "saryarka", "Сарыарка", "Сарыарқа", 51.1200, 71.4800, [new(51.127, 71.465), new(51.13, 71.495), new(51.114, 71.498), new(51.109, 71.468)]),
        new(4, "aygarlyn", "Айгарлын", "Айғарлын", 51.1600, 71.3900, [new(51.168, 71.377), new(51.171, 71.404), new(51.152, 71.408), new(51.149, 71.382)]),
        new(5, "almaty", "Алматинский", "Алматы", 51.0900, 71.5500, [new(51.097, 71.536), new(51.101, 71.564), new(51.086, 71.568), new(51.081, 71.541)]),
    ];

    public async Task<IReadOnlyCollection<DistrictDto>> GetDistrictsAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["TwoGis:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return FallbackDistricts;
        }

        try
        {
            var query = "Астана район";
            var requestUri = $"/3.0/items?q={Uri.EscapeDataString(query)}&fields=items.point,items.name&key={apiKey}";
            using var response = await httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("2GIS district call failed with status {StatusCode}", response.StatusCode);
                return FallbackDistricts;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var items = document.RootElement.GetProperty("result").GetProperty("items");
            var extracted = new List<DistrictDto>();
            var mapByName = FallbackDistricts.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var item in items.EnumerateArray())
            {
                var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                if (name is null)
                {
                    continue;
                }

                var matchedDistrict = mapByName.Values.FirstOrDefault(x => name.Contains(x.NameRu, StringComparison.OrdinalIgnoreCase) || name.Contains(x.NameKz, StringComparison.OrdinalIgnoreCase));
                if (matchedDistrict is null)
                {
                    continue;
                }

                extracted.Add(matchedDistrict);
            }

            return extracted.Count > 0 ? extracted : FallbackDistricts;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "2GIS district integration failed. Using fallback district data.");
            return FallbackDistricts;
        }
    }

    public async Task<IReadOnlyCollection<(string ExternalId, string Name, string Type, double Lat, double Lng, double Demand)>>
        GetBuildingsAsync(string districtName, int districtId, CancellationToken cancellationToken = default)
    {
        var apiKey = configuration["TwoGis:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var requestUri = $"/3.0/items?q={Uri.EscapeDataString($"{districtName} Астана ЖК дом школа")}&fields=items.id,items.name,items.point&key={apiKey}";
                using var response = await httpClient.GetAsync(requestUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    var payload = await JsonSerializer.DeserializeAsync<TwoGisResult>(stream, JsonOptions, cancellationToken);
                    if (payload?.Result?.Items is { Count: > 0 })
                    {
                        return payload.Result.Items
                            .Where(x => x.Point is not null)
                            .Select((x, index) => (
                                x.Id ?? $"{districtId}-{index}",
                                x.Name ?? $"Object {index + 1}",
                                index % 5 == 0 ? "apartment" : index % 7 == 0 ? "infrastructure" : "house",
                                x.Point!.Lat,
                                x.Point.Lon,
                                index % 5 == 0 ? 80d : index % 7 == 0 ? 25d : 10d))
                            .Take(120)
                            .ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "2GIS building integration failed for district {District}", districtName);
            }
        }

        var district = FallbackDistricts.First(x => x.Id == districtId);
        return Enumerable.Range(1, 22)
            .Select(i =>
            {
                var latOffset = ((i % 6) - 3) * 0.0022;
                var lngOffset = ((i / 6) - 2) * 0.0027;
                var type = i % 6 == 0 ? "apartment" : i % 8 == 0 ? "infrastructure" : "house";
                var demand = type switch
                {
                    "apartment" => 90d,
                    "infrastructure" => 28d,
                    _ => 12d
                };
                return ($"{districtName}-{i}", $"{district.NameRu} {i}", type, district.CenterLat + latOffset, district.CenterLng + lngOffset, demand);
            })
            .ToArray();
    }

    private sealed class TwoGisResult
    {
        public TwoGisResultBody? Result { get; init; }
    }

    private sealed class TwoGisResultBody
    {
        public List<TwoGisItem> Items { get; init; } = [];
    }

    private sealed class TwoGisItem
    {
        public string? Id { get; init; }
        public string? Name { get; init; }
        public TwoGisPoint? Point { get; init; }
    }

    private sealed class TwoGisPoint
    {
        public double Lat { get; init; }
        public double Lon { get; init; }
    }
}
