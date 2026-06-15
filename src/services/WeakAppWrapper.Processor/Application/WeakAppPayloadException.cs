namespace WeakAppWrapper.Processor.Application;

public sealed class WeakAppPayloadException : Exception
{
    public WeakAppPayloadException() { }

    public WeakAppPayloadException(string message)
        : base(message) { }

    public WeakAppPayloadException(string message, Exception innerException)
        : base(message, innerException) { }
}
