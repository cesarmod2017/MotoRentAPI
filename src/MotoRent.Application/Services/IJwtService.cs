﻿namespace MotoRent.Application.Services
{
    public interface IJwtService
    {
        string GenerateToken(string userId, string role);
    }
}