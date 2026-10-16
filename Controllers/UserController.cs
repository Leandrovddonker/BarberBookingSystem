using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using planeProject.Data;
using planeProject.Common;
using planeProject.Models;
using planeProject.Models.Dtos;

namespace planeProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserByIdAsync(int id)
    {
        var user = await userService.GetUserByIdAsync(id);
        return Ok(user);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllUsersAsync();
        return Ok(users);
    }
    

   
}