using Inventario.Core.Entities;
using Inventario.Core.Interfaces;
using Inventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventario.Infrastructure.Repositories;

public class UserRepository(InventarioDbContext db) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<bool> AnyAsync() => db.Users.AnyAsync();

    public async Task AddAsync(User user) => await db.Users.AddAsync(user);
}
