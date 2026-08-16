#pragma warning disable CA1305 // Specify IFormatProvider - workspace commands use invariant strings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Workspace.Commands;
using Fdw.Roslyn.Commands.Workspace.Results;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Workspace.Translators;

/// <summary>
/// Translator for comparing current workspace to baseline.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "CompareToBaseline")]
public sealed class CompareToBaselineTranslator
    : RoslynCommandTranslatorBase<CompareToBaselineCommand, QueryResult<ComparisonData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompareToBaselineTranslator"/> class.
    /// </summary>
    public CompareToBaselineTranslator()
        : base("CompareToBaselineTranslator", "Translates compare to baseline commands")
    {
    }

    /// <inheritdoc/>
    // MA0051: Method length acceptable - sequential comparison algorithm (build dictionaries, compare modified/added/removed, build result)
#pragma warning disable MA0051 // Method is too long
    public override async Task<IGenericResult<QueryResult<ComparisonData>>> Translate(
        CompareToBaselineCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
#pragma warning restore MA0051
    {
        CompareToBaselineTranslatorLog.Comparing(Logger);

        // Baseline comparison requires workspace access - handler provides baseline via command
        var baseline = command.BaselineSolution;

        if (baseline is null)
        {
            CompareToBaselineTranslatorLog.NoBaseline(Logger);

            var data = new ComparisonData
            {
                HasBaseline = false,
                ChangeCount = 0
            };

            var result = new QueryResult<ComparisonData>(
                "No baseline set - cannot compare",
                data);

            return GenericResult<QueryResult<ComparisonData>>.Success(result);
        }

        var changes = new List<WorkspaceChange>();

        // Multiple projects can contribute the same file path via shared sources
        // (e.g. IsExternalInit polyfill from contentFiles). Deduplicate by keeping
        // the first occurrence per path — the comparison cares only about content.
        var baselineDocs = new Dictionary<string, Document>(StringComparer.Ordinal);
        foreach (var doc in baseline.Projects.SelectMany(p => p.Documents))
        {
            var key = doc.FilePath ?? doc.Name;
            if (!baselineDocs.ContainsKey(key))
                baselineDocs[key] = doc;
        }

        var currentDocs = new Dictionary<string, Document>(StringComparer.Ordinal);
        foreach (var doc in solution.Projects.SelectMany(p => p.Documents))
        {
            var key = doc.FilePath ?? doc.Name;
            if (!currentDocs.ContainsKey(key))
                currentDocs[key] = doc;
        }

        // Check for modified documents
        foreach (var docPath in baselineDocs.Keys.Intersect(currentDocs.Keys, StringComparer.Ordinal))
        {
            var baseDoc = baselineDocs[docPath];
            var currDoc = currentDocs[docPath];

            var baseText = await baseDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var currText = await currDoc.GetTextAsync(cancellationToken).ConfigureAwait(false);

            if (!baseText.ContentEquals(currText))
            {
                changes.Add(new WorkspaceChange
                {
                    Type = "Modified",
                    FilePath = docPath,
                    Project = currDoc.Project.Name
                });
            }
        }

        // Check for added documents
        foreach (var docPath in currentDocs.Keys.Except(baselineDocs.Keys, StringComparer.Ordinal))
        {
            var doc = currentDocs[docPath];
            changes.Add(new WorkspaceChange
            {
                Type = "Added",
                FilePath = docPath,
                Project = doc.Project.Name
            });
        }

        // Check for removed documents
        foreach (var docPath in baselineDocs.Keys.Except(currentDocs.Keys, StringComparer.Ordinal))
        {
            var doc = baselineDocs[docPath];
            changes.Add(new WorkspaceChange
            {
                Type = "Removed",
                FilePath = docPath,
                Project = doc.Project.Name
            });
        }

        var comparisonData = new ComparisonData
        {
            HasBaseline = true,
            ChangeCount = changes.Count,
            Changes = changes
        };

        var queryResult = new QueryResult<ComparisonData>(
            $"Found {changes.Count} changes from baseline",
            comparisonData);

        CompareToBaselineTranslatorLog.Compared(Logger, changes.Count);

        return GenericResult<QueryResult<ComparisonData>>.Success(queryResult);
    }
}
