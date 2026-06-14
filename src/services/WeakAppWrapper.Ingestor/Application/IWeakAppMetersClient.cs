namespace WeakAppWrapper.Ingestor.Application;

public interface IWeakAppMetersClient
{
    Task<string> QueryMetersAsync(CancellationToken cancellationToken);
}
