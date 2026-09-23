using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceReport.Application.Dtos;
using FluentAssertions;
using Microsoft.IdentityModel.JsonWebTokens;

namespace FinanceReport.Api.IntegrationTests;

public sealed class AuthApiTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthApiTests() => _client = _factory.CreateClient();

    public void Dispose() => _factory.Dispose();

    [Fact] // TC-FUNC-15
    public async Task Access_account_can_be_initialized_only_once()
    {
        (await Status()).Should().BeFalse();

        var tooShort = await Setup("guillaume", "motdepasse1", "motdepasse1");
        await tooShort.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "password");

        var mismatch = await Setup("guillaume", ApiFactory.Password, "autre-mot-de-passe");
        await mismatch.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "passwordConfirmation");

        var shortName = await Setup("gc", ApiFactory.Password, ApiFactory.Password);
        await shortName.ShouldBeErrorAsync(HttpStatusCode.BadRequest, "VALIDATION_ERROR", "username");
        File.Exists(_factory.FilePath("credentials")).Should().BeFalse();

        var valid = await Setup(ApiFactory.Username, ApiFactory.Password, ApiFactory.Password);
        valid.StatusCode.Should().Be(HttpStatusCode.Created);
        (await valid.Content.ReadFromJsonAsync<SetupResponse>())!.Username.Should().Be(ApiFactory.Username);

        var file = await File.ReadAllTextAsync(_factory.FilePath("credentials"));
        JsonDocument.Parse(file).RootElement.GetProperty("passwordHash").GetString().Should().StartWith("$2a$12$");
        file.Should().NotContain(ApiFactory.Password);
        (await Status()).Should().BeTrue();

        var again = await Setup("autre", ApiFactory.Password, ApiFactory.Password);
        await again.ShouldBeErrorAsync(HttpStatusCode.Conflict, "CONFLICT");
        Directory.Exists(Path.Combine(_factory.DataPath, "backups", "credentials")).Should().BeFalse();
    }

    [Fact] // TC-TECH-01 : exp = iat + 28 800 s
    public async Task Login_issues_an_8_hour_token()
    {
        var token = new JsonWebToken(await ApiFactory.SetupAndLoginAsync(_client));

        token.Subject.Should().Be(ApiFactory.Username);
        token.Issuer.Should().Be("FinanceReport");
        token.Audiences.Should().Equal("FinanceReport");
        token.Alg.Should().Be("HS512");
        (token.ValidTo - token.IssuedAt).TotalSeconds.Should().Be(28_800);
    }

    [Theory] // UC-02
    [InlineData(ApiFactory.Username, "mauvais-mot-de-passe")]
    [InlineData("inconnu", ApiFactory.Password)]
    public async Task Wrong_credentials_are_rejected_with_a_generic_message(string username, string password)
    {
        await ApiFactory.SetupAndLoginAsync(_client);

        var response = await Login(username, password);

        var error = await response.ShouldBeErrorAsync(HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        error.GetProperty("message").GetString().Should().Be("Identifiant ou mot de passe incorrect");
    }

    [Fact] // TC-FUNC-16
    public async Task Five_consecutive_failures_lock_login_for_15_minutes()
    {
        await ApiFactory.SetupAndLoginAsync(_client);

        for (var i = 0; i < 4; i++)
        {
            (await Login(ApiFactory.Username, "faux")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        (await Login(ApiFactory.Username, ApiFactory.Password)).StatusCode.Should().Be(HttpStatusCode.OK, "4 échecs ne bloquent pas");

        for (var i = 0; i < 5; i++)
        {
            (await Login(ApiFactory.Username, "faux")).StatusCode.Should().Be(HttpStatusCode.Unauthorized, "le 5e échec renvoie 401");
        }

        var locked = await Login(ApiFactory.Username, ApiFactory.Password);
        var error = await locked.ShouldBeErrorAsync((HttpStatusCode)423, "ACCOUNT_LOCKED");
        error.GetProperty("details").GetProperty("retryAfterSeconds").GetInt32().Should().Be(900);

        _factory.Time.Advance(new TimeSpan(0, 14, 59));
        (await Login(ApiFactory.Username, ApiFactory.Password)).StatusCode.Should().Be((HttpStatusCode)423);

        _factory.Time.Advance(TimeSpan.FromSeconds(2));
        (await Login(ApiFactory.Username, ApiFactory.Password)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<bool> Status() =>
        (await _client.GetFromJsonAsync<AuthStatusResponse>("/api/auth/status"))!.Initialized;

    private Task<HttpResponseMessage> Setup(string username, string password, string confirmation) =>
        _client.PostAsJsonAsync("/api/auth/setup", new SetupRequest(username, password, confirmation));

    private Task<HttpResponseMessage> Login(string username, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
}
