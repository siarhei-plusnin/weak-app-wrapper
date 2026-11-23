namespace WeakAppWrapper.Persistence.Domain;

public sealed class Reading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Source { get; set; } = string.Empty;
    public string SourceMessageId { get; set; } = string.Empty;
    public int ItemIndex { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RawRecord { get; set; } = string.Empty;
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset ProcessorReceivedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
