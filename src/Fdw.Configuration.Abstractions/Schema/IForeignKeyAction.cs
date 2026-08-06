using Fdw.Collections;

namespace Fdw.Configuration.Persistence.Schema;

/// <summary>
/// Interface for foreign key referential actions.
/// </summary>
public interface IForeignKeyAction : ITypeOption<int, ForeignKeyActionBase> { }
