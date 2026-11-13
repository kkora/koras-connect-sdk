using Koras.Connect.Abstractions;
using Koras.Connect.Authentication;
using Microsoft.Extensions.Options;
namespace Koras.Connect.Adp;

/// <summary>Bridges the standard .NET configuration-reload primitive, <see cref="IOptionsMonitor{TOptions}"/>, to
/// <see cref="ICredentialProvider{TCredential}"/> (ADP-003: "provider-source configuration"). Every call to
/// <see cref="GetAsync"/> re-reads <see cref="IOptionsMonitor{TOptions}.CurrentValue"/> and projects it through the
/// constructor-supplied projection, so an appsettings/secret-manager reload the host's configuration system already
/// supports is observed by <see cref="AdpRotatingTokenSource"/> on the very next token acquisition, without
/// recreating this provider, the token source, or any of its dependents. This closes the gap between the
/// pre-existing in-memory-only <see cref="DelegatingCredentialProvider{TCredential}"/> demonstration and an actual
/// host configuration-reload primitive.</summary>
public sealed class AdpOptionsMonitorCredentialProvider<TOptions> : ICredentialProvider<ClientCredentialsOptions>
    where TOptions : class
{
    private readonly IOptionsMonitor<TOptions> _monitor;
    private readonly Func<TOptions, ClientCredentialsOptions> _project;

    /// <param name="monitor">The configuration-reload source, for example one backed by <c>IConfiguration</c> via
    /// <c>services.Configure&lt;TOptions&gt;(...)</c>.</param>
    /// <param name="project">Maps the current options value to <see cref="ClientCredentialsOptions"/>.</param>
    public AdpOptionsMonitorCredentialProvider(IOptionsMonitor<TOptions> monitor, Func<TOptions, ClientCredentialsOptions> project)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    public ValueTask<ClientCredentialsOptions> GetAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_project(_monitor.CurrentValue));
}
