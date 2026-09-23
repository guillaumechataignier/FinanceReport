namespace FinanceReport.Application.Dtos;

public sealed record AuthStatusResponse(bool Initialized);

public sealed record SetupRequest(string? Username, string? Password, string? PasswordConfirmation);

public sealed record SetupResponse(string Username);

public sealed record LoginRequest(string? Username, string? Password);

public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt);
