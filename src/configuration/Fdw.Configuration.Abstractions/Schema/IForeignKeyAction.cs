using Fdw.Collections;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Interface for foreign key referential actions.
/// </summary>
public interface IForeignKeyAction : ITypeOption<int, ForeignKeyActionBase>
{
    /// <summary>Gets the action's SQL keyword as it follows ON DELETE / ON UPDATE (for example "SET NULL").</summary>
    /// <remarks>
    /// The standard spelling. A dialect that cannot express an action refuses it; it never substitutes another.
    /// </remarks>
    string SqlKeyword { get; }
}
