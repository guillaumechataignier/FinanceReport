using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Infrastructure.Persistence;

/// <summary>
/// Au démarrage, crée chaque fichier de référentiel absent avec ses valeurs initiales (TS §2.4).
/// Un fichier existant, même vide, n'est jamais modifié.
/// </summary>
public sealed class ReferentialSeeder(
    ReferentialRepository referentials,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ReferentialSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var kind in Enum.GetValues<ReferentialKind>())
        {
            var store = referentials.Store(kind);
            if (store.FileExists)
            {
                continue;
            }

            var now = clock.UtcNow;
            var items = ReferentialDefaults.For(kind)
                .Select(v => new ReferentialItem
                {
                    Id = Guid.NewGuid(),
                    Code = v.Code,
                    Label = v.Label,
                    CreatedAt = now,
                    UpdatedAt = now,
                })
                .ToList();

            await unitOfWork.ExecuteAsync(() => { store.Save(items); return items.Count; }, cancellationToken);
            logger.LogInformation("Référentiel {Referential} initialisé : {Count} valeur(s)", store.EntityName, items.Count);
        }
    }
}
