namespace Koras.Connect.Transport;

public static class EndpointSafety
{
    public static Uri ValidateRoot(Uri root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (!root.IsAbsoluteUri || root.Scheme != "https" || root.UserInfo.Length != 0 || root.Query.Length != 0 || root.Fragment.Length != 0)
            throw new ArgumentException("Root must be an absolute HTTPS URI without credentials, query or fragment.", nameof(root));
        return new Uri(root.AbsoluteUri.TrimEnd('/') + "/");
    }
    public static string Segment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." || value.Contains('/') || value.Contains('\\') || value.Contains('%'))
            throw new ArgumentException("A single unencoded identifier segment is required.", nameof(value));
        return Uri.EscapeDataString(value);
    }
    /// <summary>Confines server-provided pagination URLs to the configured origin and API path.</summary>
    public static Uri Continuation(Uri root, string link)
    {
        var next = new Uri(root, link);
        if (next.Scheme != root.Scheme || next.Host != root.Host || next.Port != root.Port || next.UserInfo.Length != 0 || next.Fragment.Length != 0
            || !next.AbsolutePath.StartsWith(root.AbsolutePath, StringComparison.Ordinal))
            throw new InvalidDataException("Continuation leaves the configured API boundary.");
        return next;
    }
}
