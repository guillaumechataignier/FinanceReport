using System.Net;
using System.Text.Json;
using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class AccountApiTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;
    private ReferenceDataSet _data = null!;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
        _data = await ReferenceDataSet.LoadAsync(_factory, _client);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact] // TC-TECH-01, étapes 1 et 2
    public async Task Accounts_require_a_token()
    {
        await (await _factory.CreateClient().GetAsync("/api/accounts")).ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        (await _client.GetAsync("/api/accounts")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact] // Ajout validé : valeur actuelle et dernier solde sur la liste des comptes
    public async Task List_shows_current_value_and_hides_archived_accounts_by_default()
    {
        var active = await _client.GetJsonAsync<List<AccountDto>>("/api/accounts");
        var all = await _client.GetJsonAsync<List<AccountDto>>("/api/accounts?includeArchived=true");

        active.Select(a => a.Id).Should().BeEquivalentTo([_data.A, _data.B]);
        var a = active.Single(x => x.Id == _data.A);
        a.Category.Should().Be(AccountCategory.Titres);
        a.InstitutionName.Should().Be("Boursorama");
        a.CurrentValue.Should().Be(1235.00m);
        a.Cash.Should().Be(200.00m);
        a.CashDate.Should().Be(new DateOnly(2026, 9, 1));
        active.Single(x => x.Id == _data.B).CurrentValue.Should().Be(5000.00m);

        var c = all.Single(x => x.Id == _data.C);
        c.Archived.Should().BeTrue();
        c.CurrentValue.Should().Be(1000.00m, "la valeur d'un compte archivé reste affichée, hors patrimoine");
        all.Last().Id.Should().Be(_data.C, "les comptes archivés sont listés en dernier");
    }

    [Fact] // TC-FUNC-22
    public async Task Account_name_must_be_unique()
    {
        await (await _client.PostAsync("/api/accounts", new { name = "Livret A", type = "LIVRET", institutionId = _data.E2 }))
            .ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
        await (await _client.PostAsync("/api/accounts", new { name = " livret a ", type = "LIVRET", institutionId = _data.E2 }))
            .ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
    }

    [Fact] // TC-FUNC-17, étapes 3 et 4
    public async Task Account_type_is_frozen_once_the_account_has_data()
    {
        await (await _client.PutAsync($"/api/accounts/{_data.A}", new { name = "PEA Test", type = "CTO", institutionId = _data.E1 }))
            .ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");

        var d = await _client.PostJsonAsync<AccountDto>("/api/accounts", new { name = "D", type = "CTO", institutionId = _data.E1 });
        var updated = await _client.PutJsonAsync<AccountDto>($"/api/accounts/{d.Id}", new { name = "D", type = "PEA", institutionId = _data.E1 });
        updated.Type.Should().Be(AccountType.Pea);
    }

    [Theory]
    [InlineData("""{ "name": "D", "type": "PLAN", "institutionId": null }""", "type")]
    [InlineData("""{ "name": "", "type": "CTO", "institutionId": "00000000-0000-0000-0000-000000000001" }""", "name")]
    [InlineData("""{ "name": "D", "institutionId": "00000000-0000-0000-0000-000000000001" }""", "type")]
    public async Task Invalid_account_requests_are_rejected(string json, string field)
    {
        var response = await _client.PostAsync("/api/accounts", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await response.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", field);
    }

    [Fact] // TC-FUNC-23, étapes 1 et 3 (le patrimoine est vérifié en phase 6)
    public async Task Archived_account_can_be_unarchived()
    {
        var c = await _client.PostJsonAsync<AccountDto>($"/api/accounts/{_data.C}/unarchive");
        c.Archived.Should().BeFalse();
        (await _client.GetJsonAsync<List<AccountDto>>("/api/accounts")).Should().Contain(a => a.Id == _data.C);

        await _client.PostJsonAsync<AccountDto>($"/api/accounts/{_data.A}/archive");
        (await _client.GetJsonAsync<List<AccountDto>>("/api/accounts")).Should().NotContain(a => a.Id == _data.A);
    }

    [Fact] // UC-04 : upsert sur (compte, date), historique décroissant
    public async Task Balance_is_upserted_and_listed_newest_first()
    {
        await _client.PutJsonAsync<BalanceDto>($"/api/accounts/{_data.B}/balances/2026-09-15", new { amount = 5100.50m });
        var replaced = await _client.PutJsonAsync<BalanceDto>($"/api/accounts/{_data.B}/balances/2026-09-15", new { amount = -12.34m });

        replaced.Amount.Should().Be(-12.34m, "un solde négatif est autorisé");
        var history = await _client.GetJsonAsync<List<BalanceDto>>($"/api/accounts/{_data.B}/balances");
        history.Select(h => (h.Date, h.Amount)).Should().Equal(
            (new DateOnly(2026, 9, 15), -12.34m),
            (new DateOnly(2026, 9, 1), 5000.00m));
        (await _client.GetJsonAsync<AccountDto>($"/api/accounts/{_data.B}")).CurrentValue.Should().Be(-12.34m);
    }

    [Fact] // Ajout validé : suppression d'un solde
    public async Task Balance_can_be_deleted()
    {
        (await _client.DeleteAsync($"/api/accounts/{_data.A}/balances/2026-09-01")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.GetJsonAsync<AccountDto>($"/api/accounts/{_data.A}")).CurrentValue.Should().Be(1035.00m);
        await (await _client.DeleteAsync($"/api/accounts/{_data.A}/balances/2026-09-01")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Fact] // TC-FUNC-02 (g) et TC-FUNC-21 (solde)
    public async Task Balance_precision_and_date_are_checked()
    {
        await (await _client.PutAsync($"/api/accounts/{_data.B}/balances/2026-09-20", new { amount = 100.001m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "amount");
        await (await _client.PutAsync($"/api/accounts/{_data.B}/balances/2026-09-24", new { amount = 100m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "date");
        (await _client.PutAsync($"/api/accounts/{_data.B}/balances/2026-09-23", new { amount = 100m }))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        await (await _client.PutAsync($"/api/accounts/{_data.B}/balances/23-09-2026", new { amount = 100m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact] // RG-27 : pas de saisie sur un compte archivé
    public async Task Balance_on_archived_account_is_rejected()
    {
        await (await _client.PutAsync($"/api/accounts/{_data.C}/balances/2026-09-20", new { amount = 10m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "accountId");
    }

    [Fact] // RG-13, RG-14 : chaque écriture met à jour le snapshot du jour
    public async Task Balance_write_updates_today_snapshot()
    {
        await _client.PutJsonAsync<BalanceDto>($"/api/accounts/{_data.B}/balances/2026-09-23", new { amount = 6000m });

        using var snapshots = JsonDocument.Parse(await File.ReadAllTextAsync(_factory.FilePath("snapshots")));
        var today = snapshots.RootElement.GetProperty("items").EnumerateArray()
            .Single(s => s.GetProperty("date").GetString() == "2026-09-23");
        today.GetProperty("totalNetWorth").GetDecimal().Should().Be(7235m);
    }

    [Fact] // TC-TECH-02 par l'API
    public async Task Account_writes_keep_the_100_most_recent_backups()
    {
        var accountsBackups = Path.Combine(_factory.DataPath, "backups", "accounts");
        var backupsBefore = Directory.GetFiles(accountsBackups).Length;

        _factory.Time.Advance(TimeSpan.FromMilliseconds(1));
        var before = await File.ReadAllTextAsync(_factory.FilePath("accounts"));
        await _client.PutJsonAsync<AccountDto>($"/api/accounts/{_data.B}", new { name = "Livret A bis", type = "LIVRET", institutionId = _data.E2 });
        var newest = Directory.GetFiles(accountsBackups).Order(StringComparer.Ordinal).Last();
        (await File.ReadAllTextAsync(newest)).Should().Be(before, "la sauvegarde contient l'état avant modification");
        Directory.GetFiles(accountsBackups).Should().HaveCount(backupsBefore + 1);

        for (var i = 0; i < 105; i++)
        {
            _factory.Time.Advance(TimeSpan.FromMilliseconds(1));
            await _client.PutJsonAsync<AccountDto>($"/api/accounts/{_data.B}", new { name = $"Livret A {i}", type = "LIVRET", institutionId = _data.E2 });
        }

        Directory.GetFiles(accountsBackups).Should().HaveCount(100);
        Directory.Exists(Path.Combine(_factory.DataPath, "backups", "credentials")).Should().BeFalse();
    }
}
