using FinanceReport.Application.Exceptions;
using FinanceReport.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>Contrôleur de test : endpoint protégé et déclenchement de chaque type d'erreur.</summary>
[ApiController]
[Route("api/test")]
public sealed class ProbeController : ControllerBase
{
    [HttpGet("protected")]
    public object Protected() => new { user = User.Identity?.Name };

    [AllowAnonymous]
    [HttpGet("throw/{kind}")]
    public IActionResult Throw(string kind) => throw (kind switch
    {
        "validation" => ValidationException.ForField("amount", "Le montant doit avoir au plus 2 décimales."),
        "notfound" => new NotFoundException("Compte introuvable."),
        "conflict" => new ConflictException("Valeur utilisée.", new Dictionary<string, object?> { ["usageCount"] = 2 }),
        "quantity" => new InsufficientQuantityException(Guid.Empty, 4m, new DateOnly(2026, 9, 15)),
        "locked" => new AccountLockedException(812),
        _ => new InvalidOperationException("Détail interne à ne pas exposer"),
    });

    [AllowAnonymous]
    [HttpPost("echo")]
    public EchoRequest Echo(EchoRequest request) => request;

    public sealed record EchoRequest(decimal Amount, DateOnly Date);
}
