using System.Security.Claims;

namespace MoodzApi.Services;
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext!.User;
            return user!.FindFirstValue(ClaimTypes.NameIdentifier)!;
        }
    }
}