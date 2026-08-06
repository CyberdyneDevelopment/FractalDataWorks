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
/// Translator for AddProjectReferenceCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AddProjectReferenceTranslator")]
public sealed class AddProjectReferenceTranslator : RoslynCommandTranslatorBase<AddProjectReferenceCommand, MutationResult<AddProjectReferenceResult>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddProjectReferenceTranslator"/> class.
    /// </summary>
    public AddProjectReferenceTranslator()
        : base("AddProjectReferenceTranslator", "Translates AddProjectReferenceCommand to add a project reference")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<AddProjectReferenceResult>>> Translate(
        AddProjectReferenceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            return Task.FromResult<IGenericResult<MutationResult<AddProjectReferenceResult>>>(
                GenericResult<MutationResult<AddProjectReferenceResult>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName)));
        }

        var referenceProject = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ReferenceName, StringComparison.OrdinalIgnoreCase));

        if (referenceProject is null)
        {
            return Task.FromResult<IGenericResult<MutationResult<AddProjectReferenceResult>>>(
                GenericResult<MutationResult<AddProjectReferenceResult>>.Failure(
                RoslynResultCodes.ByName("ReferenceProjectNotFound"),
                ResultDetails.Create().With("ReferenceName", command.ReferenceName)));
        }

        // Check if reference already exists
        if (project.ProjectReferences.Any(r => r.ProjectId == referenceProject.Id))
        {
            var existingResult = new AddProjectReferenceResult(
                projectName: command.ProjectName,
                referenceName: command.ReferenceName,
                added: false,
                reason: "Reference already exists");

            var existingMutationResult = new MutationResult<AddProjectReferenceResult>(
                $"Reference to {command.ReferenceName} already exists",
                solution,
                existingResult);

            return Task.FromResult(GenericResult<MutationResult<AddProjectReferenceResult>>.Success(existingMutationResult));
        }

        // Add the reference
        var newSolution = solution.AddProjectReference(project.Id, new ProjectReference(referenceProject.Id));

        var result = new AddProjectReferenceResult(
            projectName: command.ProjectName,
            referenceName: command.ReferenceName,
            added: true);

        var mutationResult = new MutationResult<AddProjectReferenceResult>(
            $"Added reference to {command.ReferenceName} in {command.ProjectName}",
            newSolution,
            result);

        return Task.FromResult(GenericResult<MutationResult<AddProjectReferenceResult>>.Success(mutationResult));
    }
}
