using Microsoft.Extensions.Configuration;

namespace Koras.Connect.Tests;

/// <summary>Layered configuration source for opt-in, credential-bearing sandbox tests (see <see cref="AdpSandboxTests"/>
/// and any future SAP/Qualtrics equivalents). Never commits a secret to source control: precedence, lowest to
/// highest, is an optional gitignored <c>appsettings.local.json</c> in this project directory, then
/// <see href="https://learn.microsoft.com/aspnet/core/security/app-secrets">.NET User Secrets</see> (a JSON file
/// stored outside the repository, under the user profile, keyed by this project's <c>UserSecretsId</c>), then
/// environment variables (so CI or a one-off shell override always wins). All three layers use the exact same flat
/// key names (for example <c>KORAS_ADP_SANDBOX_CLIENT_ID</c>) so existing environment-variable-based setup keeps
/// working unchanged.</summary>
public static class SandboxConfiguration
{
    private static readonly IConfiguration Root = new ConfigurationBuilder()
        .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
        .AddUserSecrets(typeof(SandboxConfiguration).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();

    /// <summary>Returns the configured value for <paramref name="key"/>, or <c>null</c> if unset/blank in every layer.</summary>
    public static string? Get(string key) => string.IsNullOrWhiteSpace(Root[key]) ? null : Root[key];

    public static bool IsSet(string key) => Get(key) is not null;
}
