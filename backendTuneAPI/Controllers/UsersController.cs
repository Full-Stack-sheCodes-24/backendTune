using MoodzApi.Models;
using Microsoft.AspNetCore.Mvc;
using MoodzApi.Services;
using MoodzApi.Mappers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;

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
    
    [HttpGet("{id:length(24)}/entries")]
    public async Task<ActionResult<List<Entry>>> GetEntries(string id)
    {
        // Fetch all entries for the specified userId
        var entries = await _usersService.GetEntriesByUserIdAsync(id);

        // If the user has no entries, you can choose to return a 404 or an empty list with 200 OK
        if (entries == null)
        {
            return NotFound();
        }

        // Return the list of entries with a 200 OK status
        return Ok(entries);
    }

    [HttpPost("{id:length(24)}/entries")]
    public async Task<ActionResult> AddEntry(string id, Entry newEntry)
    {
        // Call the service method to add the entry
        var success = await _usersService.AddEntryToUserAsync(id, newEntry);

        // If the entry was added successfully, return 201 Created
        if (success)
        {
            return CreatedAtAction(nameof(GetEntries), new { id = id }, newEntry);
        }

        // If the user was not found or the entry was not added, return 404 Not Found
        return NotFound();
    }

    [HttpDelete("{id:length(24)}/entries/{date}")]
    public async Task<ActionResult> DeleteEntry(string id, DateTime date)
    {
        // Call the service method to delete the entry
        var success = await _usersService.DeleteEntryByDateAsync(id, date);

        // Return 204 No Content if the entry was deleted
        if (success)
        {
            return NoContent();
        }

        // If the user or entry was not found, return 404 Not Found
        return NotFound();
    }
}
