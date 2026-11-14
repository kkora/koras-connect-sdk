using Koras.Connect.Abstractions;
using Koras.Connect.Microsoft.Graph;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>MGR-002: tenant and permission configuration - typed permission declaration and its validation.</summary>
public sealed class GraphConnectionConfigurationTests
{
    [Fact]
    public void ZeroRequiredPermissionsIsRejected()
    {
        var options = new GraphConnectionOptions("tenant", "client", []);
        Assert.Throws<OptionsValidationException>(options.Validate);
    }

    [Fact]
    public void DelegatedVsApplicationDistinctionIsPreserved()
    {
        var options = new GraphConnectionOptions("tenant", "client",
        [
            new GraphPermissionRequirement("User.Read", IsApplicationPermission: false, "Read the signed-in user's profile."),
            new GraphPermissionRequirement("User.Read.All", IsApplicationPermission: true, "Read every user's profile unattended."),
        ]);
        options.Validate();
        Assert.False(options.RequiredPermissions[0].IsApplicationPermission);
        Assert.True(options.RequiredPermissions[1].IsApplicationPermission);
    }

    [Fact]
    public void PermissionRequirementRejectsAnEmptyPermission()
    {
        Assert.Throws<ArgumentException>(() => new GraphPermissionRequirement("", false, "reason"));
    }

    [Fact]
    public void PermissionRequirementRejectsAnEmptyReason()
    {
        Assert.Throws<ArgumentException>(() => new GraphPermissionRequirement("User.Read", false, ""));
    }

    [Fact]
    public void MissingTenantOrClientIdIsRejected()
    {
        var requirements = new[] { new GraphPermissionRequirement("User.Read", false, "reason") };
        Assert.Throws<OptionsValidationException>(() => new GraphConnectionOptions("", "client", requirements).Validate());
        Assert.Throws<OptionsValidationException>(() => new GraphConnectionOptions("tenant", "", requirements).Validate());
    }
}
