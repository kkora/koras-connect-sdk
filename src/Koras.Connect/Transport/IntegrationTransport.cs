using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Koras.Connect.Transport;

public sealed class IntegrationRequestException(string message, HttpStatusCode statusCode) : HttpRequestException(message, null, statusCode)
{
    public HttpStatusCode ResponseStatus { get; } = statusCode;
}

public interface IAccessTokenSource
{
    ValueTask<string> GetTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class IntegrationTransport(HttpClient httpClient, IAccessTokenSource tokens)
{
    public async Task<JsonDocument> ReadJsonAsync(Uri address, IReadOnlyDictionary<string, string>? headers = null, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await tokens.GetTokenAsync(cancellationToken));
        if (headers is not null)
            foreach (var header in headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new IntegrationRequestException($"Integration endpoint returned {(int)response.StatusCode}.", response.StatusCode);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }
}
