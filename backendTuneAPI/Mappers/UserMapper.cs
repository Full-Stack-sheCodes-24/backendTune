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
            BioText = user.BioText,
            Birthday = user.Birthday,
            Entries = user.Entries,
            Settings = user.Settings
        };
    }
    public PublicUserState UserToPublicUserState(User user)
    {
        return new PublicUserState()
        {
            Id = user.Id!,
            Name = user.Name,
            ProfilePicUrl = user.ProfilePicUrl,
            BioText = user.BioText,
            Birthday = user.Birthday,
            Entries = user.Entries
        };
    }

    public PrivateUserState UserToPrivateUserState(User user)
    {
        return new PrivateUserState()
        {
            Id = user.Id!,
            Name = user.Name,
            ProfilePicUrl = user.ProfilePicUrl
        };
    }
}