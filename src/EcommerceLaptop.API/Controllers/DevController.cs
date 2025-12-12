using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Interfaces.Services;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevController(IDevService devService) : ControllerBase
{
    private readonly IDevService _devService = devService;

    [HttpPost("seed-permissions")]
    public async Task<IActionResult> SeedPermissions()
    {
        await _devService.SeedPermissionsAsync();
        return Ok(new { message = "Permissions seeded successfully" });
    }

    [HttpGet("debug-admin")]
    public async Task<IActionResult> DebugAdmin()
    {
        var result = await _devService.GetDebugAdminInfoAsync();
        return Ok(result);
    }
}