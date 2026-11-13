namespace Koras.Connect.Transport;

/// <summary>Applies credentials immediately before sending. Implementations must not log or retain request data.</summary>
public interface IRequestAuthenticator
{
    ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
