using System.Runtime.CompilerServices;
using Koras.Connect.Adp;
using Koras.Connect.Adp.Export;
using Koras.Connect.Adp.Workers;
using Koras.Connect.Authentication;
using Koras.Connect.Export;
using Koras.Connect.Transport;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>ADP-020: opt-in sandbox/live-tenant checks, formalizing "opt-in sandbox testing" without requiring real
/// credentials to exist in CI or in this offline development environment.
///
/// This test is skipped using xunit's built-in <see cref="FactAttribute.Skip"/> mechanism, which every xunit
/// runner (including this repository's xunit.runner.visualstudio) reports as a distinct "Skipped" result with the
/// given reason string &#8212; never silently "Passed" and never simply absent from the run. A fully dynamic,
/// per-run skip driven by reading the KORAS_ADP_SANDBOX_* environment variables at test-execution time was
/// evaluated (via xunit's documented "$XunitDynamicSkip$"/<c>Xunit.Sdk.SkipException.ForSkip</c> convention) and
/// found to report as a hard Failure rather than Skipped under this repository's exact xunit 2.9.3 +
/// xunit.runner.visualstudio 3.1.4 combination; using it here would break the test run instead of skipping it.
/// The static <see cref="FactAttribute.Skip"/> gate below is the verified-working alternative. The required
/// environment variables are still documented so a future maintainer can wire a true live-tenant check (removing
/// this attribute and recording tenant/grant evidence in docs/features/adp/ADP-020.md) once real sandbox
/// credentials and a dynamic-skip-capable runner are both available.</summary>
[Trait("Category", "Sandbox")]
public sealed class AdpSandboxTests
{
    public const string TenantVar = "KORAS_ADP_SANDBOX_TENANT";
    public const string ClientIdVar = "KORAS_ADP_SANDBOX_CLIENT_ID";
    public const string ClientSecretVar = "KORAS_ADP_SANDBOX_CLIENT_SECRET";

    /// <summary>Public certificate path. Either a PEM file paired with <see cref="KeyPathVar"/>, or a standalone
    /// PKCS#12 (.pfx/.p12) bundle used alone (optionally with <see cref="CertPasswordVar"/>).</summary>
    public const string CertPathVar = "KORAS_ADP_SANDBOX_CERT_PATH";

    /// <summary>Private key path for the PEM form. Omit entirely when <see cref="CertPathVar"/> is a PKCS#12 bundle.</summary>
    public const string KeyPathVar = "KORAS_ADP_SANDBOX_KEY_PATH";

    /// <summary>PKCS#12 password, if any. Unused for the PEM form: <see cref="Koras.Connect.Authentication.ClientCertificateTransport.LoadPem"/>
    /// does not support an encrypted private key today.</summary>
    public const string CertPasswordVar = "KORAS_ADP_SANDBOX_CERT_PASSWORD";

    public const string ApiRootVar = "KORAS_ADP_SANDBOX_API_ROOT";
    public const string AuthEndpointVar = "KORAS_ADP_SANDBOX_AUTH_ENDPOINT";
    public const string AssociateOidVar = "KORAS_ADP_SANDBOX_ASSOCIATE_OID";

    /// <summary>Reads one sandbox setting through the same environment-variable / User Secrets / local-json layering
    /// described in <see cref="SandboxConfiguration"/>, rather than the environment directly, so sandbox credentials
    /// can be set once via <c>dotnet user-secrets set</c> instead of a shell environment variable every run.</summary>
    private static bool IsSet(string key) => SandboxConfiguration.IsSet(key);

    /// <summary>True when every required KORAS_ADP_SANDBOX_* setting is present. <see cref="KeyPathVar"/> and
    /// <see cref="CertPasswordVar"/> are intentionally excluded here: exactly which of them is required depends on
    /// whether <see cref="CertPathVar"/> turns out to be a PEM or a PKCS#12 file, which <see cref="LoadCertificate"/>
    /// decides at load time. Not currently wired to a live check (see the class summary); kept so a future
    /// dynamic-skip-capable implementation has a ready-made guard to call.</summary>
    public static bool SandboxConfigured =>
        IsSet(TenantVar) && IsSet(ClientIdVar) && IsSet(ClientSecretVar) && IsSet(CertPathVar)
        && IsSet(ApiRootVar) && IsSet(AuthEndpointVar) && IsSet(AssociateOidVar);

    /// <summary>Resolves a possibly-relative configured path against, in order, the current directory, the test
    /// assembly's output directory, and this source file's directory (captured at compile time via
    /// <see cref="System.Runtime.CompilerServices.CallerFilePathAttribute"/>) &#8212; so a path like
    /// <c>Certificates/foo.pem</c> set once in User Secrets resolves correctly whether <c>dotnet test</c> is run
    /// from the repo root, the test project directory, or the build output directory. Returns the path unchanged
    /// (and unresolved) if none of those candidates exist, so a genuinely missing file still fails clearly.</summary>
    private static string ResolvePath(string path, [System.Runtime.CompilerServices.CallerFilePath] string callerFile = "")
    {
        if (Path.IsPathRooted(path) && File.Exists(path)) return path;
        var sourceDirectory = Path.GetDirectoryName(callerFile) ?? "";
        foreach (var candidate in new[] { path, Path.Combine(AppContext.BaseDirectory, path), Path.Combine(sourceDirectory, path) })
            if (File.Exists(candidate)) return candidate;
        return path;
    }

    /// <summary>Loads the configured client certificate: the PEM form (<see cref="CertPathVar"/> +
    /// <see cref="KeyPathVar"/>) when a key path is present, otherwise the PKCS#12 form (<see cref="CertPathVar"/>
    /// alone, optionally with <see cref="CertPasswordVar"/>). Throws rather than silently picking a form if neither
    /// is fully configured.</summary>
    public static System.Security.Cryptography.X509Certificates.X509Certificate2 LoadCertificate()
    {
        var certPath = ResolvePath(SandboxConfiguration.Get(CertPathVar)
            ?? throw new InvalidOperationException($"{CertPathVar} is not configured."));
        var keyPath = SandboxConfiguration.Get(KeyPathVar) is { } rawKeyPath ? ResolvePath(rawKeyPath) : null;
        return keyPath is not null
            ? Koras.Connect.Authentication.ClientCertificateTransport.LoadPem(certPath, keyPath)
            : Koras.Connect.Authentication.ClientCertificateTransport.LoadPkcs12(certPath, SandboxConfiguration.Get(CertPasswordVar));
    }

    [Fact(Skip = "Opt-in sandbox check: set KORAS_ADP_SANDBOX_TENANT, KORAS_ADP_SANDBOX_CLIENT_ID, " +
        "KORAS_ADP_SANDBOX_CLIENT_SECRET and KORAS_ADP_SANDBOX_CERT_PATH and implement an authorized " +
        "AdpConnector.DiagnoseAsync sandbox read to enable this test; it is never run otherwise and never issues a network call by default.")]
    public void LiveTenantConnectionDiagnosticsRequireExplicitOptIn()
        => throw new InvalidOperationException("Scaffolding only: no live-tenant check is wired up yet. " +
            "Remove the Skip attribute only after adding an authorized read here and recording tenant/grant evidence in docs/features/adp/ADP-020.md.");

    /// <summary>Opt-in, single-worker, read-only sandbox check. Issues exactly one authorized GET
    /// (<see cref="AdpWorkersClient.GetWorkerAsync"/>) against the one associate OID configured via
    /// <see cref="AssociateOidVar"/>; never enumerates, never touches <see cref="AdpOperationsClient"/>, and never
    /// issues anything but a GET anywhere in this method. Kept <c>[Fact(Skip=...)]</c>-gated by default (per the
    /// class summary's finding that this xunit/runner combination cannot report a runtime dynamic skip as
    /// "Skipped" rather than "Failed") so CI and any machine without sandbox credentials are unaffected; run it
    /// explicitly and individually, e.g. <c>dotnet test --filter FullyQualifiedName~SingleWorkerReadIsAuthorizedAndReturnsExpectedOid</c>,
    /// after temporarily removing the Skip attribute on a machine with <see cref="SandboxConfigured"/> set.</summary>
    [Fact(Skip = "Live, credential-bearing sandbox read. Remove Skip only for a deliberate, individually-filtered local run.")]
    public async Task SingleWorkerReadIsAuthorizedAndReturnsExpectedOid()
    {
        using var scope = BuildWorkersClientScope();
        // The single authorized network operation this test performs: one GET for one named worker.
        var worker = await scope.Workers.GetWorkerAsync(scope.AssociateOid);

        Assert.NotNull(worker);
        Assert.Equal(scope.AssociateOid, worker!.AssociateOid, ignoreCase: true);
    }

    /// <summary>Opt-in, single-worker, read-only sandbox check that additionally demonstrates
    /// <see cref="WorkerExportProjection"/>: the same one authorized GET as
    /// <see cref="SingleWorkerReadIsAuthorizedAndReturnsExpectedOid"/>, then streamed through
    /// <see cref="CsvExportProvider"/> and <see cref="WorkerExportProjection.CreateSchema"/> so only the documented,
    /// non-sensitive column set (associate OID, given/family name, job title, hire/termination date &#8212; never
    /// SSN, other government identifiers, compensation, or the raw vendor extension-data bag) is written, to a file
    /// outside the repository (<see cref="Path.GetTempPath"/>) rather than committed anywhere. No second network
    /// call and no write/create/update/delete operation of any kind.</summary>
    [Fact(Skip = "Live, credential-bearing sandbox read. Remove Skip only for a deliberate, individually-filtered local run.")]
    public async Task SingleWorkerReadExportsDocumentedColumnsToCsv()
    {
        using var scope = BuildWorkersClientScope();
        var worker = await scope.Workers.GetWorkerAsync(scope.AssociateOid);
        Assert.NotNull(worker);

        var exportPath = Path.Combine(Path.GetTempPath(), "koras-adp-sandbox-export.csv");
        await using var output = File.Create(exportPath);
        await WorkerExportProjection.ExportAsync(new CsvExportProvider(), output, AsSingleItemAsync(worker!));
    }

    private static async IAsyncEnumerable<Worker> AsSingleItemAsync(Worker worker, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return worker;
        await Task.CompletedTask;
    }

    /// <summary>Builds the mTLS-authenticated <see cref="AdpWorkersClient"/> shared by every sandbox test, bundling
    /// every disposable resource (both named <c>HttpClient</c>s and the token source) behind one <see cref="IDisposable"/>.</summary>
    private static WorkersClientScope BuildWorkersClientScope()
    {
        Assert.True(SandboxConfigured, "Sandbox settings are not fully configured; see the KORAS_ADP_SANDBOX_* constants on this class.");

        var certificate = LoadCertificate();
        var credentials = new ClientCredentialsOptions(
            new Uri(SandboxConfiguration.Get(AuthEndpointVar)!),
            SandboxConfiguration.Get(ClientIdVar)!,
            SandboxConfiguration.Get(ClientSecretVar)!);
        var workersOptions = new AdpWorkersOptions(new Uri(SandboxConfiguration.Get(ApiRootVar)!));
        var associateOid = SandboxConfiguration.Get(AssociateOidVar)!;

        var oauthHandler = new HttpClientHandler { AllowAutoRedirect = false };
        oauthHandler.ClientCertificates.Add(certificate);
        var oauthHttp = new HttpClient(oauthHandler);
        var tokenSource = new ClientCredentialsTokenSource(oauthHttp, credentials);

        var apiHandler = new HttpClientHandler { AllowAutoRedirect = false };
        apiHandler.ClientCertificates.Add(certificate);
        var apiHttp = new HttpClient(apiHandler);
        var transport = new IntegrationTransport(apiHttp, tokenSource);
        var workers = new AdpWorkersClient(transport, workersOptions);

        return new WorkersClientScope(workers, associateOid, oauthHttp, apiHttp, tokenSource);
    }

    private sealed class WorkersClientScope(AdpWorkersClient workers, string associateOid, params IDisposable[] disposables) : IDisposable
    {
        public AdpWorkersClient Workers { get; } = workers;
        public string AssociateOid { get; } = associateOid;
        public void Dispose() { foreach (var disposable in disposables) disposable.Dispose(); }
    }
}
