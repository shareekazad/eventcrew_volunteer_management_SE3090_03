using EventCrew.Domain.Entities;

namespace EventCrew.Api.Services;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string password);
}
