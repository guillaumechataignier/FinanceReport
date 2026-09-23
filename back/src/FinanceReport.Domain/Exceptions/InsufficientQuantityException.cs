using System.Globalization;

namespace FinanceReport.Domain.Exceptions;

/// <summary>Une vente dépasse la quantité détenue à sa date (RG-10).</summary>
public sealed class InsufficientQuantityException(Guid movementId, decimal availableQuantity, DateOnly date)
    : Exception(string.Create(
        CultureInfo.InvariantCulture,
        $"Quantité insuffisante : {availableQuantity:0.00000000} disponibles au {date:yyyy-MM-dd}"))
{
    public Guid MovementId { get; } = movementId;
    public decimal AvailableQuantity { get; } = availableQuantity;
    public DateOnly Date { get; } = date;
}
