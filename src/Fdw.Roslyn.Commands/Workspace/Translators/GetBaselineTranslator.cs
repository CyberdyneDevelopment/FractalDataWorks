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
/// Translator for getting baseline information.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetBaseline")]
public sealed class GetBaselineTranslator
    : RoslynCommandTranslatorBase<GetBaselineCommand, QueryResult<BaselineData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetBaselineTranslator"/> class.
    /// </summary>
    public GetBaselineTranslator()
        : base("GetBaselineTranslator", "Translates get baseline commands")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<QueryResult<BaselineData>>> Translate(
        GetBaselineCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        // Baseline is provided by handler via command property
        var baseline = command.BaselineSolution;

        if (baseline is null)
        {
            var data = new BaselineData
            {
                HasBaseline = false,
                ProjectCount = 0,
                DocumentCount = 0
            };

            var result = new QueryResult<BaselineData>(
                "No baseline has been set",
                data);

            return Task.FromResult<IGenericResult<QueryResult<BaselineData>>>(
                GenericResult<QueryResult<BaselineData>>.Success(result));
        }

        var projectCount = baseline.Projects.Count();
        var documentCount = baseline.Projects.Sum(p => p.Documents.Count());

        var baselineData = new BaselineData
        {
            HasBaseline = true,
            ProjectCount = projectCount,
            DocumentCount = documentCount
        };

        var queryResult = new QueryResult<BaselineData>(
            $"Baseline has {projectCount} projects and {documentCount} documents",
            baselineData);

        return Task.FromResult<IGenericResult<QueryResult<BaselineData>>>(
            GenericResult<QueryResult<BaselineData>>.Success(queryResult));
    }
}
