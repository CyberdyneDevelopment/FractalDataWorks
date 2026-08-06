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
/// Translator for reverting workspace to baseline.
/// Handler provides baseline via command property and applies the returned solution.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "RevertToBaseline")]
public sealed class RevertToBaselineTranslator
    : RoslynCommandTranslatorBase<RevertToBaselineCommand, MutationResult<BaselineData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RevertToBaselineTranslator"/> class.
    /// </summary>
    public RevertToBaselineTranslator()
        : base("RevertToBaselineTranslator", "Translates revert to baseline commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<BaselineData>>> Translate(
        RevertToBaselineCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        // Handler provides baseline via command property
        var baseline = command.BaselineSolution;

        if (baseline is null)
        {
            return Task.FromResult<IGenericResult<MutationResult<BaselineData>>>(
                GenericResult<MutationResult<BaselineData>>.Failure(
                    RoslynResultCodes.ByName("NoBaselineSet")));
        }

        var projectCount = baseline.Projects.Count();
        var documentCount = baseline.Projects.Sum(p => p.Documents.Count());

        var data = new BaselineData
        {
            HasBaseline = true,
            ProjectCount = projectCount,
            DocumentCount = documentCount
        };

        // Return the baseline as the new solution - handler will apply it
        var result = new MutationResult<BaselineData>(
            $"Reverted to baseline with {projectCount} projects",
            baseline,
            data);

        return Task.FromResult<IGenericResult<MutationResult<BaselineData>>>(
            GenericResult<MutationResult<BaselineData>>.Success(result));
    }
}
