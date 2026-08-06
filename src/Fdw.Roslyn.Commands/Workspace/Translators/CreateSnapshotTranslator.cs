#pragma warning disable CA1305 // Specify IFormatProvider - workspace commands use invariant strings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for creating workspace snapshots.
/// Returns snapshot metadata - actual snapshot creation is handled by the command handler.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "CreateSnapshot")]
public sealed class CreateSnapshotTranslator
    : RoslynCommandTranslatorBase<CreateSnapshotCommand, MutationResult<SnapshotData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSnapshotTranslator"/> class.
    /// </summary>
    public CreateSnapshotTranslator()
        : base("CreateSnapshotTranslator", "Translates create snapshot commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<SnapshotData>>> Translate(
        CreateSnapshotCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.SnapshotName))
        {
            return Task.FromResult<IGenericResult<MutationResult<SnapshotData>>>(
                GenericResult<MutationResult<SnapshotData>>.Failure(
                RoslynResultCodes.ByName("SnapshotNameRequired")));
        }

        // Generate a new snapshot ID - handler will use this to store the snapshot
        var snapshotId = Guid.NewGuid().ToString("N");

        var projectCount = solution.Projects.Count();
        var documentCount = solution.Projects.Sum(p => p.Documents.Count());

        var data = new SnapshotData
        {
            SnapshotId = snapshotId,
            Name = command.SnapshotName,
            Description = command.SnapshotDescription,
            ProjectCount = projectCount,
            DocumentCount = documentCount,
            Restored = false
        };

        var result = new MutationResult<SnapshotData>(
            $"Created snapshot '{command.SnapshotName}'",
            solution,
            data);

        return Task.FromResult<IGenericResult<MutationResult<SnapshotData>>>(
            GenericResult<MutationResult<SnapshotData>>.Success(result));
    }
}
