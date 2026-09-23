using System.Net.Http.Json;
using System.Text.Json;
using FinanceReport.Infrastructure.Serialization;

namespace FinanceReport.Api.IntegrationTests;

/// <summary>Appels JSON avec les conventions de l'API (enums en majuscules, dates ISO).</summary>
internal static class ApiClientExtensions
{
    public static JsonSerializerOptions Json => JsonDefaults.Options;

    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public static async Task<T> PostJsonAsync<T>(this HttpClient client, string url, object? body = null)
    {
        var response = await client.PostAsJsonAsync(url, body ?? new { }, Json);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public static async Task<T> PutJsonAsync<T>(this HttpClient client, string url, object body)
    {
        var response = await client.PutAsJsonAsync(url, body, Json);
        await EnsureSuccessAsync(response);
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    public static Task<HttpResponseMessage> PostAsync(this HttpClient client, string url, object body) =>
        client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutAsync(this HttpClient client, string url, object body) =>
        client.PutAsJsonAsync(url, body, Json);

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"{(int)response.StatusCode} sur {response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} : " +
                await response.Content.ReadAsStringAsync());
        }
    }
}
