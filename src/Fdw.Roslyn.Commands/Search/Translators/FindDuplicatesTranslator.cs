using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the FindDuplicatesCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindDuplicates")]
public sealed class FindDuplicatesTranslator : RoslynCommandTranslatorBase<FindDuplicatesCommand, QueryResult<IReadOnlyList<DuplicateGroup>>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FindDuplicatesTranslator"/> class.
    /// </summary>
    public FindDuplicatesTranslator()
        : base("FindDuplicates", "Detects duplicate code blocks in the solution")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: iterate projects/documents, hash method bodies, group duplicates
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<DuplicateGroup>>>> Translate(
        FindDuplicatesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindDuplicatesTranslatorLog.Scanning(Logger, command.MinLines, command.MinTokens);

        var codeBlocks = new Dictionary<string, List<DuplicateCodeBlock>>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            foreach (var document in project.Documents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (syntaxRoot is null)
                    continue;

                var methods = syntaxRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Where(m => m.Body is not null || m.ExpressionBody is not null);

                foreach (var method in methods)
                {
                    var body = method.Body?.ToString() ?? method.ExpressionBody?.ToString() ?? string.Empty;
                    var lines = body.Split('\n').Length;
                    var tokens = method.DescendantTokens().Count();

                    if (lines >= command.MinLines && tokens >= command.MinTokens)
                    {
                        var normalizedBody = NormalizeCode(body);
                        var hash = ComputeHash(normalizedBody);

                        var lineSpan = method.GetLocation().GetLineSpan();
                        var blockInfo = new DuplicateCodeBlock(
                            document.FilePath ?? string.Empty,
                            method.Identifier.Text,
                            lineSpan.StartLinePosition.Line + 1,
                            lineSpan.EndLinePosition.Line + 1,
                            lines,
                            tokens);

                        if (!codeBlocks.TryGetValue(hash, out var list))
                        {
                            list = new List<DuplicateCodeBlock>();
                            codeBlocks[hash] = list;
                        }
                        list.Add(blockInfo);
                    }
                }
            }
        }

        var duplicates = codeBlocks
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => new DuplicateGroup(kvp.Key.Substring(0, 8), kvp.Value))
            .ToList();

        var result = new QueryResult<IReadOnlyList<DuplicateGroup>>(
            $"Found {duplicates.Count} duplicate code blocks",
            duplicates);

        FindDuplicatesTranslatorLog.Found(Logger, duplicates.Count);

        return GenericResult<QueryResult<IReadOnlyList<DuplicateGroup>>>.Success(result);
    }
#pragma warning restore MA0051

    private static string NormalizeCode(string code)
    {
        // Remove whitespace differences and normalize identifiers
        var normalized = code
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        // Simple normalization - remove empty lines and trim
        var lines = normalized.Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("//", StringComparison.Ordinal));

        return string.Join("\n", lines);
    }

#pragma warning disable CA5351, SCS0006 // MD5 is used for code fingerprinting, not security
    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
#if NETSTANDARD2_0
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(bytes);
#else
        var hash = MD5.HashData(bytes);
#endif
        return Convert.ToHexString(hash);
    }
#pragma warning restore CA5351, SCS0006
}
