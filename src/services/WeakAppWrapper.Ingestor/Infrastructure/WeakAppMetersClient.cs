using WeakAppWrapper.Ingestor.Application;

namespace WeakAppWrapper.Ingestor.Infrastructure;

public sealed class WeakAppMetersClient(IHttpClientFactory httpClientFactory) : IWeakAppMetersClient
{
    internal const string HttpClientName = "WeakApp";

    public async Task<string> QueryMetersAsync(CancellationToken cancellationToken)
    {
        using HttpClient httpClient = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Get, "meters");

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"WeakApp returned non-success status {(int)response.StatusCode} ({response.StatusCode}): {errorBody}",
                null,
                response.StatusCode
            );
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
