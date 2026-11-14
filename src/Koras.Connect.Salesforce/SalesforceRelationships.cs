using System.Text;

namespace Koras.Connect.Salesforce;

/// <summary>Composes a SOQL <c>TYPEOF</c> clause for a polymorphic relationship field (SFD-015), for example
/// <c>TYPEOF What WHEN Account THEN Name WHEN Opportunity THEN Amount ELSE Name END</c>, available v46.0+
/// [snippet-sourced, not independently re-verified this session]. The composed text is embedded directly into the
/// caller's SOQL <c>SELECT</c> list - this type does not execute a query itself, matching SFD-005's "relationship
/// traversal is caller-authored SOQL text, not a query-builder DSL" precedent exactly.</summary>
public sealed record SalesforceTypeOfClause(string Field, IReadOnlyDictionary<string, IReadOnlyList<string>> WhenFields, IReadOnlyList<string>? ElseFields = null)
{
    private static readonly System.Text.RegularExpressions.Regex IdentifierPattern =
        new("^[A-Za-z][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public override string ToString()
    {
        ValidateIdentifier(Field, nameof(Field));
        if (WhenFields is null || WhenFields.Count == 0) throw new ArgumentException("At least one WHEN clause is required.", nameof(WhenFields));
        var sb = new StringBuilder("TYPEOF ").Append(Field);
        foreach (var (type, fields) in WhenFields)
        {
            ValidateIdentifier(type, nameof(WhenFields));
            if (fields is null || fields.Count == 0) throw new ArgumentException($"At least one field is required for WHEN type '{type}'.", nameof(WhenFields));
            foreach (var field in fields) ValidateIdentifier(field, nameof(WhenFields));
            sb.Append(" WHEN ").Append(type).Append(" THEN ").Append(string.Join(',', fields));
        }
        if (ElseFields is { Count: > 0 })
        {
            foreach (var field in ElseFields) ValidateIdentifier(field, nameof(ElseFields));
            sb.Append(" ELSE ").Append(string.Join(',', ElseFields));
        }
        return sb.Append(" END").ToString();
    }

    private static void ValidateIdentifier(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value) || !IdentifierPattern.IsMatch(value))
            throw new ArgumentException("Every field/type name must be a simple identifier.", paramName);
    }
}
