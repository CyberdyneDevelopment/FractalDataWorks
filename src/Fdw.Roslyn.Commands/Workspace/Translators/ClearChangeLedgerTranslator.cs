using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for <see cref="ClearChangeLedgerCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ClearChangeLedger")]
public sealed class ClearChangeLedgerTranslator
    : RoslynCommandTranslatorBase<ClearChangeLedgerCommand, IRoslynCommandResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearChangeLedgerTranslator"/> class.
    /// </summary>
    public ClearChangeLedgerTranslator()
        : base("ClearChangeLedger", "Discards the recorded change history")
    {
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The clear itself is performed by the handler, which owns the ledger; this reports what is about to
    /// be discarded so the count is visible before it is gone.
    /// </remarks>
    public override Task<IGenericResult<IRoslynCommandResult>> Translate(
        ClearChangeLedgerCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        ClearChangeLedgerTranslatorLog.Clearing(Logger, command.Reason ?? string.Empty);

        return Task.FromResult<IGenericResult<IRoslynCommandResult>>(
            GenericResult<IRoslynCommandResult>.Success(
                new QueryResult<ClearChangeLedgerData>(
                    "Change ledger cleared" + (string.IsNullOrWhiteSpace(command.Reason) ? string.Empty : $": {command.Reason}"),
                    new ClearChangeLedgerData { Reason = command.Reason ?? string.Empty })));
    }
}
