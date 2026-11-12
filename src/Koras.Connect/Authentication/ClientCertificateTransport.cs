using System.Security.Cryptography.X509Certificates;

namespace Koras.Connect.Authentication;

public static class ClientCertificateTransport
{
    public static HttpClient Create(X509Certificate2 certificate, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        if (!certificate.HasPrivateKey) throw new ArgumentException("Client certificate must include a private key.", nameof(certificate));
        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler, disposeHandler: true) { Timeout = timeout ?? TimeSpan.FromSeconds(100) };
    }
    public static X509Certificate2 LoadPkcs12(string path, string? password = null)
        => X509CertificateLoader.LoadPkcs12FromFile(path, password);
    public static X509Certificate2 LoadPem(string certificatePath, string privateKeyPath)
        => X509Certificate2.CreateFromPemFile(certificatePath, privateKeyPath);
}
