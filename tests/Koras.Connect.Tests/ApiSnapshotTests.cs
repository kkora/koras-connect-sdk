using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
namespace Koras.Connect.Tests;

/// <summary>SDK-002 "API snapshot tests": a committed, reviewable text snapshot of a neutral package's exported
/// public surface. No ApiCompat/PublicApiGenerator tooling exists anywhere in this repo (confirmed by inspection
/// of every .csproj and eng/*.py), so this is a minimal, dependency-free reflection-based equivalent: any
/// intentional public API change for Koras.Connect.Abstractions must update the committed snapshot file in the
/// same change, making the diff visible in review instead of drifting silently.</summary>
public sealed class ApiSnapshotTests
{
    private static string RepoRoot([CallerFilePath] string callerFile = "")
        => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(callerFile)!, "..", ".."));

    [Fact]
    public void AbstractionsPublicSurfaceMatchesCommittedSnapshot()
    {
        var assembly = Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "Koras.Connect.Abstractions.dll"));
        var actual = Describe(assembly);
        var baselinePath = Path.Combine(RepoRoot(), "tests", "Koras.Connect.Tests", "ApiSnapshots", "Koras.Connect.Abstractions.snapshot.txt");
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
