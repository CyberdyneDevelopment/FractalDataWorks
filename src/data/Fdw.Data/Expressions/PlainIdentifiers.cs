namespace Fdw.Data;

/// <summary>
/// The names an SQL statement writes as they are, quoted, and never as text it was given to interpret: a letter or an underscore, then letters,
/// digits and underscores (FDW-6969).
/// </summary>
/// <remarks>
/// The one statement of the rule: the SQL translators refuse a column, container or alias name it does not hold (SQL injection prevention), and a
/// planner that pushes a read into a store asks the same question first, so it never sends a name the statement would refuse.
/// </remarks>
public static class PlainIdentifiers
{
    /// <summary>Gets whether a name is one an SQL statement writes as it is.</summary>
    /// <param name="name">The name.</param>
    /// <returns>True for a letter or an underscore followed by letters, digits and underscores; false otherwise, and for an empty name.</returns>
    public static bool IsPlain(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!char.IsLetter(name[0]) && name[0] != '_')
            return false;

        for (var index = 1; index < name.Length; index++)
        {
            if (!char.IsLetterOrDigit(name[index]) && name[index] != '_')
                return false;
        }

        return true;
    }
}
