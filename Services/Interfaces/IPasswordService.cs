﻿namespace API.Services.Interfaces;

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateRandomPassword(int length = 12);
    bool IsPasswordStrong(string password);
}