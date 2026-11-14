using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-001: verifies the shipped package graph (master spec section 7) stays acyclic and matches the
/// exact, explicitly-reviewed set of permitted edges &#8212; any new or accidental cross-package reference (for
/// example a provider depending on another provider, or on Export/Observability) fails this test instead of
/// silently shipping. Also verifies the <c>Koras.Connect</c> compatibility facade's type forwarders actually
/// resolve to the packages that now own each type.</summary>
public sealed class PackageGraphTests
{
    private static string RepoRoot([CallerFilePath] string callerFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerFile)!, "..", "..", ".."));

    /// <summary>The only internal project-to-project edges this split authorizes. Every other project pair must
    /// NOT reference each other, enforcing that Abstractions has no dependents it depends on, that neither provider
    /// depends on another, and that no provider depends on Export/Observability.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> AllowedEdges = new Dictionary<string, string[]>
    {
        ["Koras.Connect.Abstractions"] = [],
        ["Koras.Connect.Core"] = ["Koras.Connect.Abstractions"],
        ["Koras.Connect.Authentication"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core"],
        ["Koras.Connect.Export"] = ["Koras.Connect.Abstractions"],
        ["Koras.Connect.Adp"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core", "Koras.Connect.Authentication"],
        ["Koras.Connect.Qualtrics"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core"],
        ["Koras.Connect.Sap.SuccessFactors"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core"],
        ["Koras.Connect.Dataverse"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core", "Koras.Connect.Authentication"],
        ["Koras.Connect.Salesforce"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core", "Koras.Connect.Authentication"],
        ["Koras.Connect"] = ["Koras.Connect.Abstractions", "Koras.Connect.Core", "Koras.Connect.Authentication", "Koras.Connect.Export"],
    };

    private sealed record ProjectInfo(string PackageId, string FolderName, string[] References);

    private static IReadOnlyList<ProjectInfo> LoadSrcProjects()
    {
        var srcDir = Path.Combine(RepoRoot(), "src");
        var projects = new List<ProjectInfo>();
        foreach (var csproj in Directory.EnumerateFiles(srcDir, "*.csproj", SearchOption.AllDirectories))
        {
            if (csproj.Contains(Path.Combine("bin", "")) || csproj.Contains(Path.Combine("obj", ""))) continue;
            var doc = XDocument.Load(csproj);
            var packageId = doc.Descendants("PackageId").Single().Value;
            var folderName = Path.GetFileName(Path.GetDirectoryName(csproj))!;
            var references = doc.Descendants("ProjectReference")
                .Select(e => Path.GetFileNameWithoutExtension(e.Attribute("Include")!.Value))
                .ToArray();
            projects.Add(new ProjectInfo(packageId, folderName, references));
        }
        return projects;
    }

    [Fact]
    public void EveryProjectFolderNameMatchesItsPackageId()
    {
        // eng/generate_sbom.py resolves each release-manifest entry via src/<PackageId>/obj/project.assets.json;
        // a folder/PackageId mismatch would silently break SBOM generation for that package.
        foreach (var project in LoadSrcProjects())
            Assert.Equal(project.PackageId, project.FolderName);
    }

    [Fact]
    public void EveryProjectReferenceIsOnTheReviewedAllowlist()
    {
        var projects = LoadSrcProjects();
        var byId = projects.ToDictionary(p => p.PackageId);
        foreach (var project in projects)
        {
            Assert.True(AllowedEdges.TryGetValue(project.PackageId, out var allowed),
                $"{project.PackageId} is not represented in the reviewed dependency matrix.");
            foreach (var referencedId in project.References)
            {
                Assert.True(byId.ContainsKey(referencedId), $"{project.PackageId} references unknown project '{referencedId}'.");
                Assert.Contains(referencedId, allowed);
            }
        }
    }

    [Fact]
    public void PackageReferenceGraphHasNoCycle()
    {
        var projects = LoadSrcProjects().ToDictionary(p => p.PackageId, p => p.References);
        var visiting = new HashSet<string>();
        var visited = new HashSet<string>();

        void Visit(string id, List<string> path)
        {
            if (visited.Contains(id)) return;
            Assert.True(visiting.Add(id), $"Cycle detected: {string.Join(" -> ", path)} -> {id}");
            path.Add(id);
            foreach (var next in projects[id]) Visit(next, path);
            path.RemoveAt(path.Count - 1);
            visiting.Remove(id);
            visited.Add(id);
        }

        foreach (var id in projects.Keys) Visit(id, []);
    }

    /// <summary>Every public type forwarded (master spec section 6, ADR-M06) from the <c>Koras.Connect</c> facade
    /// resolves through to its real, physical home &#8212; proving the facade is wired correctly, not just that it
    /// compiles. <see cref="Assembly.GetType(string)"/> follows <see cref="System.Runtime.CompilerServices.TypeForwardedToAttribute"/>
    /// forwarders transparently.</summary>
    [Theory]
    [InlineData("Koras.Connect.Abstractions.IConnector", "Koras.Connect.Abstractions")]
    [InlineData("Koras.Connect.Abstractions.ICredentialProvider`1", "Koras.Connect.Abstractions")]
    [InlineData("Koras.Connect.Transport.IAccessTokenSource", "Koras.Connect.Abstractions")]
    [InlineData("Koras.Connect.Transport.IRequestAuthenticator", "Koras.Connect.Abstractions")]
    [InlineData("Koras.Connect.Export.IExportProvider", "Koras.Connect.Abstractions")]
    [InlineData("Koras.Connect.Exceptions.AuthenticationException", "Koras.Connect.Core")]
    [InlineData("Koras.Connect.Transport.IntegrationTransport", "Koras.Connect.Core")]
    [InlineData("Koras.Connect.Transport.BearerTokenAuthenticator", "Koras.Connect.Core")]
    [InlineData("Koras.Connect.Synchronization.Reconciliation", "Koras.Connect.Core")]
    [InlineData("Koras.Connect.Authentication.ClientCredentialsTokenSource", "Koras.Connect.Authentication")]
    [InlineData("Koras.Connect.Authentication.CertificateCredentialProvider", "Koras.Connect.Authentication")]
    [InlineData("Koras.Connect.Export.CsvExportProvider", "Koras.Connect.Export")]
    [InlineData("Koras.Connect.Export.TabularExport", "Koras.Connect.Export")]
    public void FacadeTypeForwardResolvesToItsNewAssembly(string fullTypeName, string expectedAssembly)
    {
        var facade = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "Koras.Connect.dll"));
        var resolved = facade.GetType(fullTypeName, throwOnError: true)!;
        Assert.Equal(expectedAssembly, resolved.Assembly.GetName().Name);
    }
}
