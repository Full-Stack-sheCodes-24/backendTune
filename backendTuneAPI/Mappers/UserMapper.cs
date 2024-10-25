using MoodzApi.Models;

namespace MoodzApi.Mappers
{
    public class UserMapper
    {
        public User UserCreateRequestToUser(UserCreateRequest request)
        {
            return new User()
            {
                Id = null,
                Email = request.Email,
                Name = request.Name
            };
        }
    }
}
