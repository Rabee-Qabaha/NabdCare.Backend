using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NabdCare.IntegrationTests.Helpers;

public static class HttpClientExtensions
{
    public static void SetBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<HttpResponseMessage> GetAuthenticatedAsync(
        this HttpClient client, 
        string url, 
        string token)
    {
        client.SetBearerToken(token);
        return await client.GetAsync(url);
    }

    public static async Task<HttpResponseMessage> PostAuthenticatedAsync<T>(
        this HttpClient client, 
        string url, 
        T data, 
        string token)
    {
        client.SetBearerToken(token);
        return await client.PostAsJsonAsync(url, data);
    }
}