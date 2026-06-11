using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartEnergyCity.Api.Data;
using SmartEnergyCity.Api.DTOs;
using SmartEnergyCity.Api.Services;

namespace SmartEnergyCity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class MapController(GameDbContext dbContext, ITwoGisService twoGisService, IGameService gameService) : ControllerBase
{
    [HttpGet("districts")]
    public async Task<ActionResult<IReadOnlyCollection<DistrictDto>>> GetDistricts(CancellationToken cancellationToken)
    {
        var districts = await twoGisService.GetDistrictsAsync(cancellationToken);
        if (districts.Count > 0)
        {
            return Ok(districts);
        }

        var persisted = await dbContext.Districts.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        return Ok(persisted.Select(x => new DistrictDto(
            x.Id,
            x.Name,
            x.NameRu,
            x.NameKz,
            x.CenterLat,
            x.CenterLng,
            JsonSerializer.Deserialize<List<CoordinateDto>>(x.Boundary) ?? []
        )).ToArray());
    }

    [HttpGet("buildings")]
    public async Task<ActionResult<IReadOnlyCollection<BuildingDto>>> GetBuildings(CancellationToken cancellationToken)
        => Ok(await gameService.GetBuildingsAsync(cancellationToken));
}
