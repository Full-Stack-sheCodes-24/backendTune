﻿namespace MoodzApi.Models;
public class UserState
{
    public string? Id { get; set; }
    public string Name { get; set; } = null!;
    public Entry[] Entries { get; set; } = null!;
}