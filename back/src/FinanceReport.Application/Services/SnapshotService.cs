using System.Diagnostics;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Calculators;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FinanceReport.Application.Services;

/// <summary>Photographies quotidiennes du patrimoine (TS §3.4, RG-13, RG-14).</summary>
public sealed class SnapshotService(
    IRepository<Snapshot> snapshots,
    IRepository<Account> accounts,
    IRepository<Movement> movements,
    IRepository<Balance> balances,
    IRepository<SecurityPrice> prices,
    IClock clock,
    ILogger<SnapshotService> logger)
{
    /// <summary>
    /// Recalcule chaque snapshot existant de date ≥ <paramref name="from"/>, puis crée ou remplace celui du jour.
    /// Un seul passage chronologique sur les mouvements. À appeler dans une opération <see cref="IUnitOfWork"/>.
    /// </summary>
    public void Rebuild(DateOnly from)
    {
        var stopwatch = Stopwatch.StartNew();
        var today = clock.Today;
        var existing = snapshots.GetAll();
        var dates = existing
            .Select(s => s.Date)
            .Where(d => d >= from && d <= today)
            .Append(today)
            .Distinct()
            .Order()
            .ToList();

        var accountList = accounts.GetAll();
        var balanceSeries = DatedSeries<Guid>.Create(balances.GetAll(), b => b.AccountId, b => b.Date, b => b.Amount);
        var priceSeries = DatedSeries<Guid>.Create(prices.GetAll(), p => p.SecurityId, p => p.Date, p => p.Price);
        var trades = movements.GetAll().Where(m => m.Type.IsTrade()).InReplayOrder().ToList();

        var ledger = new PositionLedger();
        var next = 0;
        var computedAt = clock.UtcNow;
        var rebuilt = new List<Snapshot>(dates.Count);
        foreach (var date in dates)
        {
            while (next < trades.Count && trades[next].Date <= date)
            {
                ledger.Apply(trades[next++]);
            }

            var valuation = ValuationCalculator.Valuate(date, accountList, ledger.Positions, balanceSeries, priceSeries);
            rebuilt.Add(ToSnapshot(valuation, computedAt));
        }

        var rebuiltDates = rebuilt.Select(s => s.Date).ToHashSet();
        snapshots.Save([.. existing.Where(s => !rebuiltDates.Contains(s.Date)).Concat(rebuilt).OrderBy(s => s.Date)]);

        logger.LogInformation(
            "Snapshots recalculés depuis le {From:yyyy-MM-dd} : {Count} snapshot(s) en {Elapsed} ms",
            from,
            rebuilt.Count,
            stopwatch.ElapsedMilliseconds);
    }

    private static Snapshot ToSnapshot(Valuation valuation, DateTimeOffset computedAt) => new()
    {
        Date = valuation.Date,
        TotalNetWorth = valuation.TotalNetWorth,
        ComputedAt = computedAt,
        Accounts = [.. valuation.Accounts.Select(a => new SnapshotAccount { AccountId = a.AccountId, Value = a.Value, Cash = a.Cash })],
        Positions =
        [
            .. valuation.Positions.Select(p => new SnapshotPosition
            {
                AccountId = p.AccountId,
                SecurityId = p.SecurityId,
                Quantity = p.Quantity,
                AverageCost = p.AverageCost,
                Price = p.Price,
                MissingPrice = p.MissingPrice,
                MarketValue = p.MarketValue,
                UnrealizedGain = p.UnrealizedGain,
            }),
        ],
    };
}
