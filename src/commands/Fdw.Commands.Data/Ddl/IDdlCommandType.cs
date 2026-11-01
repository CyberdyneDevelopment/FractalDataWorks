using Fdw.Collections;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Interface for DDL command types (schema operations).
/// </summary>
public interface IDdlCommandType : ITypeOption<int, DdlCommandTypeBase> { }
