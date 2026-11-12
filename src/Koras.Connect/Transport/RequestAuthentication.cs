using System.Net.Http.Headers;
namespace Koras.Connect.Transport;

/// <summary>Applies credentials immediately before sending. Implementations must not log or retain request data.</summary>
public interface IRequestAuthenticator
{
    ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default);
}
public sealed class BearerTokenAuthenticator(IAccessTokenSource tokens) : IRequestAuthenticator
{
    private readonly IAccessTokenSource _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    public async ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
        => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokens.GetTokenAsync(cancellationToken));
}
/// <summary>Header-based API token auth with a caller-owned source for rotation. Never emits bearer authorization.</summary>
public sealed class HeaderTokenAuthenticator : IRequestAuthenticator
{
    private readonly string _header;
    private readonly IAccessTokenSource _tokens;
    public HeaderTokenAuthenticator(string header, IAccessTokenSource tokens)
    {
        if (string.IsNullOrWhiteSpace(header) || !header.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
            || header.Equals("Host", StringComparison.OrdinalIgnoreCase) || header.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A nonreserved token header is required.", nameof(header));
        _header = header; _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }
    public async ValueTask AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var token = await _tokens.GetTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(token) || token.Any(c => c is '\r' or '\n')) throw new InvalidOperationException("Invalid API token.");
        request.Headers.Add(_header, token);
    }
}
