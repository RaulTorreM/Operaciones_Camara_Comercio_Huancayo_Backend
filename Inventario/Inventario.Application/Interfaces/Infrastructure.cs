using Inventario.Core.Entities;

namespace Inventario.Application.Interfaces;

/// <summary>Usuario que realiza la petición (para la bitácora). Lo implementa la capa API.</summary>
public interface ICurrentUser
{
    string Name { get; }
}

public interface IJwtTokenService
{
    string CreateToken(User user);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IFileStorage
{
    /// <summary>Guarda una imagen enviada como data URL y devuelve su URL pública.</summary>
    Task<string> SaveImageAsync(string dataUrl);
    Task DeleteAsync(string? url);
}
