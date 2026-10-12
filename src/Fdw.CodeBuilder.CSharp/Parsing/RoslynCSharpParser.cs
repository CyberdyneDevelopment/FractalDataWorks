using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.CodeBuilder.Abstractions;
using Fdw.CodeBuilder.CSharp.Results;
using Fdw.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.CodeBuilder.CSharp.Parsing;

/// <summary>
/// Roslyn-based parser for C# code.
/// </summary>
public sealed class RoslynCSharpParser : ICodeParser
{
    /// <inheritdoc/>
    public string Language => "csharp";

    /// <inheritdoc/>
    public async Task<IGenericResult<ISyntaxTree>> Parse(
        string sourceCode,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(sourceCode))
        {
            return GenericResult<ISyntaxTree>.Failure(CodeBuilderCSharpResultCodes.ByName("SourceCodeRequired"));
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            return await Task.Run(() =>
            {
                // Parse the source code using Roslyn
                var options = CSharpParseOptions.Default
                    .WithLanguageVersion(LanguageVersion.Latest)
                    .WithDocumentationMode(DocumentationMode.Parse);

                var syntaxTree = CSharpSyntaxTree.ParseText(
                    sourceCode,
                    options,
                    filePath ?? string.Empty,
                    cancellationToken: cancellationToken);

                var roslynTree = new RoslynSyntaxTree(syntaxTree, sourceCode, Language, filePath);
                return GenericResult<ISyntaxTree>.Success(roslynTree);
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            return GenericResult<ISyntaxTree>.Failure(
                CodeBuilderCSharpResultCodes.ByName("ParseCancelled"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
        catch (Exception ex)
        {
            return GenericResult<ISyntaxTree>.Failure(
                CodeBuilderCSharpResultCodes.ByName("ParseFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<IGenericResult> Validate(
        string sourceCode,
        CancellationToken cancellationToken = default)
    {
        var parseResult = await Parse(sourceCode, null, cancellationToken).ConfigureAwait(false);

        if (parseResult.IsFailure)
        {
            return parseResult;
        }

        if (parseResult.Value!.HasErrors)
        {
            var errorCount = 0;
            foreach (var _ in parseResult.Value.GetErrors())
            {
                errorCount++;
            }
            return GenericResult.Failure(
                CodeBuilderCSharpResultCodes.ByName("SyntaxErrors"),
                ResultDetails.Create().With("ErrorCount", errorCount));
        }

        return GenericResult.Success();
    }
}
