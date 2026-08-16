using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Commands;
using Fdw.Roslyn.Commands.Conventions.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// Translator for finding MessageLogging attribute usages.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindMessageLogging")]
public sealed class FindMessageLoggingTranslator
    : RoslynCommandTranslatorBase<FindMessageLoggingCommand, QueryResult<MessageLoggingData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindMessageLoggingTranslator"/> class.
    /// </summary>
    public FindMessageLoggingTranslator()
        : base("FindMessageLoggingTranslator", "Translates message logging search commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: iterate projects, find MessageLogging attributes, extract event IDs
    public override async Task<IGenericResult<QueryResult<MessageLoggingData>>> Translate(
        FindMessageLoggingCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindMessageLoggingTranslatorLog.Scanning(Logger, command.ProjectFilter ?? "(all)");

        var loggingMethods = new List<LoggingMethodInfo>();
        var eventIdRanges = new Dictionary<string, List<int>>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            if (!string.IsNullOrEmpty(command.ProjectFilter) &&
                !project.Name.Contains(command.ProjectFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            foreach (var document in project.Documents)
            {
                if (command.IsGeneratedDocument(document)) continue;

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null) continue;

                var semanticModel = compilation.GetSemanticModel(syntaxRoot.SyntaxTree);

                var methodDeclarations = syntaxRoot.DescendantNodes().OfType<MethodDeclarationSyntax>();
                foreach (var methodDecl in methodDeclarations)
                {
                    var messageLoggingAttr = methodDecl.AttributeLists
                        .SelectMany(al => al.Attributes)
                        .FirstOrDefault(a => a.Name.ToString().Contains("MessageLogging", StringComparison.Ordinal));

                    if (messageLoggingAttr is null) continue;

                    if (semanticModel.GetDeclaredSymbol(methodDecl, cancellationToken) is not IMethodSymbol methodSymbol)
                        continue;

                    var containingType = methodSymbol.ContainingType?.Name ?? "Unknown";

                    // Extract attribute arguments
                    var eventIdArg = messageLoggingAttr.ArgumentList?.Arguments
                        .FirstOrDefault(a => string.Equals(a.NameEquals?.Name.ToString(), "EventId", StringComparison.Ordinal));
                    var levelArg = messageLoggingAttr.ArgumentList?.Arguments
                        .FirstOrDefault(a => string.Equals(a.NameEquals?.Name.ToString(), "Level", StringComparison.Ordinal));
                    var messageArg = messageLoggingAttr.ArgumentList?.Arguments
                        .FirstOrDefault(a => string.Equals(a.NameEquals?.Name.ToString(), "Message", StringComparison.Ordinal));

                    var eventIdStr = eventIdArg?.Expression.ToString() ?? "0";
                    if (int.TryParse(eventIdStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var eventId))
                    {
                        if (!eventIdRanges.TryGetValue(project.Name, out var ids))
                        {
                            ids = new List<int>();
                            eventIdRanges[project.Name] = ids;
                        }
                        ids.Add(eventId);
                    }

                    loggingMethods.Add(new LoggingMethodInfo
                    {
                        MethodName = methodSymbol.Name,
                        ContainingType = containingType,
                        Project = project.Name,
                        FilePath = document.FilePath ?? document.Name,
                        Line = methodDecl.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                        EventId = eventIdStr,
                        Level = levelArg?.Expression.ToString() ?? "Unknown",
                        Message = messageArg?.Expression.ToString() ?? "Unknown"
                    });
                }
            }
        }

        // Calculate event ID ranges per project
        var rangesSummary = new Dictionary<string, EventIdRange>(StringComparer.Ordinal);
        foreach (var kvp in eventIdRanges)
        {
            var sortedIds = kvp.Value.OrderBy(x => x).ToList();
            if (sortedIds.Count > 0)
            {
                rangesSummary[kvp.Key] = new EventIdRange
                {
                    Min = sortedIds[0],
                    Max = sortedIds[^1],
                    Count = sortedIds.Count
                };
            }
        }

        var data = new MessageLoggingData
        {
            Count = loggingMethods.Count,
            ProjectFilter = command.ProjectFilter ?? "(all)",
            EventIdRanges = rangesSummary,
            Methods = loggingMethods
        };

        var result = new QueryResult<MessageLoggingData>(
            $"Found {loggingMethods.Count} MessageLogging methods",
            data);

        FindMessageLoggingTranslatorLog.Found(Logger, loggingMethods.Count);

        return GenericResult<QueryResult<MessageLoggingData>>.Success(result);
    }
#pragma warning restore MA0051
}
