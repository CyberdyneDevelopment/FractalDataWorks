using Fdw.Collections;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Interface for DDL operation types.</summary>
public interface IDdlCommandType : ITypeOption<int, DdlCommandTypeBase> { }
