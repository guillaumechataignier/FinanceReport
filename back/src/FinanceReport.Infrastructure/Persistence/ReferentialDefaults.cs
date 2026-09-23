using FinanceReport.Domain.Enums;

namespace FinanceReport.Infrastructure.Persistence;

/// <summary>Valeurs initiales des référentiels (FS §3.11) : 5 zones, 13 secteurs, aucun établissement.</summary>
public static class ReferentialDefaults
{
    public static IReadOnlyList<(string Code, string Label)> Zones { get; } =
    [
        ("EUROPE", "Europe"),
        ("AMERIQUE_NORD", "Amérique du Nord"),
        ("ASIE_PACIFIQUE", "Asie-Pacifique"),
        ("EMERGENTS", "Émergents"),
        ("MONDE", "Monde"),
    ];

    public static IReadOnlyList<(string Code, string Label)> Sectors { get; } =
    [
        ("ENERGIE", "Énergie"),
        ("MATERIAUX", "Matériaux"),
        ("INDUSTRIE", "Industrie"),
        ("CONSO_DISCRETIONNAIRE", "Consommation discrétionnaire"),
        ("CONSO_BASE", "Consommation de base"),
        ("SANTE", "Santé"),
        ("FINANCE", "Finance"),
        ("TECHNOLOGIE", "Technologies de l'information"),
        ("COMMUNICATION", "Services de communication"),
        ("SERVICES_PUBLICS", "Services aux collectivités"),
        ("IMMOBILIER", "Immobilier"),
        ("DIVERSIFIE", "Diversifié"),
        ("NON_APPLICABLE", "Non applicable"),
    ];

    public static IReadOnlyList<(string Code, string Label)> For(ReferentialKind kind) => kind switch
    {
        ReferentialKind.Zones => Zones,
        ReferentialKind.Sectors => Sectors,
        _ => [],
    };
}
