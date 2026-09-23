using FinanceReport.Domain.Calculators;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Domain.Exceptions;
using FluentAssertions;
using static FinanceReport.Domain.Tests.ReferenceData;

namespace FinanceReport.Domain.Tests;

public class PositionCalculatorTests
{
    private static readonly PositionKey AS1 = new(AccountA, S1);
    private const decimal PruAfterM2 = 1553m / 15m;

    [Fact] // TC-FUNC-03
    public void Average_cost_includes_fees_and_is_unchanged_by_a_sale()
    {
        var at15Jan = PositionCalculator.Compute(Movements, new DateOnly(2026, 1, 15))[AS1];
        var at15Feb = PositionCalculator.Compute(Movements, new DateOnly(2026, 2, 15))[AS1];
        var at15Mar = PositionCalculator.Compute(Movements, new DateOnly(2026, 3, 15))[AS1];

        at15Jan.AverageCost.Should().Be(100.20m);
        at15Jan.Quantity.Should().Be(10m);

        at15Feb.AverageCost.Should().BeApproximately(PruAfterM2, 1e-24m);
        at15Feb.Quantity.Should().Be(15m);

        at15Mar.AverageCost.Should().Be(at15Feb.AverageCost);
        at15Mar.Quantity.Should().Be(9m);
    }

    [Fact] // TC-FUNC-04
    public void Closed_position_resets_average_cost_and_keeps_realized_gain()
    {
        var movements = Movements.Concat(
        [
            Sell(Id(0x104), new DateOnly(2026, 4, 1), AccountA, S1, 9m, 125.00m, 0m, sequence: 4),
            Buy(Id(0x105), new DateOnly(2026, 5, 1), AccountA, S1, 2m, 130.00m, 1.00m, sequence: 5),
        ]).ToList();

        var at15Apr = PositionCalculator.Compute(movements, new DateOnly(2026, 4, 15))[AS1];
        var at15May = PositionCalculator.Compute(movements, new DateOnly(2026, 5, 15))[AS1];

        at15Apr.Quantity.Should().Be(0m);
        at15Apr.IsOpen.Should().BeFalse();
        at15Apr.AverageCost.Should().Be(0m);
        Math.Round(at15Apr.RealizedGain, 2).Should().Be(290.50m);

        at15May.AverageCost.Should().Be(130.50m);
        at15May.Quantity.Should().Be(2m);
        Math.Round(at15May.RealizedGain, 2).Should().Be(290.50m);
    }

    [Fact] // TC-FUNC-05
    public void Realized_gain_deducts_sale_fees()
    {
        var position = PositionCalculator.Compute(Movements, Today)[AS1];

        Math.Round(position.RealizedGain, 2).Should().Be(97.30m);
    }

    [Fact] // TC-FUNC-25 : la PV réalisée repose sur le PRU non arrondi
    public void Realized_gain_uses_unrounded_average_cost()
    {
        var position = PositionCalculator.Compute(Movements, Today)[AS1];
        var withRoundedPru = 6m * (120m - 103.53m) - 1.50m;

        position.RealizedGain.Should().BeApproximately(97.30m, 1e-20m);
        withRoundedPru.Should().Be(97.32m, "l'arrondi intermédiaire fausserait le résultat de 2 centimes");
    }

    [Fact] // TC-FUNC-08
    public void Sale_above_held_quantity_is_rejected_with_available_quantity()
    {
        var sale = Sell(Id(0x200), new DateOnly(2026, 9, 20), AccountA, S1, 10m, 115m, 0m, sequence: 4);

        var act = () => PositionCalculator.Compute(Movements.Append(sale), DateOnly.MaxValue);

        var exception = act.Should().Throw<InsufficientQuantityException>().Which;
        exception.MovementId.Should().Be(sale.Id);
        exception.AvailableQuantity.Should().Be(9m);
        exception.Date.Should().Be(new DateOnly(2026, 9, 20));
        exception.Message.Should().Be("Quantité insuffisante : 9.00000000 disponibles au 2026-09-20");
    }

    [Fact] // TC-FUNC-08, variante : même date, achat créé avant la vente
    public void Same_day_sale_created_after_purchase_is_accepted()
    {
        var day = new DateOnly(2026, 9, 20);
        var movements = Movements.Concat(
        [
            Buy(Id(0x201), day, AccountA, S1, 2m, 115m, 0m, sequence: 4),
            Sell(Id(0x202), day, AccountA, S1, 11m, 115m, 0m, sequence: 5),
        ]);

        PositionCalculator.Compute(movements, DateOnly.MaxValue)[AS1].Quantity.Should().Be(0m);
    }

    [Fact] // TC-FUNC-08, variante : même date, vente créée avant l'achat
    public void Same_day_sale_created_before_purchase_is_rejected()
    {
        var day = new DateOnly(2026, 9, 20);
        var sale = Sell(Id(0x202), day, AccountA, S1, 11m, 115m, 0m, sequence: 4);
        var movements = Movements.Concat([sale, Buy(Id(0x201), day, AccountA, S1, 2m, 115m, 0m, sequence: 5)]);

        var act = () => PositionCalculator.Compute(movements, DateOnly.MaxValue);

        act.Should().Throw<InsufficientQuantityException>().Which.MovementId.Should().Be(sale.Id);
    }

    [Fact] // TC-FUNC-12 : sans M1, la vente M3 devient excédentaire
    public void Removing_an_earlier_purchase_makes_the_later_sale_conflict()
    {
        var withoutM1 = Movements.Where(m => m.Id != M1);

        var act = () => PositionCalculator.Compute(withoutM1, DateOnly.MaxValue);

        var exception = act.Should().Throw<InsufficientQuantityException>().Which;
        exception.MovementId.Should().Be(M3);
        exception.AvailableQuantity.Should().Be(5m);
    }

    [Fact]
    public void Movements_are_replayed_in_date_order_whatever_the_input_order()
    {
        var shuffled = Movements.Reverse();

        PositionCalculator.Compute(shuffled, Today)[AS1].Quantity.Should().Be(9m);
    }

    [Fact] // RG-22
    public void Deposits_and_withdrawals_do_not_create_positions()
    {
        var movements = new List<Movement>
        {
            CashMovement(MovementType.Versement, new DateOnly(2026, 9, 20), AccountA, 1000m, sequence: 1),
            CashMovement(MovementType.Retrait, new DateOnly(2026, 9, 21), AccountA, 100m, sequence: 2),
        };

        PositionCalculator.Compute(movements, Today).Should().BeEmpty();
    }

    [Fact]
    public void Positions_are_kept_per_account_and_security()
    {
        var movements = Movements.Concat(
        [
            Buy(Id(0x300), new DateOnly(2026, 9, 15), AccountA, S2, 0.01m, 50000m, 5m, sequence: 4),
            Buy(Id(0x301), new DateOnly(2026, 9, 15), AccountC, S1, 1m, 100m, 0m, sequence: 5),
        ]);

        var positions = PositionCalculator.Compute(movements, Today);

        positions.Should().HaveCount(3);
        positions[new PositionKey(AccountA, S2)].AverageCost.Should().Be(50500m);
        positions[new PositionKey(AccountC, S1)].Quantity.Should().Be(1m);
    }
}
