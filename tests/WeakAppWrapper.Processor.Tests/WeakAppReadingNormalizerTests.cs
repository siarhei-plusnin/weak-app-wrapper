using System.Text.Json;
using WeakAppWrapper.Contracts.Messages;
using WeakAppWrapper.Processor.Application;
using Xunit;

namespace WeakAppWrapper.Processor.Tests;

public sealed class WeakAppReadingNormalizerTests
{
    private static readonly int[] ExpectedItemIndexes = [0, 1];

    [Fact]
    public void NormalizePreservesEnvelopeIdentityAndPayloadItemOrder()
    {
        DateTimeOffset fetchedAt = new(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
        WeakAppMetersPolledMessage message = CreateMessage(
            """
            [
              {"type":"energy","name":"Office","payload":{"energy":123.45}},
              {"type":"motion","name":"Kitchen","payload":{"motionDetected":true}}
            ]
            """,
            fetchedAt
        );

        IReadOnlyList<NormalizedReading> readings = WeakAppReadingNormalizer.Normalize(message);

        Assert.Equal(2, readings.Count);
        Assert.All(readings, reading => Assert.Equal(message.MessageId, reading.SourceMessageId));
        Assert.Equal(ExpectedItemIndexes, readings.Select(reading => reading.ItemIndex));
        Assert.Equal("weakapp", readings[0].Source);
        Assert.Equal("energy", readings[0].Type);
        Assert.Equal(123.45, readings[0].Payload.GetProperty("energy").GetDouble());
        Assert.Equal("energy", readings[0].RawRecord.GetProperty("type").GetString());
        Assert.Equal("Office", readings[0].RawRecord.GetProperty("name").GetString());
        Assert.Equal(fetchedAt, readings[0].CapturedAt);
        Assert.Equal(JsonValueKind.True, readings[1].Payload.GetProperty("motionDetected").ValueKind);
    }

    [Theory]
    [InlineData("{\"type\":17,\"name\":\"Office\",\"payload\":{}}")]
    [InlineData("{\"type\":\"energy\",\"name\":false,\"payload\":{}}")]
    [InlineData("{\"type\":\" \",\"name\":\"Office\",\"payload\":{}}")]
    [InlineData("{\"type\":\"energy\",\"name\":\" \",\"payload\":{}}")]
    public void NormalizeRejectsNonStringOrBlankTypeAndName(string meter)
    {
        WeakAppMetersPolledMessage message = CreateMessage($"[{meter}]", DateTimeOffset.UtcNow);

        Assert.Throws<WeakAppPayloadException>(() => WeakAppReadingNormalizer.Normalize(message));
    }

    [Fact]
    public void NormalizeRejectsNonArrayPayload()
    {
        WeakAppMetersPolledMessage message = CreateMessage("{}", DateTimeOffset.UtcNow);

        Assert.Throws<WeakAppPayloadException>(() => WeakAppReadingNormalizer.Normalize(message));
    }

    [Fact]
    public void NormalizeRejectsMissingMessageIdOrFetchTimestamp()
    {
        WeakAppMetersPolledMessage missingId = CreateMessage("[]", DateTimeOffset.UtcNow, messageId: " ");
        WeakAppMetersPolledMessage missingFetchedAt = CreateMessage("[]", default);

        Assert.Throws<WeakAppPayloadException>(() => WeakAppReadingNormalizer.Normalize(missingId));
        Assert.Throws<WeakAppPayloadException>(() => WeakAppReadingNormalizer.Normalize(missingFetchedAt));
    }

    [Fact]
    public void ProcessedEventSerializesPayloadAsAnObject()
    {
        using JsonDocument payloadDocument = JsonDocument.Parse("{\"energy\":123.45}");
        var message = new ProcessedReadingMessage(
            1,
            "event-123",
            "poll-123",
            Guid.NewGuid(),
            2,
            "weakapp",
            "energy",
            "Office",
            "Office",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            payloadDocument.RootElement.Clone()
        );

        using JsonDocument serializedMessage = JsonDocument.Parse(JsonSerializer.Serialize(message));
        JsonElement root = serializedMessage.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("event-123", root.GetProperty("messageId").GetString());
        Assert.Equal("poll-123", root.GetProperty("sourceMessageId").GetString());
        Assert.Equal(2, root.GetProperty("itemIndex").GetInt32());
        Assert.Equal("Office", root.GetProperty("locationName").GetString());
        Assert.Equal(JsonValueKind.Object, root.GetProperty("payload").ValueKind);
        Assert.Equal(123.45, root.GetProperty("payload").GetProperty("energy").GetDouble());
    }

    [Fact]
    public void ProcessedEventReadsLegacyMessagesWithoutPayload()
    {
        const string legacyMessage = """
            {"messageId":"event-old","sourceMessageId":"poll-old","readingId":"11111111-1111-1111-1111-111111111111","source":"weakapp","type":"energy","name":"Office","observedAt":"2026-11-19T12:00:00Z","receivedAt":"2026-11-19T12:00:00Z","processedAt":"2026-11-19T12:00:01Z"}
            """;

        ProcessedReadingMessage message = JsonSerializer.Deserialize<ProcessedReadingMessage>(legacyMessage)!;

        Assert.Equal(0, message.SchemaVersion);
        Assert.Null(message.Payload);
    }

    private static WeakAppMetersPolledMessage CreateMessage(
        string payload,
        DateTimeOffset fetchedAt,
        string messageId = "poll-123",
        string source = "weakapp"
    )
    {
        using JsonDocument payloadDocument = JsonDocument.Parse(payload);
        return new WeakAppMetersPolledMessage(messageId, source, fetchedAt, payloadDocument.RootElement.Clone());
    }
}
