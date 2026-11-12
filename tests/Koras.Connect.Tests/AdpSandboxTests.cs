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
    public const string CertPathVar = "KORAS_ADP_SANDBOX_CERT_PATH";

    private static bool IsSet(string variable) => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variable));

    /// <summary>True when every KORAS_ADP_SANDBOX_* variable is present. Not currently wired to a live check (see
    /// the class summary); kept so a future dynamic-skip-capable implementation has a ready-made guard to call.</summary>
    public static bool SandboxConfigured => IsSet(TenantVar) && IsSet(ClientIdVar) && IsSet(ClientSecretVar) && IsSet(CertPathVar);

    [Fact(Skip = "Opt-in sandbox check: set KORAS_ADP_SANDBOX_TENANT, KORAS_ADP_SANDBOX_CLIENT_ID, " +
        "KORAS_ADP_SANDBOX_CLIENT_SECRET and KORAS_ADP_SANDBOX_CERT_PATH and implement an authorized " +
        "AdpConnector.DiagnoseAsync sandbox read to enable this test; it is never run otherwise and never issues a network call by default.")]
    public void LiveTenantConnectionDiagnosticsRequireExplicitOptIn()
        => throw new InvalidOperationException("Scaffolding only: no live-tenant check is wired up yet. " +
            "Remove the Skip attribute only after adding an authorized read here and recording tenant/grant evidence in docs/features/adp/ADP-020.md.");
}
