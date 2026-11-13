namespace Koras.Connect.Transport;

public interface IAccessTokenSource
{
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
