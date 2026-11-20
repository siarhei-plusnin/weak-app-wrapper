using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using WeakAppWrapper.Ingestor.Infrastructure;
using Xunit;

namespace WeakAppWrapper.Ingestor.Tests;

public sealed class BufferResponseContentHandlerTests
{
    [Fact]
    public async Task BodyReadFailureIsRetriedInsideTheResiliencePipeline()
    {
        var innerHandler = new FailFirstBodyReadHandler();
        var services = new ServiceCollection();
        services.AddTransient<BufferResponseContentHandler>();
        IHttpClientBuilder httpClient = services
            .AddHttpClient("body-retry")
            .ConfigurePrimaryHttpMessageHandler(() => innerHandler);
        httpClient.AddResilienceHandler(
            "body-retry",
            builder => builder.AddRetry(new HttpRetryStrategyOptions { MaxRetryAttempts = 1, Delay = TimeSpan.Zero })
        );
        httpClient.AddHttpMessageHandler<BufferResponseContentHandler>();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using HttpClient client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("body-retry");
        using var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/meters");

        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        string body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"ok\":true}", body);
        Assert.Equal(2, innerHandler.RequestCount);
    }

    private sealed class FailFirstBodyReadHandler : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => _requestCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            _requestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = _requestCount == 1 ? new TruncatedContent() : new StringContent("{\"ok\":true}", Encoding.UTF8);
            return Task.FromResult(response);
        }
    }

    private sealed class TruncatedContent : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.FromException(new HttpRequestException("simulated response truncation"));

        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context,
            CancellationToken cancellationToken
        ) => Task.FromException(new HttpRequestException("simulated response truncation"));

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
