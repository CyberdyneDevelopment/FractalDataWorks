using Fdw.Collections;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Interface for foreign key referential action types.
/// </summary>
public interface IForeignKeyAction : ITypeOption<int, ForeignKeyActionBase> { }
