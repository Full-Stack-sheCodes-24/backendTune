﻿using MoodzApi.Models;

namespace MoodzApi.Mappers;
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

    public UserState UserToUserState(User user)
    {
        return new UserState()
        {
            Id = user.Id!,
            Name = user.Name,
            ProfilePicUrl = user.ProfilePicUrl,
            Entries = user.Entries
        };
    }
}