using MoodzApi.Models;
using Microsoft.AspNetCore.Mvc;
using MoodzApi.Services;
using MoodzApi.Mappers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using MongoDB.Bson;

namespace MoodzApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly UsersService _usersService;
    private readonly UserMapper _userMapper;
    private readonly IUserContext _userContext;
    private const string EMAIL_RE = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z";
    public UsersController(UsersService usersService, IUserContext userContext)
    {
        _usersService = usersService;
        _userMapper = new UserMapper(); // Note: should try to update DI Injection Container to all Mappers to be singletons, but this is fine for now
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<List<User>> Get() =>
        await _usersService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<OtherUserState>> Get(string id)
    {
        var user = await _usersService.GetAsync(id);

        if (user is null) return NotFound();

        bool isPrivate = user?.Settings?.IsPrivate ?? false;    // Default to false if not initialized
        if (isPrivate) return Ok(_userMapper.UserToPrivateUserState(user!));

        return Ok(_userMapper.UserToPublicUserState(user!));
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
    [Authorize]
    public async Task<IActionResult> Delete(string id) {
        var user = await _usersService.GetAsync(id);

        if (user is null) return NotFound();

        var result = await _usersService.RemoveAsync(id);
        if (result is false) return NoContent();

        return Ok();
    }

    [HttpGet("entries")]
    [Authorize]
    public async Task<ActionResult<List<Entry>>> GetEntries()
    {
        var id = _userContext.UserId;

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

    [HttpPost("entries")]
    [Authorize]
    public async Task<ActionResult> AddEntry(Entry newEntry)
    {
        var id = _userContext.UserId;

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

    [HttpDelete("entries/{date}")]
    [Authorize]
    public async Task<ActionResult> DeleteEntry(DateTime date)
    {
        var id = _userContext.UserId;

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

    [HttpGet("search")]
    public async Task<ActionResult<List<OtherUserState>>> SearchUsersByName([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 256) return BadRequest("Query must be between 1 and 256 characters.");

        try
        {
            var searchResults = await _usersService.SearchUsersByName(query);

            // Convert List<User> to List<OtherUserState>
            var userStateResults = new List<OtherUserState>();
            foreach (var user in searchResults)
            {
                bool isPrivate = user?.Settings?.IsPrivate ?? false;    // Default to false if not initialized
                if (isPrivate)
                {
                    userStateResults.Add(_userMapper.UserToPrivateUserState(user!));
                } else
                {
                    userStateResults.Add(_userMapper.UserToPublicUserState(user!));
                }
            }

            return Ok(userStateResults);
        } catch (Exception ex)
        {
            return StatusCode(500, (ex.Message));
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateUserProfile([FromBody] ProfileInfo profileInfo)
    {
        var id = _userContext.UserId;

        // Call the service method to update profile info
        var success = await _usersService.UpdateProfileInfoAsync(id, profileInfo);

        if (success)
        {
            return Ok();
        }

        // If the user or entry was not found, return 404 Not Found
        return NotFound();
    }

    [HttpPut("settings")]
    [Authorize]
    public async Task<IActionResult> UpdateUserSettings([FromBody] Settings settings)
    {
        var id = _userContext.UserId;

        // Call the service method to update profile info
        var success = await _usersService.UpdateSettingsAsync(id, settings);

        if (success)
        {
            return Ok();
        }

        // If the user or entry was not found, return 404 Not Found
        return NotFound();
    }

    [HttpGet("refresh")]
    [Authorize]
    public async Task<IActionResult> GetUserState()
    {
        var id = _userContext.UserId;

        // Call the service method to update profile info
        var user = await _usersService.GetAsync(id);

        // If the user or entry was not found, return 404 Not Found
        if (user == null) {
            return NotFound();
        }

        // Condense user info to user state to send to frontend
        UserState userState = _userMapper.UserToUserState(user);

        return Ok(userState);
    }

    [HttpPut("follow/{toUserId:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Follow(string toUserId)
    {
        var id = _userContext.UserId;

        // Call the service method to send a follow
        var status = await _usersService.Follow(new ObjectId(id), new ObjectId(toUserId));

        // If successful, return ok
        if (status)
        {
            return Ok();
        }

        // Else return error
        return StatusCode(500);
    }

    [HttpPut("unfollow/{toUserId:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Unfollow(string toUserId)
    {
        var id = _userContext.UserId;

        try
        {
            // Call the service method to send a follow
            var status = await _usersService.Unfollow(new ObjectId(id), new ObjectId(toUserId));

            // If successful, return ok
            if (status) return Ok();
        }
        catch (Exception e)
        {
            return StatusCode(500, e);
        }

        // Else return error
        return StatusCode(500);
    }

    [HttpPut("accept/{otherId:length(24)}")]
    [Authorize]
    public async Task<IActionResult> AcceptFollowRequest(string otherId)
    {
        var id = _userContext.UserId;

        // Call the service method to accept a follow request
        var status = await _usersService.AcceptFollowRequest(new ObjectId(otherId), new ObjectId(id));

        // If successful, return ok
        if (status)
        {
            return Ok();
        }

        // Else return error
        return StatusCode(500);
    }

    [HttpPut("decline/{otherId:length(24)}")]
    [Authorize]
    public async Task<IActionResult> DeclineFollowRequest(string otherId)
    {
        var id = _userContext.UserId;

        // Call the service method to remove a follow request
        // Swap id and otherId since we are declining a request, the matched follow request fields should be flipped with the id's
        // fromUserId should be otherId, toUserId should be from the owner of the account
        var status = await _usersService.RemoveFollowRequest(new ObjectId(otherId), new ObjectId(id));

        // If successful, return ok
        if (status)
        {
            return Ok();
        }

        // Else return error
        return StatusCode(500);
    }

    [HttpPut("unrequest/{otherId:length(24)}")]
    [Authorize]
    public async Task<IActionResult> Unrequest( string otherId)
    {
        var id = _userContext.UserId;

        // Call the service method to remove a follow request
        var status = await _usersService.RemoveFollowRequest(new ObjectId(id), new ObjectId(otherId));

        // If successful, return ok
        if (status)
        {
            return Ok();
        }

        // Else return error
        return StatusCode(500);
    }
}
