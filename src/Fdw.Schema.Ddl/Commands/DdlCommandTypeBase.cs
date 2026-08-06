using Fdw.Collections;

namespace Fdw.Schema.Ddl.Commands;

/// <summary>Base class for DDL operation types.</summary>
public abstract class DdlCommandTypeBase : TypeOptionBase<int, DdlCommandTypeBase>, IDdlCommandType
{
    /// <summary>Initializes a new instance of <see cref="DdlCommandTypeBase"/>.</summary>
    protected DdlCommandTypeBase(int id, string name) : base(id, name) { }
}
