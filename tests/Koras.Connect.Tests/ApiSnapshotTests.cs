using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-002/SDK-017 "API snapshot tests": a committed, reviewable text snapshot of each shipping
/// package's exported public surface. No ApiCompat/PublicApiGenerator tooling exists anywhere in this repo
/// (confirmed by inspection of every .csproj and eng/*.py), so this is a minimal, dependency-free reflection-based
/// equivalent: any intentional public API change for a covered package must update its committed snapshot file in
/// the same change, making the diff visible in review instead of drifting silently - this is this repo's answer
/// to SDK-017's "add package validation against approved baseline" and "deliberately removed/changed API" unit-test
/// requirements (a removed/renamed/retyped member changes the deterministic Describe() output, which a snapshot
/// comparison catches; type-forwarding identity and old/new consumer compilation are separately covered by
/// PackageGraphTests.FacadeTypeForwardResolvesToItsNewAssembly and the two samples/PackageConsumer* projects).
/// Covers every shipping assembly except the Koras.Connect facade, whose only "exported types" are forwarders
/// already exercised by that test. "Transitive dependency bounds" has nothing to check here: every package in
/// this repo shares one Directory.Build.props Version, so no per-package version skew is structurally possible.</summary>
public sealed class ApiSnapshotTests
{
    private static string RepoRoot([CallerFilePath] string callerFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerFile)!, "..", ".."));

    [Theory]
    [InlineData("Koras.Connect.Abstractions")]
    [InlineData("Koras.Connect.Core")]
    [InlineData("Koras.Connect.Authentication")]
    [InlineData("Koras.Connect.Export")]
    [InlineData("Koras.Connect.Adp")]
    [InlineData("Koras.Connect.Sap.SuccessFactors")]
    [InlineData("Koras.Connect.Qualtrics")]
    public void PublicSurfaceMatchesCommittedSnapshot(string assemblyName)
    {
        var assembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, assemblyName + ".dll"));
        var actual = Describe(assembly);
        var baselinePath = Path.Combine(RepoRoot(), "tests", "Koras.Connect.Tests", "ApiSnapshots", assemblyName + ".snapshot.txt");
        var expected = File.ReadAllText(baselinePath).Replace("\r\n", "\n").TrimEnd('\n');
        Assert.Equal(expected, actual);
    }

    private static string Describe(Assembly assembly)
    {
        var lines = new List<string>();
        foreach (var type in assembly.GetExportedTypes().OrderBy(t => t.FullName, StringComparer.Ordinal))
        {
            lines.Add(DescribeType(type));
            foreach (var member in DescribeMembers(type)) lines.Add("  " + member);
        }
        return string.Join("\n", lines);
    }

    private static string DescribeType(Type type)
    {
        var kind = type.IsInterface ? "interface" : type.IsEnum ? "enum" : type.IsValueType ? "struct" : "class";
        var modifier = type.IsEnum ? "" : type.IsInterface ? "" : type.IsAbstract ? "abstract " : type.IsSealed ? "sealed " : "";
        return $"{modifier}{kind} {FormatType(type)}";
    }

    private static IEnumerable<string> DescribeMembers(Type type)
    {
        if (type.IsEnum)
        {
            foreach (var name in Enum.GetNames(type))
                yield return $"enum-value {name} = {Convert.ToInt64(Enum.Parse(type, name))}";
            yield break;
        }
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        foreach (var ctor in type.GetConstructors(flags)
            .OrderBy(c => string.Join(",", c.GetParameters().Select(p => p.ParameterType.FullName)), StringComparer.Ordinal))
            yield return $"ctor({string.Join(", ", ctor.GetParameters().Select(DescribeParameter))})";

        foreach (var prop in type.GetProperties(flags).OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var access = prop.GetMethod is not null ? "get" : "";
            if (prop.SetMethod is not null) access += IsInitOnly(prop.SetMethod) ? "+init" : "+set";
            yield return $"property {FormatType(prop.PropertyType)} {prop.Name} {{{access}}}";
        }

        foreach (var method in type.GetMethods(flags).Where(m => !m.IsSpecialName)
            .OrderBy(m => $"{m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName))})", StringComparer.Ordinal))
            yield return $"method {FormatType(method.ReturnType)} {method.Name}({string.Join(", ", method.GetParameters().Select(DescribeParameter))})";
    }

    private static string DescribeParameter(ParameterInfo p)
        => $"{FormatType(p.ParameterType)} {p.Name}" + (p.HasDefaultValue ? $" = {p.DefaultValue ?? "null"}" : "");

    private static bool IsInitOnly(MethodInfo setMethod)
        => setMethod.ReturnParameter.GetRequiredCustomModifiers().Any(m => m == typeof(IsExternalInit));

    private static string FormatType(Type t)
    {
        if (t.IsGenericParameter) return t.Name;
        if (!t.IsGenericType) return t.FullName ?? t.Name;
        var name = t.GetGenericTypeDefinition().FullName!;
        name = name[..name.IndexOf('`')];
        return $"{name}<{string.Join(", ", t.GetGenericArguments().Select(FormatType))}>";
    }
}
