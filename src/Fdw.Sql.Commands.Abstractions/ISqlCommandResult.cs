namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Common shape returned from every SQL command translator.</summary>
public interface ISqlCommandResult
{
    /// <summary>Short human-readable summary of what the command did.</summary>
    string Summary { get; }

    /// <summary>True if the command mutated the workspace (changed scripts in memory).</summary>
    bool IsMutation { get; }
}
