using Fdw.Collections;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Interface for ALTER TABLE operation types.
/// </summary>
public interface IAlterTableOperationType : ITypeOption<int, AlterTableOperationTypeBase> { }
