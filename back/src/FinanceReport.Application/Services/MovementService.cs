using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Validators;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Calculators;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Rules;
using FluentValidation;
using Microsoft.Extensions.Logging;
using ValidationException = FinanceReport.Application.Exceptions.ValidationException;

namespace FinanceReport.Application.Services;

/// <summary>
/// Mouvements (UC-06 à UC-09). Toute écriture rejoue les couples (compte, support) concernés sur la liste candidate :
/// une vente excédentaire est rejetée en 422 sans aucune écriture (TS §3.2, RG-10, RG-13).
/// </summary>
public sealed class MovementService(
    IRepository<Movement> movements,
    IRepository<Account> accounts,
    IRepository<Security> securities,
    SnapshotService snapshotService,
    IValidator<MovementRequest> validator,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<MovementService> logger)
{
    public IReadOnlyList<MovementDto> List(MovementFilter filter)
    {
        var mapping = new Mapping(accounts.GetAll(), securities.GetAll());
        return
        [
            .. movements.GetAll()
                .Where(m => filter.AccountId is null || m.AccountId == filter.AccountId)
                .Where(m => filter.SecurityId is null || m.SecurityId == filter.SecurityId)
                .Where(m => filter.Type is null || m.Type == filter.Type)
                .Where(m => filter.From is null || m.Date >= filter.From)
                .Where(m => filter.To is null || m.Date <= filter.To)
                .OrderByDescending(m => m.Date)
                .ThenByDescending(m => m.Sequence)
                .Select(mapping.ToDto),
        ];
    }

    public MovementDto Get(Guid id) => new Mapping(accounts.GetAll(), securities.GetAll()).ToDto(Find(movements.GetAll(), id));

    public async Task<MovementDto> CreateAsync(MovementRequest request)
    {
        validator.ValidateOrThrow(request);
        var id = await unitOfWork.ExecuteAsync(() =>
        {
            var all = movements.GetAll();
            EnsureReferences(request, previous: null);

            var now = clock.UtcNow;
            var movement = Apply(request, new Movement
            {
                Id = Guid.NewGuid(),
                Type = request.Type!.Value,
                Date = request.Date!.Value,
                AccountId = request.AccountId!.Value,
                Sequence = all.Count == 0 ? 1 : all.Max(m => m.Sequence) + 1,
                CreatedAt = now,
                UpdatedAt = now,
            });

            List<Movement> candidate = [.. all, movement];
            EnsureConsistent(candidate, PairOf(movement));
            movements.Save(candidate);
            snapshotService.Rebuild(movement.Date);
            return movement.Id;
        });

        logger.LogInformation("Mouvement {Id} créé", id);
        return Get(id);
    }

    public async Task<MovementDto> UpdateAsync(Guid id, MovementRequest request)
    {
        validator.ValidateOrThrow(request);
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = movements.GetAll();
            var current = Find(all, id);
            if (request.Type != current.Type)
            {
                throw ValidationException.ForField("type", "Le type d'un mouvement n'est pas modifiable.");
            }

            EnsureReferences(request, current);
            var updated = Apply(request, current with
            {
                Date = request.Date!.Value,
                AccountId = request.AccountId!.Value,
                UpdatedAt = clock.UtcNow,
            });

            List<Movement> candidate = [.. all.Select(m => m.Id == id ? updated : m)];
            EnsureConsistent(candidate, PairOf(current), PairOf(updated));
            movements.Save(candidate);
            snapshotService.Rebuild(Min(current.Date, updated.Date));
            return id;
        });

        logger.LogInformation("Mouvement {Id} modifié", id);
        return Get(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = movements.GetAll();
            var current = Find(all, id);

            List<Movement> candidate = [.. all.Where(m => m.Id != id)];
            EnsureConsistent(candidate, PairOf(current));
            movements.Save(candidate);
            snapshotService.Rebuild(current.Date);
            return id;
        });

        logger.LogInformation("Mouvement {Id} supprimé", id);
    }

    /// <summary>
    /// Compte titres non archivé (RG-21, RG-27), support existant et non archivé (RG-12), date non future (RG-25),
    /// précision du prix selon le support (RG-02). Un compte ou un support inchangé lors d'une modification est toléré
    /// même s'il a été archivé depuis.
    /// </summary>
    private void EnsureReferences(MovementRequest request, Movement? previous)
    {
        DateRules.EnsureNotFuture(request.Date!.Value, clock.Today);

        var account = accounts.GetAll().FirstOrDefault(a => a.Id == request.AccountId)
            ?? throw ValidationException.ForField("accountId", "Le compte n'existe pas.");
        if (!account.Type.IsSecuritiesAccount())
        {
            throw ValidationException.ForField("accountId", "Les mouvements ne sont possibles que sur un compte titres (CTO, PEA, CRYPTO).");
        }

        if (account.Archived && account.Id != previous?.AccountId)
        {
            throw ValidationException.ForField("accountId", "Le compte est archivé.");
        }

        if (!request.Type!.Value.IsTrade())
        {
            return;
        }

        var security = securities.GetAll().FirstOrDefault(s => s.Id == request.SecurityId)
            ?? throw ValidationException.ForField("securityId", "Le support n'existe pas.");
        if (security.Archived && security.Id != previous?.SecurityId)
        {
            throw ValidationException.ForField("securityId", "Le support est archivé : il ne peut plus recevoir de mouvement.");
        }

        PriceRules.EnsurePrecision(request.UnitPrice!.Value, security, "unitPrice", "Le prix unitaire");
    }

    private static Movement Apply(MovementRequest request, Movement movement) => request.Type!.Value.IsTrade()
        ? movement with
        {
            SecurityId = request.SecurityId,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Fees = request.Fees ?? 0m,
            Amount = null,
        }
        : movement with { SecurityId = null, Quantity = null, UnitPrice = null, Fees = null, Amount = request.Amount };

    /// <summary>Rejoue les couples impactés jusqu'à la fin de l'historique ; lève une InsufficientQuantityException (422).</summary>
    private static void EnsureConsistent(IReadOnlyList<Movement> candidate, params PositionKey?[] pairs)
    {
        var impacted = pairs.OfType<PositionKey>().ToHashSet();
        if (impacted.Count == 0)
        {
            return;
        }

        PositionCalculator.Compute(
            candidate.Where(m => m.SecurityId is { } s && impacted.Contains(new PositionKey(m.AccountId, s))),
            DateOnly.MaxValue);
    }

    private static PositionKey? PairOf(Movement movement) =>
        movement.Type.IsTrade() && movement.SecurityId is { } securityId ? new PositionKey(movement.AccountId, securityId) : null;

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static Movement Find(IReadOnlyList<Movement> all, Guid id) =>
        all.FirstOrDefault(m => m.Id == id) ?? throw new NotFoundException("Mouvement introuvable.");

    private sealed class Mapping(IReadOnlyList<Account> accounts, IReadOnlyList<Security> securities)
    {
        private readonly Dictionary<Guid, Account> _accounts = accounts.ToDictionary(a => a.Id);
        private readonly Dictionary<Guid, Security> _securities = securities.ToDictionary(s => s.Id);

        public MovementDto ToDto(Movement m)
        {
            var security = m.SecurityId is { } id ? _securities.GetValueOrDefault(id) : null;
            var total = m.Type switch
            {
                MovementType.Achat => m.Quantity!.Value * m.UnitPrice!.Value + (m.Fees ?? 0m),
                MovementType.Vente => m.Quantity!.Value * m.UnitPrice!.Value - (m.Fees ?? 0m),
                _ => m.Amount ?? 0m,
            };

            return new MovementDto(
                m.Id,
                m.Type,
                m.Date,
                m.AccountId,
                _accounts.GetValueOrDefault(m.AccountId)?.Name ?? "",
                m.SecurityId,
                security?.Name,
                security?.Code,
                m.Quantity,
                m.UnitPrice,
                m.Fees,
                m.Amount,
                DecimalMath.Round(total, Precision.Amount),
                m.Sequence,
                m.CreatedAt,
                m.UpdatedAt);
        }
    }
}
