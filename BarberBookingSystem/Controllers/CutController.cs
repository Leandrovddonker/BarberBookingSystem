using BarberBookingSystem.Common;
using BarberBookingSystem.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using BarberBookingSystem.Data;
using BarberBookingSystem.Models;


namespace BarberBookingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CutController(ICutService hairCutService) : ControllerBase
{

    [HttpGet("all")]
    public async Task<IActionResult> GetAllAsync()
    {
        var haircuts = await hairCutService.GetAllAsync();
        return Ok(haircuts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(int id)
    {
        var haircut = await hairCutService.GetHairCutByIdAsync(id);
        return Ok(haircut);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddHairCutAsync([FromBody] HairCutDto hairCut)
    {
        var addHairCut = await hairCutService.AddHairCutAsync(hairCut);
        return Ok(addHairCut);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditHairCutAsync([FromBody] HairCutDto hairCut, int id)
    {
        var editHaircut = await hairCutService.UpdateHairCutByIdAsync(id ,hairCut);
        return Ok(editHaircut);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHairCutAsync(int id)
    {
        await hairCutService.DeleteHairCutByIdAsync(id);
        return NoContent();
    }
}