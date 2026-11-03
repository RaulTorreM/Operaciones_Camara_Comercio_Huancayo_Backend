namespace Inventario.Application.DTOs.Auth;

public record LoginRequest(string? Username, string? Password);
public record LoginResponse(string Token, string Name, string Role);
