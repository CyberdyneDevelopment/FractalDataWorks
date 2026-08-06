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
/// Translator for setting baseline for change detection.
/// Handler will store the current solution as baseline after translation.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "SetBaseline")]
public sealed class SetBaselineTranslator
    : RoslynCommandTranslatorBase<SetBaselineCommand, MutationResult<BaselineData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetBaselineTranslator"/> class.
    /// </summary>
    public SetBaselineTranslator()
        : base("SetBaselineTranslator", "Translates set baseline commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<MutationResult<BaselineData>>> Translate(
        SetBaselineCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        // Handler will store the solution as baseline based on this result
        var projectCount = solution.Projects.Count();
        var documentCount = solution.Projects.Sum(p => p.Documents.Count());

        var data = new BaselineData
        {
            HasBaseline = true,
            ProjectCount = projectCount,
            DocumentCount = documentCount
        };

        var result = new MutationResult<BaselineData>(
            $"Set baseline with {projectCount} projects and {documentCount} documents",
            solution,
            data);

        return Task.FromResult<IGenericResult<MutationResult<BaselineData>>>(
            GenericResult<MutationResult<BaselineData>>.Success(result));
    }
}
