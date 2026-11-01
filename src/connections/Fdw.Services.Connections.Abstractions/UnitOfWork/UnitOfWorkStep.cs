using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Abstractions.UnitOfWork;

/// <summary>
/// One step of a unit of work: a data command and the container it runs against.
/// </summary>
public sealed class UnitOfWorkStep
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWorkStep"/> class.
    /// </summary>
    /// <param name="command">The data command to run.</param>
    /// <param name="container">The container the command runs against.</param>
    public UnitOfWorkStep(IDataCommand command, IDataContainer container)
    {
        Command = command;
        Container = container;
    }

    /// <summary>Gets the data command to run.</summary>
    public IDataCommand Command { get; }

    /// <summary>Gets the container the command runs against.</summary>
    public IDataContainer Container { get; }
}
