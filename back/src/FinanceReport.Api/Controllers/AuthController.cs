using FinanceReport.Application.Dtos;
using FinanceReport.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(AuthService authService) : ControllerBase
{
    [HttpGet("status")]
    public AuthStatusResponse Status() => authService.Status();

    [HttpPost("setup")]
    public IActionResult Setup(SetupRequest request) =>
        StatusCode(StatusCodes.Status201Created, authService.Setup(request));

    [HttpPost("login")]
    public LoginResponse Login(LoginRequest request) => authService.Login(request);
}
