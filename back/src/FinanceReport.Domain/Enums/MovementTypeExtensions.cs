namespace FinanceReport.Domain.Enums;

public static class MovementTypeExtensions
{
    /// <summary>ACHAT et VENTE portent un support, une quantité, un prix et des frais ; VERSEMENT et RETRAIT, un montant.</summary>
    public static bool IsTrade(this MovementType type) => type is MovementType.Achat or MovementType.Vente;
}
