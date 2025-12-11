using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Interfaces.Services;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevController : ControllerBase
{
    private readonly IDevService _devService;

    public DevController(IDevService devService)
    {
        _devService = devService;
    }

    [HttpPost("seed-permissions")]
    public async Task<IActionResult> SeedPermissions()
    {
        try
        {
            await _devService.SeedPermissionsAsync();
            return Ok(new { message = "Permissions seeded successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }

    [HttpGet("debug-admin")]
    public async Task<IActionResult> DebugAdmin()
    {
        try
        {
            var result = await _devService.GetDebugAdminInfoAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, details = ex.ToString() });
        }
    }
}