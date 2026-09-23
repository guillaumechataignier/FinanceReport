using System.Text.RegularExpressions;
using FinanceReport.Application.Tests.Support;
using FinanceReport.Domain.Entities;
using FluentAssertions;

namespace FinanceReport.Application.Tests.Persistence;

public sealed partial class BackupTests : IDisposable
{
    private readonly StorageHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact] // TC-TECH-02, étapes 1 et 2
    public async Task Backup_holds_the_state_before_modification()
    {
        var store = _harness.Store<Account>("accounts");
        var account = _harness.NewAccount("PEA");

        await _harness.WriteAsync(store, [account]);
        _harness.BackupFiles("accounts").Should().BeEmpty("le fichier n'existait pas");

        var original = await File.ReadAllTextAsync(_harness.FilePath("accounts"));
        _harness.Time.Advance(TimeSpan.FromMilliseconds(1));
        await _harness.WriteAsync(store, [account with { Name = "PEA Boursorama" }]);

        var backup = _harness.BackupFiles("accounts").Should().ContainSingle().Subject;
        Path.GetFileName(backup).Should().Be("accounts_20260923T070000001Z.json");
        (await File.ReadAllTextAsync(backup)).Should().Be(original);
    }

    [Fact] // TC-TECH-02, étape 3
    public async Task Only_the_100_most_recent_backups_are_kept()
    {
        var store = _harness.Store<Account>("accounts");
        var account = _harness.NewAccount("PEA");

        for (var i = 0; i < 107; i++)
        {
            await _harness.WriteAsync(store, [account with { Name = $"PEA {i}" }]);
            _harness.Time.Advance(TimeSpan.FromSeconds(1));
        }

        var backups = _harness.BackupFiles("accounts");
        backups.Should().HaveCount(100);
        backups.Should().AllSatisfy(f => BackupName().IsMatch(Path.GetFileName(f)).Should().BeTrue());
        // 106 sauvegardes prises (écritures 2 à 107, de 07:00:01 à 07:01:46) : les 6 plus anciennes sont purgées.
        Path.GetFileName(backups[0]).Should().Be("accounts_20260923T070007000Z.json");
        Path.GetFileName(backups[^1]).Should().Be("accounts_20260923T070146000Z.json");
    }

    [Fact]
    public async Task Backups_taken_in_the_same_millisecond_do_not_collide()
    {
        var store = _harness.Store<Account>("accounts");

        for (var i = 0; i < 3; i++)
        {
            await _harness.WriteAsync(store, [_harness.NewAccount($"PEA {i}")]);
        }

        _harness.BackupFiles("accounts").Select(Path.GetFileName).Should().Equal(
            "accounts_20260923T070000000Z.json",
            "accounts_20260923T070000000Z_001.json");
    }

    [GeneratedRegex(@"^accounts_\d{8}T\d{9}Z\.json$")]
    private static partial Regex BackupName();
}
