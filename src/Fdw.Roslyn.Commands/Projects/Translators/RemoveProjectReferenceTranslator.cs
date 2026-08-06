#pragma warning disable CA1305 // Specify IFormatProvider - project commands use invariant strings

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Projects.Commands;
using Fdw.Roslyn.Commands.Projects.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Projects.Translators;

/// <summary>
/// Translator for RemoveProjectReferenceCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RemoveProjectReferenceTranslator")]
public sealed class RemoveProjectReferenceTranslator : RoslynCommandTranslatorBase<RemoveProjectReferenceCommand, MutationResult<RemoveProjectReferenceResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveProjectReferenceTranslator"/> class.
    /// </summary>
    public RemoveProjectReferenceTranslator()
        : base("RemoveProjectReferenceTranslator", "Translates RemoveProjectReferenceCommand to remove a project reference")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<RemoveProjectReferenceResult>>> Translate(
        RemoveProjectReferenceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            return Task.FromResult<IGenericResult<MutationResult<RemoveProjectReferenceResult>>>(
                GenericResult<MutationResult<RemoveProjectReferenceResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        var referenceProject = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ReferenceName, StringComparison.OrdinalIgnoreCase));

        if (referenceProject is null)
        {
            return Task.FromResult<IGenericResult<MutationResult<RemoveProjectReferenceResult>>>(
                GenericResult<MutationResult<RemoveProjectReferenceResult>>.Failure(
                RoslynResultCodes.ByName("ReferenceProjectNotFound"),
                ResultDetails.Create().With("ReferenceName", command.ReferenceName)));
        }

        // Check if reference exists
        var existingReference = project.ProjectReferences
            .FirstOrDefault(r => r.ProjectId == referenceProject.Id);

        if (existingReference is null)
        {
            var notFoundResult = new RemoveProjectReferenceResult(
                projectName: command.ProjectName,
                referenceName: command.ReferenceName,
                removed: false,
                reason: "Reference does not exist");

            var notFoundMutationResult = new MutationResult<RemoveProjectReferenceResult>(
                $"Reference to {command.ReferenceName} does not exist",
                solution,
                notFoundResult);

            return Task.FromResult(GenericResult<MutationResult<RemoveProjectReferenceResult>>.Success(notFoundMutationResult));
        }

        // Remove the reference
        var newSolution = solution.RemoveProjectReference(project.Id, existingReference);

        var result = new RemoveProjectReferenceResult(
            projectName: command.ProjectName,
            referenceName: command.ReferenceName,
            removed: true);

        var mutationResult = new MutationResult<RemoveProjectReferenceResult>(
            $"Removed reference to {command.ReferenceName} from {command.ProjectName}",
            newSolution,
            result);

        return Task.FromResult(GenericResult<MutationResult<RemoveProjectReferenceResult>>.Success(mutationResult));
    }
}
