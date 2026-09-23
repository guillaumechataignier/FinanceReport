using System.Text.RegularExpressions;
using FinanceReport.Application.Dtos;
using FinanceReport.Application.Exceptions;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>
/// Référentiels administrables : zones, secteurs, établissements (TS §2.4, RG-24, RG-30).
/// Aucune écriture ne déclenche de recalcul des snapshots, qui ne stockent ni zone, ni secteur, ni établissement.
/// </summary>
public sealed partial class ReferentialService(
    IReferentialRepository referentials,
    IRepository<Security> securities,
    IRepository<Account> accounts,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ReferentialService> logger)
{
    public const int MaxLabelLength = 60;

    public IReadOnlyList<ReferentialItemDto> List(ReferentialKind kind, bool includeArchived) =>
    [
        .. referentials.For(kind).GetAll()
            .Where(i => includeArchived || !i.Archived)
            .OrderBy(i => i.Label, TextComparison.French)
            .Select(i => ToDto(kind, i)),
    ];

    public async Task<ReferentialItemDto> CreateAsync(ReferentialKind kind, ReferentialItemRequest request)
    {
        var (code, label) = Validate(kind, request);
        var item = await unitOfWork.ExecuteAsync(() =>
        {
            var repository = referentials.For(kind);
            var items = repository.GetAll();
            EnsureUnique(kind, items, code, label, excludedId: null);

            var now = clock.UtcNow;
            var created = new ReferentialItem { Id = Guid.NewGuid(), Code = code, Label = label, CreatedAt = now, UpdatedAt = now };
            repository.Save([.. items, created]);
            return created;
        });

        logger.LogInformation("Référentiel {Kind} : valeur {Id} créée", kind, item.Id);
        return ToDto(kind, item);
    }

    public async Task<ReferentialItemDto> UpdateAsync(ReferentialKind kind, Guid id, ReferentialItemRequest request)
    {
        var (code, label) = Validate(kind, request);
        var item = await unitOfWork.ExecuteAsync(() =>
        {
            var repository = referentials.For(kind);
            var items = repository.GetAll();
            var current = Find(items, id);
            EnsureUnique(kind, items, code, label, excludedId: id);

            if (code != current.Code && UsageCount(kind, current) is var usage and > 0)
            {
                throw new ConflictException(
                    $"Le code d'une valeur utilisée n'est pas modifiable ({usage} utilisation(s)).",
                    UsageDetails(usage));
            }

            var updated = current with { Code = code, Label = label, UpdatedAt = clock.UtcNow };
            repository.Save([.. items.Select(i => i.Id == id ? updated : i)]);
            return updated;
        });

        logger.LogInformation("Référentiel {Kind} : valeur {Id} modifiée", kind, id);
        return ToDto(kind, item);
    }

    public Task<ReferentialItemDto> ArchiveAsync(ReferentialKind kind, Guid id) => SetArchivedAsync(kind, id, archived: true);

    public Task<ReferentialItemDto> UnarchiveAsync(ReferentialKind kind, Guid id) => SetArchivedAsync(kind, id, archived: false);

    public async Task DeleteAsync(ReferentialKind kind, Guid id)
    {
        await unitOfWork.ExecuteAsync(() =>
        {
            var repository = referentials.For(kind);
            var items = repository.GetAll();
            var current = Find(items, id);
            if (UsageCount(kind, current) is var usage and > 0)
            {
                throw new ConflictException(
                    $"Cette valeur est utilisée par {usage} élément(s) : archivez-la plutôt.",
                    UsageDetails(usage));
            }

            repository.Save([.. items.Where(i => i.Id != id)]);
            return id;
        });

        logger.LogInformation("Référentiel {Kind} : valeur {Id} supprimée", kind, id);
    }

    /// <summary>
    /// RG-24 : une zone ou un secteur nouvellement choisi doit exister et être actif ; une valeur inchangée,
    /// même archivée, est conservée.
    /// </summary>
    public void EnsureSelectable(ReferentialKind kind, string code, string? previousCode)
    {
        if (code == previousCode)
        {
            return;
        }

        var (field, noun) = kind == ReferentialKind.Zones ? ("zone", "La zone") : ("sector", "Le secteur");
        var item = referentials.For(kind).GetAll().FirstOrDefault(i => i.Code == code)
            ?? throw ValidationException.ForField(field, $"{noun} « {code} » n'existe pas.");
        if (item.Archived)
        {
            throw ValidationException.ForField(field, $"{noun} « {item.Label} » est archivé{(kind == ReferentialKind.Zones ? "e" : "")}.");
        }
    }

    /// <summary>RG-24 appliquée à l'établissement d'un compte.</summary>
    public void EnsureSelectableInstitution(Guid institutionId, Guid? previousId)
    {
        if (institutionId == previousId)
        {
            return;
        }

        var item = referentials.For(ReferentialKind.Institutions).GetAll().FirstOrDefault(i => i.Id == institutionId)
            ?? throw ValidationException.ForField("institutionId", "L'établissement choisi n'existe pas.");
        if (item.Archived)
        {
            throw ValidationException.ForField("institutionId", $"L'établissement « {item.Label} » est archivé.");
        }
    }

    public int UsageCount(ReferentialKind kind, ReferentialItem item) => kind switch
    {
        ReferentialKind.Zones => securities.GetAll().Count(s => s.Zone == item.Code),
        ReferentialKind.Sectors => securities.GetAll().Count(s => s.Sector == item.Code),
        ReferentialKind.Institutions => accounts.GetAll().Count(a => a.InstitutionId == item.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private async Task<ReferentialItemDto> SetArchivedAsync(ReferentialKind kind, Guid id, bool archived)
    {
        var item = await unitOfWork.ExecuteAsync(() =>
        {
            var repository = referentials.For(kind);
            var items = repository.GetAll();
            var updated = Find(items, id) with { Archived = archived, UpdatedAt = clock.UtcNow };
            repository.Save([.. items.Select(i => i.Id == id ? updated : i)]);
            return updated;
        });

        logger.LogInformation("Référentiel {Kind} : valeur {Id} {Operation}", kind, id, archived ? "archivée" : "désarchivée");
        return ToDto(kind, item);
    }

    private static (string? Code, string Label) Validate(ReferentialKind kind, ReferentialItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        var label = request.Label?.Trim() ?? "";
        if (label.Length == 0)
        {
            errors["label"] = ["Le libellé est obligatoire."];
        }
        else if (label.Length > MaxLabelLength)
        {
            errors["label"] = [$"Le libellé ne peut pas dépasser {MaxLabelLength} caractères."];
        }

        var code = request.Code;
        if (kind == ReferentialKind.Institutions)
        {
            if (code is not null)
            {
                errors["code"] = ["Un établissement n'a pas de code."];
            }
        }
        else if (string.IsNullOrEmpty(code))
        {
            errors["code"] = ["Le code est obligatoire."];
        }
        else if (!CodeFormat().IsMatch(code))
        {
            errors["code"] = ["Le code doit contenir de 2 à 40 caractères parmi A-Z, 0-9 et _ (majuscules, sans espace)."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors.First().Value[0], errors);
        }

        return (code, label);
    }

    private static void EnsureUnique(ReferentialKind kind, IReadOnlyList<ReferentialItem> items, string? code, string label, Guid? excludedId)
    {
        var others = items.Where(i => i.Id != excludedId).ToList();
        if (others.Any(i => TextComparison.SameText(i.Label, label)))
        {
            throw new ConflictException($"Le libellé « {label} » existe déjà.");
        }

        if (kind != ReferentialKind.Institutions && others.Any(i => i.Code == code))
        {
            throw new ConflictException($"Le code « {code} » existe déjà.");
        }
    }

    private static ReferentialItem Find(IReadOnlyList<ReferentialItem> items, Guid id) =>
        items.FirstOrDefault(i => i.Id == id) ?? throw new NotFoundException("Valeur de référentiel introuvable.");

    private static Dictionary<string, object?> UsageDetails(int usage) => new() { ["usageCount"] = usage };

    private ReferentialItemDto ToDto(ReferentialKind kind, ReferentialItem item) =>
        new(item.Id, item.Code, item.Label, item.Archived, UsageCount(kind, item));

    [GeneratedRegex("^[A-Z0-9_]{2,40}$")]
    private static partial Regex CodeFormat();
}
