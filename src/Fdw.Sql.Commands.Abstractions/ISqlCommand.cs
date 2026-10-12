using Fdw.Collections;

namespace Fdw.Sql.Commands.Abstractions;

/// <summary>Marker interface for every SQL command. TypeCollection member.</summary>
public interface ISqlCommand : ITypeOption<int, SqlCommandBase>
{
    /// <summary>Logical category of the command (Analysis / Build / Generation / etc.).</summary>
    ISqlCommandCategory? CommandCategory { get; }
}
