using System.Diagnostics;
using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Abstractions;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FinanceReport.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>
/// TC-TECH-07 : 20 comptes, 200 supports, 10 000 mouvements, 50 000 cours et 3 650 snapshots.
/// Les données sont écrites directement dans les dépôts (10 000 appels HTTP recalculeraient chacun les snapshots).
/// </summary>
[Trait("Category", "Performance")]
public sealed class PerformanceTests(ITestOutputHelper output) : IDisposable
{
    private static readonly DateOnly Today = new(2026, 9, 23);
    private static readonly DateOnly FirstDay = Today.AddDays(-3649);
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Restitutions_answer_under_2_seconds_on_target_volume()
    {
        var client = await _factory.CreateAuthenticatedClientAsync();
        var securityIds = await SeedAsync();

        // Modification rétroactive au premier jour : recalcul des 3 650 snapshots.
        var rebuild = Stopwatch.StartNew();
        await client.PutJsonAsync<PriceDto>($"/api/securities/{securityIds[0]}/prices/{FirstDay:yyyy-MM-dd}", new { price = 99.99m });
        rebuild.Stop();
        output.WriteLine($"Modification rétroactive : {rebuild.ElapsedMilliseconds} ms");
        output.WriteLine($"snapshots.json : {new FileInfo(_factory.FilePath("snapshots")).Length / 1024 / 1024} Mo");

        var summary = await MeasureAsync(client, "/api/dashboard/summary");
        var history = await MeasureAsync(client, "/api/dashboard/history?period=ALL");
        var positions = await MeasureAsync(client, "/api/dashboard/positions");

        (await client.GetJsonAsync<HistoryDto>("/api/dashboard/history?period=ALL")).Points.Should().HaveCount(3650);
        summary.Should().BeLessThan(TimeSpan.FromSeconds(2));
        history.Should().BeLessThan(TimeSpan.FromSeconds(2));
        positions.Should().BeLessThan(TimeSpan.FromSeconds(2));
        rebuild.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    /// <summary>Temps de réponse au 95e centile sur 10 appels, après un appel de chauffe.</summary>
    private async Task<TimeSpan> MeasureAsync(HttpClient client, string url)
    {
        (await client.GetAsync(url)).EnsureSuccessStatusCode();
        var samples = new List<TimeSpan>();
        for (var i = 0; i < 10; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            (await client.GetAsync(url)).EnsureSuccessStatusCode();
            samples.Add(stopwatch.Elapsed);
        }

        var p95 = samples.Order().ElementAt((int)Math.Ceiling(0.95 * samples.Count) - 1);
        output.WriteLine($"{url} : p95 = {p95.TotalMilliseconds:0} ms");
        return p95;
    }

    private async Task<List<Guid>> SeedAsync()
    {
        var services = _factory.Services;
        var now = _factory.Time.GetUtcNow();
        var zones = ReferentialDefaults.Zones.Select(z => z.Code).ToArray();
        var sectors = ReferentialDefaults.Sectors.Select(s => s.Code).ToArray();
        var institution = new ReferentialItem { Id = Guid.NewGuid(), Label = "Banque", CreatedAt = now, UpdatedAt = now };

        AccountType[] securitiesTypes = [AccountType.Pea, AccountType.Cto, AccountType.Crypto];
        var accounts = Enumerable.Range(0, 20).Select(i => new Account
        {
            Id = Guid.NewGuid(),
            Name = $"Compte {i:D2}",
            Type = i < 15 ? securitiesTypes[i % 3] : AccountType.Livret,
            InstitutionId = institution.Id,
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        var securities = Enumerable.Range(0, 200).Select(i => new Security
        {
            Id = Guid.NewGuid(),
            Name = $"Support {i:D3}",
            Code = $"CODE{i:D3}",
            Type = (SecurityType)(i % 4),
            Zone = zones[i % zones.Length],
            Sector = sectors[i % sectors.Length],
            CreatedAt = now,
            UpdatedAt = now,
        }).ToList();

        // 50 mouvements par support, sur le compte titres qui le détient : achat de 10, puis vente de 5.
        var movements = new List<Movement>(10_000);
        for (var k = 0; k < 50; k++)
        {
            for (var i = 0; i < securities.Count; i++)
            {
                var isBuy = k % 2 == 0;
                movements.Add(new Movement
                {
                    Id = Guid.NewGuid(),
                    Type = isBuy ? MovementType.Achat : MovementType.Vente,
                    Date = FirstDay.AddDays(k * 73),
                    AccountId = accounts[i % 15].Id,
                    SecurityId = securities[i].Id,
                    Quantity = isBuy ? 10m : 5m,
                    UnitPrice = 100m + k,
                    Fees = 1m,
                    Sequence = movements.Count + 1,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
        }

        var prices = securities.SelectMany(s => Enumerable.Range(0, 250).Select(j => new SecurityPrice
        {
            SecurityId = s.Id,
            Date = FirstDay.AddDays(j * 14),
            Price = 100m + j / 10m,
            UpdatedAt = now,
        })).ToList();

        var balances = accounts.SelectMany(a => Enumerable.Range(0, 120).Select(j => new Balance
        {
            AccountId = a.Id,
            Date = FirstDay.AddDays(j * 30),
            Amount = 1000m + j,
            UpdatedAt = now,
        })).ToList();

        var snapshots = Enumerable.Range(0, 3650).Select(d => new Snapshot
        {
            Date = FirstDay.AddDays(d),
            TotalNetWorth = 0m,
            ComputedAt = now,
            Accounts = [],
            Positions = [],
        }).ToList();

        await services.GetRequiredService<IUnitOfWork>().ExecuteAsync(() =>
        {
            services.GetRequiredService<IReferentialRepository>().For(ReferentialKind.Institutions).Save([institution]);
            services.GetRequiredService<IRepository<Account>>().Save(accounts);
            services.GetRequiredService<IRepository<Security>>().Save(securities);
            services.GetRequiredService<IRepository<Movement>>().Save(movements);
            services.GetRequiredService<IRepository<SecurityPrice>>().Save(prices);
            services.GetRequiredService<IRepository<Balance>>().Save(balances);
            services.GetRequiredService<IRepository<Snapshot>>().Save(snapshots);
            return 0;
        });

        return [.. securities.Select(s => s.Id)];
    }
}
