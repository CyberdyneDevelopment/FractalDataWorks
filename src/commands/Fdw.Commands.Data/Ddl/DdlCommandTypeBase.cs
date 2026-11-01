using Fdw.Collections;

namespace Fdw.Commands.Data.Ddl;

/// <summary>
/// Base class for DDL command types.
/// </summary>
public abstract class DdlCommandTypeBase : TypeOptionBase<DdlCommandTypeBase>, IDdlCommandType
{
    /// <summary>
    /// Initializes a new instance of <see cref="DdlCommandTypeBase"/>.
    /// </summary>
    protected DdlCommandTypeBase(string name) : base(name) { }
}
