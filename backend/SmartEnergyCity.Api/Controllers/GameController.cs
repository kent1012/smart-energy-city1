using Microsoft.AspNetCore.Mvc;
using SmartEnergyCity.Api.DTOs;
using SmartEnergyCity.Api.Services;

namespace SmartEnergyCity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GameController(IGameService gameService) : ControllerBase
{
    [HttpGet("state")]
    public async Task<ActionResult<DashboardDto>> GetState(CancellationToken cancellationToken)
        => Ok(await gameService.GetDashboardAsync(cancellationToken));

    [HttpGet("buildings")]
    public async Task<ActionResult<IReadOnlyCollection<BuildingDto>>> GetBuildings(CancellationToken cancellationToken)
        => Ok(await gameService.GetBuildingsAsync(cancellationToken));

    [HttpGet("generators")]
    public async Task<ActionResult<IReadOnlyCollection<GeneratorDto>>> GetGenerators(CancellationToken cancellationToken)
        => Ok(await gameService.GetGeneratorsAsync(cancellationToken));

    [HttpPost("generator")]
    public async Task<ActionResult<DashboardDto>> BuildGenerator([FromBody] BuildGeneratorRequest request, CancellationToken cancellationToken)
        => await Execute(() => gameService.BuildGeneratorAsync(request, cancellationToken));

    [HttpPost("substation")]
    public async Task<ActionResult<DashboardDto>> BuildSubstation([FromBody] BuildSubstationRequest request, CancellationToken cancellationToken)
        => await Execute(() => gameService.BuildSubstationAsync(request, cancellationToken));

    [HttpPost("power-lines")]
    public async Task<ActionResult<DashboardDto>> BuildPowerLine([FromBody] BuildPowerLineRequest request, CancellationToken cancellationToken)
        => await Execute(() => gameService.BuildPowerLineAsync(request, cancellationToken));

    [HttpPost("buildings/{buildingId:int}/connect")]
    public async Task<ActionResult<DashboardDto>> ConnectBuilding(int buildingId, CancellationToken cancellationToken)
        => await Execute(() => gameService.ConnectBuildingAsync(buildingId, cancellationToken));

    [HttpPost("reset")]
    public async Task<ActionResult<DashboardDto>> Reset(CancellationToken cancellationToken)
        => Ok(await gameService.ResetAsync(cancellationToken));

    [HttpGet("education-cards")]
    public ActionResult<IReadOnlyCollection<EducationCardDto>> GetCards()
        => Ok(gameService.GetEducationCards());

    private async Task<ActionResult<DashboardDto>> Execute(Func<Task<DashboardDto>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
