using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Globals;

/// <summary>
/// A T-SQL global variable — an @@ name — this endpoint answers.
/// </summary>
public interface ISqlGlobalVariable : ITypeOption<int, ISqlGlobalVariable>
{
    /// <summary>Gets the variable's T-SQL name, with its @@, such as @@VERSION.</summary>
    string SqlName { get; }

    /// <summary>Reads the variable's value for a request.</summary>
    /// <param name="context">The request.</param>
    /// <returns>The value; null for NULL.</returns>
    object? Read(SqlExecutionContext context);
}
