using System;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Messages;

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

    /// <summary>
    /// Gets the check of a step whose command reports its unmatched keys (FDW-4988): it reads the step's answer before the unit commits and returns the reason
    /// to roll the unit back, or <see langword="null"/> to go on. <see langword="null"/> for a step whose answer is not checked.
    /// </summary>
    public Func<BulkWriteAnswer, IGenericMessage?>? CheckAnswer { get; init; }
}
