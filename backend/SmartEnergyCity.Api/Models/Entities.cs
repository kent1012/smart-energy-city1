namespace SmartEnergyCity.Api.Models;

public enum GeneratorType
{
    Coal,
    Gas,
    Solar,
    Wind
}

public enum BuildingType
{
    House,
    Apartment,
    Infrastructure
}

public sealed class GameState
{
    public int Id { get; set; } = 1;
    public int Stage { get; set; } = 1;
    public decimal Currency { get; set; } = 10000;
    public double TotalCo2PerHour { get; set; }
    public double EcologyPercent { get; set; } = 100;
    public int ConnectedBuildings { get; set; }
    public int Score { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class District
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameRu { get; set; } = string.Empty;
    public string NameKz { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public string Boundary { get; set; } = string.Empty;
    public List<Building> Buildings { get; set; } = [];
}

public sealed class Building
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BuildingType Type { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double PowerDemandKw { get; set; }
    public bool IsConnected { get; set; }
    public bool IsEcoPowered { get; set; }

    public int DistrictId { get; set; }
    public District District { get; set; } = default!;
}

public sealed class Generator
{
    public int Id { get; set; }
    public GeneratorType Type { get; set; }
    public double CapacityKw { get; set; }
    public double Co2PerHour { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Cost { get; set; }

    public int DistrictId { get; set; }
    public District District { get; set; } = default!;
}

public sealed class Substation
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CapacityKw { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public decimal Cost { get; set; }

    public int DistrictId { get; set; }
    public District District { get; set; } = default!;
}

public sealed class PowerLine
{
    public int Id { get; set; }
    public string FromNode { get; set; } = string.Empty;
    public string ToNode { get; set; } = string.Empty;
    public double LengthKm { get; set; }
    public double CapacityKw { get; set; }
    public decimal Cost { get; set; }
}
