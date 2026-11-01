using Fdw.Collections;

namespace Fdw.Services.Connections.MsSql.Discovery;

/// <summary>
/// Interface for foreign key referential actions.
/// </summary>
public interface IForeignKeyAction : ITypeOption<int, ForeignKeyActionBase> { }
