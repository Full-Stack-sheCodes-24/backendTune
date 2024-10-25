using MoodzApi.Models;
using Microsoft.AspNetCore.Mvc;
using MoodzApi.Services;
using MoodzApi.Mappers;
using System.Text.RegularExpressions;

namespace MoodzApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UsersService _usersService;
    private readonly UserMapper _userMapper;
    private const string EMAIL_RE = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
    public UsersController(UsersService usersService)
    {
        _usersService = usersService;
        _userMapper = new UserMapper(); // Note: should try to update DI Injection Container to all Mappers to be singletons, but this is fine for now
    }

    [HttpGet]
    public async Task<List<User>> Get() =>
        await _usersService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<User>> Get(string id)
    {
        var user = await _usersService.GetAsync(id);

        if (user is null) return NotFound();

        return user;
    }

    [HttpPost]
    public async Task<IActionResult> Post(UserCreateRequest request)
    {
        if (!Regex.IsMatch(request.Email, EMAIL_RE)) return BadRequest($"{request.Email} is not a valid email");

        var createdUser = await _usersService.CreateAsync(_userMapper.UserCreateRequestToUser(request));
        if (createdUser?.Id is null) return NoContent();

        return CreatedAtAction(nameof(Get), new { id = createdUser.Id }, createdUser);  // Returns a response header with key value pair: "location": "base_url/api/Users/createdUser.Id"
    }

    [HttpPut]
    public async Task<IActionResult> Update(User updatedUser)
    {
        if (updatedUser.Id is null) return BadRequest("Id is null");
        if (updatedUser.Email is not null && !Regex.IsMatch(updatedUser.Email, EMAIL_RE)) return BadRequest($"{updatedUser.Email} is not a valid email");

        var user = await _usersService.GetAsync(updatedUser.Id);
        if (user is null) return NotFound();

        var result = await _usersService.UpdateAsync(updatedUser);
        if (result is false) return NoContent();

        return CreatedAtAction(nameof(Get), new { id = updatedUser.Id }, updatedUser);
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id) {
        var user = await _usersService.GetAsync(id);

        if (user is null) return NotFound();

        var result = await _usersService.RemoveAsync(id);
        if (result is false) return NoContent();

        return Ok();
    }
}
