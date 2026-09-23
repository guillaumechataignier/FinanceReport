using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Validators;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>Cours des supports : upsert sur (support, date) et recalcul des snapshots (UC-10, RG-13).</summary>
public sealed class PriceService(
    IRepository<SecurityPrice> prices,
    IRepository<Security> securities,
    SnapshotService snapshotService,
    IValidator<PriceRequest> validator,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<PriceService> logger)
{
    public IReadOnlyList<PriceDto> List(Guid securityId)
    {
        FindSecurity(securityId);
        return
        [
            .. prices.GetAll()
                .Where(p => p.SecurityId == securityId)
                .OrderByDescending(p => p.Date)
                .Select(ToDto),
        ];
    }

    public async Task<PriceDto> UpsertAsync(Guid securityId, DateOnly date, PriceRequest request)
    {
        validator.ValidateOrThrow(request);
        var price = await unitOfWork.ExecuteAsync(() =>
        {
            var security = FindSecurity(securityId);
            PriceRules.EnsurePrecision(request.Price!.Value, security, "price", "Le cours");
            DateRules.EnsureNotFuture(date, clock.Today);

            var saved = new SecurityPrice { SecurityId = securityId, Date = date, Price = request.Price.Value, UpdatedAt = clock.UtcNow };
            prices.Save([.. prices.GetAll().Where(p => !IsKey(p, securityId, date)), saved]);
            snapshotService.Rebuild(date);
            return saved;
        });

        logger.LogInformation("Cours du support {SecurityId} au {Date:yyyy-MM-dd} enregistré", securityId, date);
        return ToDto(price);
    }

    public async Task DeleteAsync(Guid securityId, DateOnly date)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            FindSecurity(securityId);
            var all = prices.GetAll();
            if (!all.Any(p => IsKey(p, securityId, date)))
            {
                throw new NotFoundException("Aucun cours à cette date pour ce support.");
            }

            prices.Save([.. all.Where(p => !IsKey(p, securityId, date))]);
            snapshotService.Rebuild(date);
            return date;
        });

        logger.LogInformation("Cours du support {SecurityId} au {Date:yyyy-MM-dd} supprimé", securityId, date);
    }

    private Security FindSecurity(Guid securityId) =>
        securities.GetAll().FirstOrDefault(s => s.Id == securityId) ?? throw new NotFoundException("Support introuvable.");

    private static bool IsKey(SecurityPrice price, Guid securityId, DateOnly date) => price.SecurityId == securityId && price.Date == date;

    private static PriceDto ToDto(SecurityPrice price) => new(price.SecurityId, price.Date, price.Price);
}
