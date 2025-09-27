using NabdCare.Domain.Entities.Users;

namespace NabdCare.Application.Interfaces;

public interface IPasswordService
{
    string HashPassword(User user,string password);
    bool VerifyPassword(string password, string hashed);
}