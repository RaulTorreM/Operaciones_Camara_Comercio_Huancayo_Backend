using DailyTasks.Application.Interfaces;

namespace DailyTasks.Infrastructure.Security;

public class DailyTaskBCryptPasswordHasher : IDailyTaskPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
