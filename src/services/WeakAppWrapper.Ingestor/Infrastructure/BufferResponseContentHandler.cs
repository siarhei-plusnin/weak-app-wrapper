namespace WeakAppWrapper.Ingestor.Infrastructure;

/// <summary>
/// Buffers response bodies inside the HttpMessageHandler pipeline so resilience handlers can retry
/// failures that occur while reading a body, not just failures returned with the response headers.
/// </summary>
public sealed class BufferResponseContentHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
        HttpContent? originalContent = response.Content;

        if (originalContent is null)
        {
            return response;
        }

        try
        {
            byte[] content = await originalContent.ReadAsByteArrayAsync(cancellationToken);
            var bufferedContent = new ByteArrayContent(content);

            foreach (KeyValuePair<string, IEnumerable<string>> header in originalContent.Headers)
            {
                bufferedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.Content = bufferedContent;
            originalContent.Dispose();

            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }
}
