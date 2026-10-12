using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Data.MsSql.ValueSources;

/// <summary>
/// Identifier safety for the load translators: a name that reaches a statement came from configuration, so it is
/// checked (letters, digits and underscores, starting with a letter or underscore) and then quoted by the dialect.
/// Values never pass through here; they are always parameters.
/// </summary>
internal static class MsSqlIdentifiers
{
    /// <summary>Returns whether <paramref name="name"/> is safe to quote into a statement.</summary>
    internal static bool IsSafe(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || (!char.IsLetter(name[0]) && name[0] != '_'))
            return false;
        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>Returns the first unsafe name in <paramref name="names"/>, or <see langword="null"/> when all are safe.</summary>
    internal static string? FirstUnsafe(IEnumerable<string> names) => names.FirstOrDefault(n => !IsSafe(n));

    /// <summary>Quotes a safe name with the dialect.</summary>
    internal static string Quote(ISqlDialect dialect, string name) => dialect.QuoteIdentifier(name);

    /// <summary>Builds a quoted, optionally database- and schema-qualified name from safe parts.</summary>
    internal static string Qualified(ISqlDialect dialect, string? database, string? schema, string name)
    {
        var quoted = dialect.QuoteIdentifier(name);
        if (string.IsNullOrEmpty(schema))
            return string.IsNullOrEmpty(database) ? quoted : $"{dialect.QuoteIdentifier(database)}..{quoted}";
        var schemaQualified = $"{dialect.QuoteIdentifier(schema)}.{quoted}";
        return string.IsNullOrEmpty(database) ? schemaQualified : $"{dialect.QuoteIdentifier(database)}.{schemaQualified}";
    }
}
