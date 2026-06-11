using Microsoft.EntityFrameworkCore;
using SmartEnergyCity.Api.Data;
using SmartEnergyCity.Api.DTOs;
using SmartEnergyCity.Api.Models;

namespace SmartEnergyCity.Api.Services;

public interface IGameService
{
    Task SeedBuildingsAsync(CancellationToken cancellationToken = default);
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BuildingDto>> GetBuildingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GeneratorDto>> GetGeneratorsAsync(CancellationToken cancellationToken = default);
    Task<DashboardDto> BuildGeneratorAsync(BuildGeneratorRequest request, CancellationToken cancellationToken = default);
    Task<DashboardDto> BuildSubstationAsync(BuildSubstationRequest request, CancellationToken cancellationToken = default);
    Task<DashboardDto> BuildPowerLineAsync(BuildPowerLineRequest request, CancellationToken cancellationToken = default);
    Task<DashboardDto> ConnectBuildingAsync(int buildingId, CancellationToken cancellationToken = default);
    Task<DashboardDto> ResetAsync(CancellationToken cancellationToken = default);
    IReadOnlyCollection<EducationCardDto> GetEducationCards();
}

public sealed class GameService(GameDbContext dbContext, ITwoGisService twoGisService) : IGameService
{
    public async Task SeedBuildingsAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.Buildings.AnyAsync(cancellationToken))
        {
            return;
        }

        var districts = await dbContext.Districts.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        foreach (var district in districts)
        {
            var source = await twoGisService.GetBuildingsAsync(district.Name, district.Id, cancellationToken);
            foreach (var item in source)
            {
                dbContext.Buildings.Add(new Building
                {
                    ExternalId = item.ExternalId,
                    Name = item.Name,
                    DistrictId = district.Id,
                    Latitude = item.Lat,
                    Longitude = item.Lng,
                    Type = item.Type switch
                    {
                        "apartment" => BuildingType.Apartment,
                        "infrastructure" => BuildingType.Infrastructure,
                        _ => BuildingType.House
                    },
                    PowerDemandKw = item.Demand,
                    IsConnected = false,
                    IsEcoPowered = false
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateAsync(cancellationToken);
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        await SeedBuildingsAsync(cancellationToken);
        return await RecalculateAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BuildingDto>> GetBuildingsAsync(CancellationToken cancellationToken = default)
    {
        await SeedBuildingsAsync(cancellationToken);
        return await dbContext.Buildings
            .OrderBy(x => x.Id)
            .Select(x => new BuildingDto(
                x.Id,
                x.Name,
                x.Type.ToString().ToLowerInvariant(),
                x.DistrictId,
                x.Latitude,
                x.Longitude,
                x.PowerDemandKw,
                x.IsConnected,
                x.IsEcoPowered))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<GeneratorDto>> GetGeneratorsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Generators
            .OrderBy(x => x.Id)
            .Select(x => new GeneratorDto(
                x.Id,
                x.Type.ToString().ToLowerInvariant(),
                x.DistrictId,
                x.Latitude,
                x.Longitude,
                x.CapacityKw,
                x.Co2PerHour,
                x.Cost))
            .ToListAsync(cancellationToken);

    public async Task<DashboardDto> BuildGeneratorAsync(BuildGeneratorRequest request, CancellationToken cancellationToken = default)
    {
        var state = await GetStateAsync(cancellationToken);
        var spec = request.Type switch
        {
            GeneratorType.Coal => (capacity: 300d, cost: 2000m, co2: 100d),
            GeneratorType.Gas => (capacity: 230d, cost: 3500m, co2: 50d),
            GeneratorType.Solar => (capacity: 160d, cost: 5000m, co2: 0d),
            GeneratorType.Wind => (capacity: 190d, cost: 4500m, co2: 0d),
            _ => throw new ArgumentOutOfRangeException()
        };

        if (state.Currency < spec.cost)
        {
            throw new InvalidOperationException("Not enough currency");
        }

        dbContext.Generators.Add(new Generator
        {
            DistrictId = request.DistrictId,
            Type = request.Type,
            CapacityKw = spec.capacity,
            Co2PerHour = spec.co2,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Cost = spec.cost
        });

        state.Currency -= spec.cost;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await RecalculateAsync(cancellationToken);
    }

    public async Task<DashboardDto> BuildSubstationAsync(BuildSubstationRequest request, CancellationToken cancellationToken = default)
    {
        const decimal cost = 1500m;
        var state = await GetStateAsync(cancellationToken);
        if (state.Currency < cost)
        {
            throw new InvalidOperationException("Not enough currency");
        }

        dbContext.Substations.Add(new Substation
        {
            DistrictId = request.DistrictId,
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Substation" : request.Name,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Cost = cost,
            CapacityKw = 500
        });

        state.Currency -= cost;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await RecalculateAsync(cancellationToken);
    }

    public async Task<DashboardDto> BuildPowerLineAsync(BuildPowerLineRequest request, CancellationToken cancellationToken = default)
    {
        var cost = Math.Round((decimal)request.LengthKm * 500m, 2);
        var state = await GetStateAsync(cancellationToken);
        if (state.Currency < cost)
        {
            throw new InvalidOperationException("Not enough currency");
        }

        dbContext.PowerLines.Add(new PowerLine
        {
            FromNode = request.FromNode,
            ToNode = request.ToNode,
            LengthKm = request.LengthKm,
            CapacityKw = request.CapacityKw,
            Cost = cost
        });

        state.Currency -= cost;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await RecalculateAsync(cancellationToken);
    }

    public async Task<DashboardDto> ConnectBuildingAsync(int buildingId, CancellationToken cancellationToken = default)
    {
        await SeedBuildingsAsync(cancellationToken);

        var building = await dbContext.Buildings.FirstOrDefaultAsync(x => x.Id == buildingId, cancellationToken)
            ?? throw new KeyNotFoundException("Building not found");

        if (building.IsConnected)
        {
            return await RecalculateAsync(cancellationToken);
        }

        var availableSupply = await dbContext.Generators.SumAsync(x => x.CapacityKw, cancellationToken);
        var usedDemand = await dbContext.Buildings.Where(x => x.IsConnected).SumAsync(x => x.PowerDemandKw, cancellationToken);
        if (availableSupply < usedDemand + building.PowerDemandKw)
        {
            throw new InvalidOperationException("Not enough generation capacity");
        }

        var greenSupply = await dbContext.Generators.Where(x => x.Type == GeneratorType.Solar || x.Type == GeneratorType.Wind)
            .SumAsync(x => x.CapacityKw, cancellationToken);

        building.IsConnected = true;
        building.IsEcoPowered = greenSupply >= usedDemand + building.PowerDemandKw;
        await dbContext.SaveChangesAsync(cancellationToken);
        return await RecalculateAsync(cancellationToken);
    }

    public async Task<DashboardDto> ResetAsync(CancellationToken cancellationToken = default)
    {
        dbContext.Buildings.RemoveRange(dbContext.Buildings);
        dbContext.Generators.RemoveRange(dbContext.Generators);
        dbContext.Substations.RemoveRange(dbContext.Substations);
        dbContext.PowerLines.RemoveRange(dbContext.PowerLines);

        var state = await GetStateAsync(cancellationToken);
        state.Stage = 1;
        state.Currency = 10000;
        state.TotalCo2PerHour = 0;
        state.EcologyPercent = 100;
        state.ConnectedBuildings = 0;
        state.Score = 0;
        state.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SeedBuildingsAsync(cancellationToken);
        return await RecalculateAsync(cancellationToken);
    }

    public IReadOnlyCollection<EducationCardDto> GetEducationCards() =>
    [
        new(GeneratorType.Coal, "Угольная станция", "Көмір станциясы", "Дешевая энергия, но высокий уровень CO2.", "Энергия арзан, бірақ CO2 шығарындылары жоғары.", "#8b4513", 100),
        new(GeneratorType.Gas, "Газовая станция", "Газ станциясы", "Переходная технология: меньше выбросов, чем уголь.", "Өтпелі технология: көмірге қарағанда шығарындылар аз.", "#4169e1", 50),
        new(GeneratorType.Solar, "Солнечная станция", "Күн станциясы", "Чистая энергия без выбросов CO2.", "CO2 шығармайтын таза энергия.", "#ffd700", 0),
        new(GeneratorType.Wind, "Ветровая станция", "Жел станциясы", "Возобновляемый источник с нулевыми выбросами.", "Нөлдік шығарындылары бар жаңартылатын қуат көзі.", "#87ceeb", 0)
    ];

    private async Task<GameState> GetStateAsync(CancellationToken cancellationToken)
    {
        var state = await dbContext.GameStates.FirstOrDefaultAsync(x => x.Id == 1, cancellationToken);
        if (state is not null)
        {
            return state;
        }

        state = new GameState();
        dbContext.GameStates.Add(state);
        await dbContext.SaveChangesAsync(cancellationToken);
        return state;
    }

    private async Task<DashboardDto> RecalculateAsync(CancellationToken cancellationToken)
    {
        var state = await GetStateAsync(cancellationToken);

        var connectedBuildings = await dbContext.Buildings.CountAsync(x => x.IsConnected, cancellationToken);
        var totalBuildings = await dbContext.Buildings.CountAsync(cancellationToken);
        var totalDemand = await dbContext.Buildings.Where(x => x.IsConnected).SumAsync(x => x.PowerDemandKw, cancellationToken);
        var totalSupply = await dbContext.Generators.SumAsync(x => x.CapacityKw, cancellationToken);
        var totalCo2 = await dbContext.Generators.SumAsync(x => x.Co2PerHour, cancellationToken);
        var greenSupply = await dbContext.Generators
            .Where(x => x.Type == GeneratorType.Solar || x.Type == GeneratorType.Wind)
            .SumAsync(x => x.CapacityKw, cancellationToken);

        var ecology = totalSupply <= 0
            ? 100
            : Math.Clamp((greenSupply / totalSupply) * 100 - (totalCo2 / 10), 0, 100);

        var districtsOnline = await dbContext.Districts
            .CountAsync(d => d.Buildings.Any(b => b.IsConnected), cancellationToken);

        var score = await dbContext.Buildings
            .Where(x => x.IsConnected)
            .SumAsync(x => x.Type == BuildingType.Apartment ? 50 : x.Type == BuildingType.House ? 10 : 25, cancellationToken);

        if (ecology >= 100 && connectedBuildings > 0)
        {
            score += 200;
        }

        state.ConnectedBuildings = connectedBuildings;
        state.TotalCo2PerHour = totalCo2;
        state.EcologyPercent = ecology;
        state.Score = score;
        state.Stage = connectedBuildings switch
        {
            <= 5 => 1,
            <= 20 => 2,
            <= 100 => 3,
            <= 220 => 4,
            _ => 5
        };
        state.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new DashboardDto(
            state.Stage,
            state.Currency,
            state.ConnectedBuildings,
            totalDemand,
            totalSupply,
            state.TotalCo2PerHour,
            state.EcologyPercent,
            state.Score,
            districtsOnline,
            totalBuildings);
    }
}
