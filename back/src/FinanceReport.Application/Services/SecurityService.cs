using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Application.Validators;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Rules;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>Référentiel des supports (UC-05, RG-12, RG-24, RG-26).</summary>
public sealed class SecurityService(
    IRepository<Security> securities,
    IRepository<Movement> movements,
    IRepository<SecurityPrice> prices,
    IReferentialRepository referentials,
    ReferentialService referentialService,
    SnapshotService snapshotService,
    IValidator<SecurityRequest> validator,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<SecurityService> logger)
{
    public IReadOnlyList<SecurityDto> List(bool includeArchived)
    {
        var context = new MappingContext(referentials, prices.GetAll());
        return
        [
            .. securities.GetAll()
                .Where(s => includeArchived || !s.Archived)
                .OrderBy(s => s.Name, TextComparison.French)
                .Select(context.ToDto),
        ];
    }

    public SecurityDto Get(Guid id) => new MappingContext(referentials, prices.GetAll()).ToDto(Find(securities.GetAll(), id));

    public async Task<SecurityDto> CreateAsync(SecurityRequest request)
    {
        validator.ValidateOrThrow(request);
        var id = await unitOfWork.ExecuteAsync(() =>
        {
            var all = securities.GetAll();
            var (name, code, zone, sector) = Normalize(request);
            EnsureUniqueCode(all, code, excludedId: null);
            referentialService.EnsureSelectable(ReferentialKind.Zones, zone, previousCode: null);
            referentialService.EnsureSelectable(ReferentialKind.Sectors, sector, previousCode: null);

            var now = clock.UtcNow;
            var security = new Security
            {
                Id = Guid.NewGuid(),
                Name = name,
                Code = code,
                Type = request.Type!.Value,
                Zone = zone,
                Sector = sector,
                CreatedAt = now,
                UpdatedAt = now,
            };
            securities.Save([.. all, security]);
            snapshotService.Rebuild(clock.Today);
            return security.Id;
        });

        logger.LogInformation("Support {Id} créé", id);
        return Get(id);
    }

    public async Task<SecurityDto> UpdateAsync(Guid id, SecurityRequest request)
    {
        validator.ValidateOrThrow(request);
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = securities.GetAll();
            var current = Find(all, id);
            var (name, code, zone, sector) = Normalize(request);
            EnsureUniqueCode(all, code, excludedId: id);
            referentialService.EnsureSelectable(ReferentialKind.Zones, zone, current.Zone);
            referentialService.EnsureSelectable(ReferentialKind.Sectors, sector, current.Sector);

            var updated = current with
            {
                Name = name,
                Code = code,
                Type = request.Type!.Value,
                Zone = zone,
                Sector = sector,
                UpdatedAt = clock.UtcNow,
            };
            securities.Save([.. all.Select(s => s.Id == id ? updated : s)]);
            snapshotService.Rebuild(clock.Today);
            return id;
        });

        logger.LogInformation("Support {Id} modifié", id);
        return Get(id);
    }

    public Task<SecurityDto> ArchiveAsync(Guid id) => SetArchivedAsync(id, archived: true);

    public Task<SecurityDto> UnarchiveAsync(Guid id) => SetArchivedAsync(id, archived: false);

    /// <summary>RG-12 : un support lié à un mouvement ne peut qu'être archivé. Ses cours sont supprimés avec lui.</summary>
    public async Task DeleteAsync(Guid id)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = securities.GetAll();
            Find(all, id);
            var movementCount = movements.GetAll().Count(m => m.SecurityId == id);
            if (movementCount > 0)
            {
                throw new ConflictException(
                    $"Ce support est utilisé par {movementCount} mouvement(s) : archivez-le plutôt.",
                    new Dictionary<string, object?> { ["movementCount"] = movementCount });
            }

            securities.Save([.. all.Where(s => s.Id != id)]);
            var allPrices = prices.GetAll();
            if (allPrices.Any(p => p.SecurityId == id))
            {
                prices.Save([.. allPrices.Where(p => p.SecurityId != id)]);
            }

            snapshotService.Rebuild(clock.Today);
            return id;
        });

        logger.LogInformation("Support {Id} supprimé", id);
    }

    private async Task<SecurityDto> SetArchivedAsync(Guid id, bool archived)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            var all = securities.GetAll();
            var updated = Find(all, id) with { Archived = archived, UpdatedAt = clock.UtcNow };
            securities.Save([.. all.Select(s => s.Id == id ? updated : s)]);
            snapshotService.Rebuild(clock.Today);
            return id;
        });

        logger.LogInformation("Support {Id} {Operation}", id, archived ? "archivé" : "désarchivé");
        return Get(id);
    }

    private static (string Name, string Code, string Zone, string Sector) Normalize(SecurityRequest request) =>
        (request.Name!.Trim(), request.Code!.Trim(), request.Zone!.Trim(), request.Sector!.Trim());

    private static void EnsureUniqueCode(IReadOnlyList<Security> all, string code, Guid? excludedId)
    {
        if (all.Any(s => s.Id != excludedId && string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictException($"Le code « {code} » est déjà utilisé par un autre support.");
        }
    }

    private static Security Find(IReadOnlyList<Security> all, Guid id) =>
        all.FirstOrDefault(s => s.Id == id) ?? throw new NotFoundException("Support introuvable.");

    /// <summary>Libellés courants des zones et secteurs, et dernier cours de chaque support.</summary>
    private sealed class MappingContext(IReferentialRepository referentials, IReadOnlyList<SecurityPrice> allPrices)
    {
        private readonly Dictionary<string, ReferentialItem> _zones = ByCode(referentials, ReferentialKind.Zones);
        private readonly Dictionary<string, ReferentialItem> _sectors = ByCode(referentials, ReferentialKind.Sectors);
        private readonly Dictionary<Guid, SecurityPrice> _lastPrices = allPrices
            .GroupBy(p => p.SecurityId)
            .ToDictionary(g => g.Key, g => g.MaxBy(p => p.Date)!);

        public SecurityDto ToDto(Security security)
        {
            var zone = _zones.GetValueOrDefault(security.Zone);
            var sector = _sectors.GetValueOrDefault(security.Sector);
            var lastPrice = _lastPrices.GetValueOrDefault(security.Id);
            return new SecurityDto(
                security.Id,
                security.Name,
                security.Code,
                security.Type,
                security.Zone,
                zone?.Label ?? security.Zone,
                zone?.Archived ?? false,
                security.Sector,
                sector?.Label ?? security.Sector,
                sector?.Archived ?? false,
                security.Archived,
                DecimalMath.Round(lastPrice?.Price, Precision.Price(security.Type)),
                lastPrice?.Date);
        }

        private static Dictionary<string, ReferentialItem> ByCode(IReferentialRepository referentials, ReferentialKind kind) =>
            referentials.For(kind).GetAll().Where(i => i.Code is not null).ToDictionary(i => i.Code!);
    }
}
