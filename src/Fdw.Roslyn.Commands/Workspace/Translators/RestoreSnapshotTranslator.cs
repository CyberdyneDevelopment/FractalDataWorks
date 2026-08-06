#pragma warning disable CA1305 // Specify IFormatProvider - workspace commands use invariant strings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for restoring workspace snapshots.
/// Handler provides the snapshot solution via command property.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RestoreSnapshot")]
public sealed class RestoreSnapshotTranslator
    : RoslynCommandTranslatorBase<RestoreSnapshotCommand, MutationResult<SnapshotData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestoreSnapshotTranslator"/> class.
    /// </summary>
    public RestoreSnapshotTranslator()
        : base("RestoreSnapshotTranslator", "Translates restore snapshot commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<SnapshotData>>> Translate(
        RestoreSnapshotCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.SnapshotId))
        {
            return Task.FromResult<IGenericResult<MutationResult<SnapshotData>>>(
                GenericResult<MutationResult<SnapshotData>>.Failure(
                RoslynResultCodes.ByName("SnapshotIdRequired")));
        }

        // Handler provides snapshot solution via command property
        var restoredSolution = command.SnapshotSolution;
        if (restoredSolution is null)
        {
            return Task.FromResult<IGenericResult<MutationResult<SnapshotData>>>(
                GenericResult<MutationResult<SnapshotData>>.Failure(
                    RoslynResultCodes.ByName("SnapshotNotFound"),
                    ResultDetails.Create("SnapshotId", command.SnapshotId)));
        }

        var projectCount = restoredSolution.Projects.Count();
        var documentCount = restoredSolution.Projects.Sum(p => p.Documents.Count());

        var data = new SnapshotData
        {
            SnapshotId = command.SnapshotId,
            Name = string.Empty,
            Description = string.Empty,
            ProjectCount = projectCount,
            DocumentCount = documentCount,
            Restored = true
        };

        var mutationResult = new MutationResult<SnapshotData>(
            $"Restored snapshot '{command.SnapshotId}'",
            restoredSolution,
            data);

        return Task.FromResult<IGenericResult<MutationResult<SnapshotData>>>(
            GenericResult<MutationResult<SnapshotData>>.Success(mutationResult));
    }
}
