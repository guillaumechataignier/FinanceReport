using System.Net;
using FinanceReport.Application.Dtos;
using FinanceReport.Domain.Enums;
using FluentAssertions;

namespace FinanceReport.Api.IntegrationTests;

public sealed class MovementApiTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();
    private HttpClient _client = null!;
    private ReferenceDataSet _data = null!;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
        _data = await ReferenceDataSet.LoadAsync(_client);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Created_movement_is_returned_with_names_total_and_sequence()
    {
        var m1 = await _client.GetJsonAsync<MovementDto>($"/api/movements/{_data.M1}");
        var m3 = await _client.GetJsonAsync<MovementDto>($"/api/movements/{_data.M3}");

        m1.Type.Should().Be(MovementType.Achat);
        m1.AccountName.Should().Be("PEA Test");
        m1.SecurityName.Should().Be("ETF Monde");
        m1.SecurityCode.Should().Be("LU1681043599");
        m1.Total.Should().Be(1002.00m);
        m1.Amount.Should().BeNull();
        m3.Total.Should().Be(718.50m);
        m3.Sequence.Should().BeGreaterThan(m1.Sequence);
    }

    [Fact] // TC-FUNC-27
    public async Task History_is_filtered_and_sorted_newest_first()
    {
        (await List("?type=ACHAT")).Select(m => m.Id).Should().Equal(_data.M2, _data.M1);
        (await List("?from=2026-02-01&to=2026-03-31")).Select(m => m.Id).Should().Equal(_data.M3, _data.M2);
        (await List($"?securityId={_data.S2}")).Should().BeEmpty();
        (await List($"?accountId={_data.A}")).Should().HaveCount(3);
        (await List("")).Select(m => m.Id).Should().Equal(_data.M3, _data.M2, _data.M1);
    }

    [Fact] // TC-FUNC-08
    public async Task Sale_above_held_quantity_is_rejected_without_any_write()
    {
        var movementsBefore = await File.ReadAllTextAsync(_factory.FilePath("movements"));
        var backupsBefore = Backups("movements");

        var response = await PostTrade("VENTE", "2026-09-20", 10m, 115m);

        var error = await response.ShouldBeErrorAsync((HttpStatusCode)422, "INSUFFICIENT_QUANTITY");
        error.GetProperty("details").GetProperty("availableQuantity").GetDecimal().Should().Be(9m);
        error.GetProperty("message").GetString().Should().Be("Quantité insuffisante : 9.00000000 disponibles au 2026-09-20");
        (await File.ReadAllTextAsync(_factory.FilePath("movements"))).Should().Be(movementsBefore);
        Backups("movements").Should().Be(backupsBefore);
    }

    [Fact] // TC-FUNC-08, variante : même date
    public async Task Same_day_sale_depends_on_creation_order()
    {
        (await PostTrade("VENTE", "2026-09-20", 11m, 115m)).StatusCode.Should().Be((HttpStatusCode)422, "la vente saisie seule est excédentaire");

        (await PostTrade("ACHAT", "2026-09-20", 2m, 115m)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await PostTrade("VENTE", "2026-09-20", 11m, 115m)).StatusCode.Should().Be(HttpStatusCode.Created, "l'achat a été créé avant la vente");
    }

    [Fact] // TC-FUNC-12
    public async Task Deleting_an_earlier_purchase_that_breaks_a_sale_is_rejected()
    {
        var movementsBefore = await File.ReadAllTextAsync(_factory.FilePath("movements"));
        var snapshotsBefore = await File.ReadAllTextAsync(_factory.FilePath("snapshots"));

        var response = await _client.DeleteAsync($"/api/movements/{_data.M1}");

        var error = await response.ShouldBeErrorAsync((HttpStatusCode)422, "INSUFFICIENT_QUANTITY");
        error.GetProperty("details").GetProperty("conflictingMovementId").GetGuid().Should().Be(_data.M3);
        error.GetProperty("details").GetProperty("availableQuantity").GetDecimal().Should().Be(5m);
        (await File.ReadAllTextAsync(_factory.FilePath("movements"))).Should().Be(movementsBefore);
        (await File.ReadAllTextAsync(_factory.FilePath("snapshots"))).Should().Be(snapshotsBefore);
    }

    [Fact] // UC-08
    public async Task Movement_can_be_updated_and_deleted()
    {
        var updated = await _client.PutJsonAsync<MovementDto>(
            $"/api/movements/{_data.M3}",
            new { type = "VENTE", date = "2026-03-10", accountId = _data.A, securityId = _data.S1, quantity = 5m, unitPrice = 120.00m, fees = 1.50m });
        updated.Quantity.Should().Be(5m);
        updated.Sequence.Should().Be((await _client.GetJsonAsync<MovementDto>($"/api/movements/{_data.M3}")).Sequence);
        (await _client.GetJsonAsync<AccountDto>($"/api/accounts/{_data.A}")).CurrentValue.Should().Be(10m * 115m + 200m);

        (await _client.DeleteAsync($"/api/movements/{_data.M3}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await List("")).Should().HaveCount(2);
        await (await _client.GetAsync($"/api/movements/{_data.M3}")).ShouldBeErrorAsync(HttpStatusCode.NotFound, "NOT_FOUND");
    }

    [Fact] // UC-08 : modification qui rend une vente ultérieure excédentaire
    public async Task Update_that_breaks_a_later_sale_is_rejected()
    {
        // M1 déplacé après la vente : au 10/03, seuls les 5 titres de M2 sont détenus pour 6 vendus.
        var response = await _client.PutAsync(
            $"/api/movements/{_data.M1}",
            new { type = "ACHAT", date = "2026-04-10", accountId = _data.A, securityId = _data.S1, quantity = 10m, unitPrice = 100m, fees = 2m });

        (await response.ShouldBeErrorAsync((HttpStatusCode)422, "INSUFFICIENT_QUANTITY"))
            .GetProperty("details").GetProperty("conflictingMovementId").GetGuid().Should().Be(_data.M3);
    }

    [Fact]
    public async Task Movement_type_cannot_be_changed()
    {
        var response = await _client.PutAsync(
            $"/api/movements/{_data.M1}",
            new { type = "VENTE", date = "2026-01-10", accountId = _data.A, securityId = _data.S1, quantity = 1m, unitPrice = 100m, fees = 0m });

        await response.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "type");
    }

    [Theory] // TC-FUNC-02 (a à d)
    [InlineData("S1", "1.123456789", "100", "0", HttpStatusCode.BadRequest, "quantity")]
    [InlineData("S1", "1.12345678", "100.123", "0", HttpStatusCode.BadRequest, "unitPrice")]
    [InlineData("S2", "0.00012345", "58000.12345678", "0", HttpStatusCode.Created, null)]
    [InlineData("S1", "1", "100", "1.999", HttpStatusCode.BadRequest, "fees")]
    public async Task Precision_depends_on_field_and_security_type(
        string security, string quantity, string unitPrice, string fees, HttpStatusCode expected, string? field)
    {
        var response = await _client.PostAsync("/api/movements", new
        {
            type = "ACHAT",
            date = "2026-09-15",
            accountId = _data.A,
            securityId = security == "S1" ? _data.S1 : _data.S2,
            quantity = decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture),
            unitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture),
            fees = decimal.Parse(fees, System.Globalization.CultureInfo.InvariantCulture),
        });

        if (field is null)
        {
            response.StatusCode.Should().Be(expected);
        }
        else
        {
            await response.ShouldBeErrorAsync(expected, "VALIDATION_ERROR", field);
        }
    }

    [Fact] // TC-FUNC-10, étape 3
    public async Task Archived_security_cannot_receive_a_new_movement()
    {
        await _client.PostJsonAsync<SecurityDto>($"/api/securities/{_data.S1}/archive");

        await (await PostTrade("ACHAT", "2026-09-15", 1m, 100m)).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "securityId");
        (await List("")).Should().HaveCount(3, "les mouvements existants restent visibles");
    }

    [Fact] // TC-FUNC-17, étapes 1 et 2
    public async Task Movements_are_only_allowed_on_securities_accounts()
    {
        await (await _client.PostAsync("/api/movements", new { type = "ACHAT", date = "2026-09-15", accountId = _data.B, securityId = _data.S1, quantity = 1m, unitPrice = 100m, fees = 0m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "accountId");
        await (await _client.PostAsync("/api/movements", new { type = "VERSEMENT", date = "2026-09-15", accountId = _data.B, amount = 100m }))
            .ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "accountId");
    }

    [Fact] // TC-FUNC-18
    public async Task Deposit_is_traced_but_does_not_change_cash()
    {
        var deposit = await _client.PostJsonAsync<MovementDto>(
            "/api/movements", new { type = "VERSEMENT", date = "2026-09-20", accountId = _data.A, amount = 1000.00m });

        deposit.Total.Should().Be(1000.00m);
        deposit.SecurityId.Should().BeNull();
        var a = await _client.GetJsonAsync<AccountDto>($"/api/accounts/{_data.A}");
        a.Cash.Should().Be(200.00m);
        a.CurrentValue.Should().Be(1235.00m);
        (await List("?type=VERSEMENT")).Should().ContainSingle();
    }

    [Theory] // FS §3.4 : champs interdits et obligatoires selon le type
    [InlineData("""{ "type": "VERSEMENT", "date": "2026-09-15", "accountId": "{A}", "amount": 10, "quantity": 1 }""", "quantity")]
    [InlineData("""{ "type": "RETRAIT", "date": "2026-09-15", "accountId": "{A}" }""", "amount")]
    [InlineData("""{ "type": "ACHAT", "date": "2026-09-15", "accountId": "{A}", "securityId": "{S1}", "quantity": 1, "unitPrice": 10, "amount": 10 }""", "amount")]
    [InlineData("""{ "type": "ACHAT", "date": "2026-09-15", "accountId": "{A}", "quantity": 1, "unitPrice": 10 }""", "securityId")]
    [InlineData("""{ "type": "ACHAT", "date": "2026-09-15", "accountId": "{A}", "securityId": "{S1}", "quantity": 0, "unitPrice": 10 }""", "quantity")]
    [InlineData("""{ "type": "VENTE", "date": "2026-09-15", "accountId": "{A}", "securityId": "{S1}", "quantity": 1, "unitPrice": 10, "fees": -1 }""", "fees")]
    [InlineData("""{ "date": "2026-09-15", "accountId": "{A}", "amount": 10 }""", "type")]
    public async Task Fields_depend_on_movement_type(string template, string field)
    {
        var json = template.Replace("{A}", _data.A.ToString()).Replace("{S1}", _data.S1.ToString());

        var response = await _client.PostAsync("/api/movements", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        await response.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", field);
    }

    [Fact] // TC-FUNC-21 (mouvement)
    public async Task Future_movement_is_rejected()
    {
        await (await PostTrade("ACHAT", "2026-09-24", 1m, 100m)).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "date");
        (await PostTrade("ACHAT", "2026-09-23", 1m, 100m)).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact] // TC-FUNC-23, étape 4
    public async Task Archived_account_cannot_receive_a_movement()
    {
        await _client.PostJsonAsync<AccountDto>($"/api/accounts/{_data.A}/archive");

        await (await PostTrade("ACHAT", "2026-09-15", 1m, 100m)).ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "accountId");
        (await List($"?accountId={_data.A}")).Should().HaveCount(3, "A reste visible dans l'historique");
    }

    private Task<List<MovementDto>> List(string query) => _client.GetJsonAsync<List<MovementDto>>($"/api/movements{query}");

    private Task<HttpResponseMessage> PostTrade(string type, string date, decimal quantity, decimal unitPrice) =>
        _client.PostAsync("/api/movements", new { type, date, accountId = _data.A, securityId = _data.S1, quantity, unitPrice, fees = 0m });

    private int Backups(string entity)
    {
        var directory = Path.Combine(_factory.DataPath, "backups", entity);
        return Directory.Exists(directory) ? Directory.GetFiles(directory).Length : 0;
    }
}
