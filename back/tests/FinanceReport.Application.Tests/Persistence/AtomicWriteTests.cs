using FinanceReport.Application.Tests.Support;
using FinanceReport.Domain.Entities;
using FluentAssertions;

namespace FinanceReport.Application.Tests.Persistence;

/// <summary>
/// TC-TECH-04 au niveau du stockage. La faute est injectée en plaçant un dossier à l'emplacement du fichier
/// temporaire de <c>snapshots.json</c>, ce qui fait échouer son écriture après celle de <c>movements.json</c>.
/// </summary>
public sealed class AtomicWriteTests : IDisposable
{
    private readonly StorageHarness _harness = new();

    public void Dispose() => _harness.Dispose();

    [Fact]
    public async Task Failure_on_second_file_restores_the_first_and_reloads_caches()
    {
        var movements = _harness.Store<Account>("movements");
        var snapshots = _harness.Store<Account>("snapshots");
        await _harness.WriteAsync(movements, [_harness.NewAccount("initial")]);
        var initialContent = await File.ReadAllTextAsync(_harness.FilePath("movements"));
        Directory.CreateDirectory(_harness.FilePath("snapshots") + ".tmp");
        _harness.Time.Advance(TimeSpan.FromMilliseconds(1));

        var act = () => _harness.UnitOfWork.ExecuteAsync(() =>
        {
            movements.Save([.. movements.GetAll(), _harness.NewAccount("nouveau")]);
            snapshots.Save([_harness.NewAccount("snapshot")]);
            return 0;
        });

        await act.Should().ThrowAsync<Exception>();
        (await File.ReadAllTextAsync(_harness.FilePath("movements"))).Should().Be(initialContent);
        movements.GetAll().Should().ContainSingle().Which.Name.Should().Be("initial");
        File.Exists(_harness.FilePath("snapshots")).Should().BeFalse();
        File.Exists(_harness.FilePath("movements") + ".tmp").Should().BeFalse();
    }

    [Fact]
    public async Task Failure_deletes_a_file_created_by_the_operation()
    {
        var movements = _harness.Store<Account>("movements");
        var snapshots = _harness.Store<Account>("snapshots");
        Directory.CreateDirectory(_harness.FilePath("snapshots") + ".tmp");

        var act = () => _harness.UnitOfWork.ExecuteAsync(() =>
        {
            movements.Save([_harness.NewAccount("nouveau")]);
            snapshots.Save([_harness.NewAccount("snapshot")]);
            return 0;
        });

        await act.Should().ThrowAsync<Exception>();
        File.Exists(_harness.FilePath("movements")).Should().BeFalse();
        movements.GetAll().Should().BeEmpty();
    }

    [Fact]
    public async Task Store_version_changes_on_each_commit()
    {
        var store = _harness.Store<Account>("accounts");
        var initial = store.Version;

        await _harness.WriteAsync(store, [_harness.NewAccount("A")]);

        store.Version.Should().BeGreaterThan(initial);
    }
}
