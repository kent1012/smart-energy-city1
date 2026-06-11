using SmartEnergyCity.Api.Models;

namespace SmartEnergyCity.Api.DTOs;

public sealed record CoordinateDto(double Lat, double Lng);

public sealed record DistrictDto(
    int Id,
    string Name,
    string NameRu,
    string NameKz,
    double CenterLat,
    double CenterLng,
    IReadOnlyCollection<CoordinateDto> Boundary);

public sealed record BuildingDto(
    int Id,
    string Name,
    string Type,
    int DistrictId,
    double Latitude,
    double Longitude,
    double PowerDemandKw,
    bool IsConnected,
    bool IsEcoPowered);

public sealed record GeneratorDto(
    int Id,
    string Type,
    int DistrictId,
    double Latitude,
    double Longitude,
    double CapacityKw,
    double Co2PerHour,
    decimal Cost);

public sealed record DashboardDto(
    int Stage,
    decimal Currency,
    int ConnectedBuildings,
    double TotalDemandKw,
    double TotalSupplyKw,
    double TotalCo2PerHour,
    double EcologyPercent,
    int Score,
    int DistrictsOnline,
    int TotalBuildings);

public sealed record BuildGeneratorRequest(int DistrictId, GeneratorType Type, double Latitude, double Longitude);
public sealed record BuildSubstationRequest(int DistrictId, string Name, double Latitude, double Longitude);
public sealed record BuildPowerLineRequest(string FromNode, string ToNode, double LengthKm, double CapacityKw);

public sealed record EducationCardDto(
    GeneratorType Type,
    string TitleRu,
    string TitleKz,
    string DescriptionRu,
    string DescriptionKz,
    string Color,
    int Co2PerHour);
