using EventCrew.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EventCrew.Api.Services;

public sealed class PasswordService(IPasswordHasher<User> passwordHasher) : IPasswordService
{
    public string HashPassword(User user, string password) => passwordHasher.HashPassword(user, password);

    public bool VerifyPassword(User user, string password) =>
        !string.IsNullOrWhiteSpace(user.PasswordHash) &&
        passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) is
            PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
}
