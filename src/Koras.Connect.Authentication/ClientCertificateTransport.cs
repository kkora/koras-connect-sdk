using System.Security.Cryptography.X509Certificates;

namespace Koras.Connect.Authentication;

public static class ClientCertificateTransport
{
    public static HttpClient Create(X509Certificate2 certificate, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (!certificate.HasPrivateKey) throw new ArgumentException("Client certificate must include a private key.", nameof(certificate));
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler, disposeHandler: true) { Timeout = timeout ?? TimeSpan.FromSeconds(100) };
    }
    public static X509Certificate2 LoadPkcs12(string path, string? password = null)
        => X509CertificateLoader.LoadPkcs12FromFile(path, password);
    /// <summary>Loads a PEM certificate/private-key pair for mTLS client authentication. On Windows,
    /// <see cref="X509Certificate2.CreateFromPemFile"/> alone returns an ephemeral in-memory key that SChannel
    /// cannot use for a TLS client-certificate handshake (it fails with "The credentials supplied to the package
    /// were not recognized" even though the certificate itself is valid and signing/export work fine) &#8212; so the
    /// result is re-imported with <see cref="X509KeyStorageFlags.PersistKeySet"/> to associate the private key with
    /// a persisted CNG key container that SChannel can present. This is the standard, documented .NET workaround
    /// for this Windows-specific limitation; it has one real side effect worth knowing about: the private key is
    /// written into the current user's Windows certificate/key store for as long as that key container exists,
    /// not only held in process memory. Callers needing to avoid that persistence on Windows should load a
    /// password-protected PKCS#12 bundle via <see cref="LoadPkcs12"/> instead.</summary>
    public static X509Certificate2 LoadPem(string certificatePath, string privateKeyPath)
    {
        using var ephemeral = X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
        return X509CertificateLoader.LoadPkcs12(ephemeral.Export(X509ContentType.Pkcs12), password: null,
            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
    }
}

