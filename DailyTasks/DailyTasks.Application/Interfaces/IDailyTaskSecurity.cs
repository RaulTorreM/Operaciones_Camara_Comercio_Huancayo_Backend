using DailyTasks.Core.Entities;

namespace DailyTasks.Application.Interfaces;

public interface IDailyTaskJwtTokenService
{
    string CreateToken(DailyTaskUser user);
}

public interface IDailyTaskPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
