using System.Text.Json;
using FinanceReport.Application.Tests.Support;
using FinanceReport.Domain.Entities;
using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Application.Tests.Persistence;

public sealed class JsonFileStoreTests : IDisposable
{
    private readonly StorageHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public void Missing_file_reads_as_empty_collection()
    {
        _harness.Store<Account>("accounts").GetAll().Should().BeEmpty();
    }

    [Fact] // TS §2.1, §2.3
    public async Task Written_file_uses_envelope_and_serialization_conventions()
    {
        var store = _harness.Store<Movement>("movements");
        var movement = new Movement
        {
            Id = Guid.NewGuid(),
            Type = MovementType.Achat,
            Date = new DateOnly(2026, 9, 15),
            AccountId = Guid.NewGuid(),
            SecurityId = Guid.NewGuid(),
            Quantity = 10.5m,
            UnitPrice = 102.40m,
            Fees = 1.99m,
            Sequence = 1542,
            CreatedAt = _harness.Clock.UtcNow,
            UpdatedAt = _harness.Clock.UtcNow,
        };

        await _harness.WriteAsync(store, [movement]);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(_harness.FilePath("movements")));
        var root = document.RootElement;
        root.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        var item = root.GetProperty("items")[0];
        item.GetProperty("type").GetString().Should().Be("ACHAT");
        item.GetProperty("date").GetString().Should().Be("2026-09-15");
        item.GetProperty("quantity").ValueKind.Should().Be(JsonValueKind.Number);
        item.GetProperty("unitPrice").GetDecimal().Should().Be(102.40m);
        item.GetProperty("amount").ValueKind.Should().Be(JsonValueKind.Null);
        item.GetProperty("sequence").GetInt64().Should().Be(1542);
    }

    [Fact]
    public async Task Written_items_are_read_back_by_a_new_store()
    {
        var account = _harness.NewAccount("PEA Test") with { Type = AccountType.Cto };
        await _harness.WriteAsync(_harness.Store<Account>("accounts"), [account]);

        var reloaded = _harness.Store<Account>("accounts").GetAll();

        reloaded.Should().ContainSingle().Which.Should().Be(account);
    }

    [Fact]
    public void Save_outside_an_operation_is_refused()
    {
        var store = _harness.Store<Account>("accounts");

        var act = () => store.Save([_harness.NewAccount("X")]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task Staged_items_are_visible_inside_the_operation_only_until_commit()
    {
        var store = _harness.Store<Account>("accounts");
        IReadOnlyList<Account>? seenInside = null;

        await _harness.UnitOfWork.ExecuteAsync(() =>
        {
            store.Save([_harness.NewAccount("A")]);
            seenInside = store.GetAll();
            File.Exists(_harness.FilePath("accounts")).Should().BeFalse("l'écriture a lieu à la validation");
            return 0;
        });

        seenInside.Should().ContainSingle();
        store.GetAll().Should().ContainSingle();
        File.Exists(_harness.FilePath("accounts")).Should().BeTrue();
    }

    [Fact] // TC-FUNC-08 : un rejet ne touche aucun fichier et ne crée aucune sauvegarde
    public async Task Failed_operation_writes_nothing()
    {
        var store = _harness.Store<Account>("accounts");
        await _harness.WriteAsync(store, [_harness.NewAccount("A")]);
        var before = await File.ReadAllTextAsync(_harness.FilePath("accounts"));

        var act = () => _harness.UnitOfWork.ExecuteAsync<int>(() =>
        {
            store.Save([]);
            throw new InvalidOperationException("règle métier violée");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await File.ReadAllTextAsync(_harness.FilePath("accounts"))).Should().Be(before);
        _harness.BackupFiles("accounts").Should().BeEmpty();
        store.GetAll().Should().ContainSingle();
    }

    [Fact]
    public async Task Nested_operation_joins_the_outer_one()
    {
        var store = _harness.Store<Account>("accounts");

        await _harness.UnitOfWork.ExecuteAsync(() =>
            _harness.UnitOfWork.ExecuteAsync(() => { store.Save([_harness.NewAccount("A")]); return 0; }).Result);

        store.GetAll().Should().ContainSingle();
    }

    [Fact] // TS §2.5 : les opérations sont sérialisées
    public async Task Concurrent_operations_do_not_lose_writes()
    {
        var store = _harness.Store<Account>("accounts");

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i => Task.Run(() =>
            _harness.UnitOfWork.ExecuteAsync(() =>
            {
                store.Save([.. store.GetAll(), _harness.NewAccount($"Compte {i}")]);
                return 0;
            }))));

        store.GetAll().Should().HaveCount(20);
        _harness.Store<Account>("accounts").GetAll().Should().HaveCount(20);
    }

    [Fact]
    public async Task Unsupported_schema_version_is_rejected()
    {
        Directory.CreateDirectory(_harness.DataPath);
        await File.WriteAllTextAsync(_harness.FilePath("accounts"), """{ "schemaVersion": 2, "items": [] }""");

        var act = () => _harness.Store<Account>("accounts").GetAll();

        act.Should().Throw<InvalidDataException>();
    }
}
