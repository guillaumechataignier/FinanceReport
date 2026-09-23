using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;

namespace FinanceReport.Domain.Abstractions;

/// <summary>Accès aux trois référentiels administrables, un fichier chacun (TS §2.4).</summary>
public interface IReferentialRepository
{
    IRepository<ReferentialItem> For(ReferentialKind kind);
}
