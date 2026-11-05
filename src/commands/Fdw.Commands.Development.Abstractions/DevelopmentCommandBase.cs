using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Base class for development commands.
/// Commands are stateless data objects that describe an operation to perform.
/// </summary>
public abstract class DevelopmentCommandBase : TypeOptionBase<DevelopmentCommandBase>, IDevelopmentCommand
{
    /// <summary>
    /// Gets the command category.
    /// </summary>
    /// <remarks>
    /// Why abstract and not a constructor parameter: the collection's generated NotFound sentinel calls the base
    /// constructor and could only pass null for it. The old constructor rejected that null, so the first touch of
    /// DevelopmentCommands threw in its static initializer (TC009, FDW-3467). An abstract member is stubbed instead.
    /// </remarks>
    public abstract IDevelopmentCommandCategory CommandCategory { get; }

    /// <summary>
    /// Gets the parameters for this command.
    /// </summary>
    public abstract IReadOnlyList<DevelopmentCommandParameter> Parameters { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevelopmentCommandBase"/> class.
    /// </summary>
    /// <param name="name">The name of the command.</param>
    /// <param name="description">The description of the command.</param>
    protected DevelopmentCommandBase(string name, string description)
        : base(name, name, name, description, "DevelopmentCommand")
    {
    }
}
