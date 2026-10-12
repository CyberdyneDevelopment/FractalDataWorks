#pragma warning disable CA1305 // Specify IFormatProvider - code compilation uses invariant strings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Compilation.Commands;
using Fdw.Roslyn.Commands.Compilation.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for getting compilation options.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GetCompilationOptions")]
public sealed class GetCompilationOptionsTranslator
    : RoslynCommandTranslatorBase<GetCompilationOptionsCommand, QueryResult<CompilationOptionsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCompilationOptionsTranslator"/> class.
    /// </summary>
    public GetCompilationOptionsTranslator()
        : base("GetCompilationOptionsTranslator", "Translates get compilation options commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<CompilationOptionsData>>> Translate(
        GetCompilationOptionsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        GetCompilationOptionsTranslatorLog.Retrieving(Logger, command.ProjectName);

        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            GetCompilationOptionsTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
            return GenericResult<QueryResult<CompilationOptionsData>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName));
        }

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            GetCompilationOptionsTranslatorLog.FailedToGetCompilation(Logger, command.ProjectName);
            return GenericResult<QueryResult<CompilationOptionsData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetCompilation"));
        }

        var options = compilation.Options;
        var csharpOptions = options as CSharpCompilationOptions;
        var parseOptions = project.ParseOptions as CSharpParseOptions;

        var data = new CompilationOptionsData
        {
            ProjectName = command.ProjectName,
            OutputKind = options.OutputKind.ToString(),
            Platform = options.Platform.ToString(),
            OptimizationLevel = options.OptimizationLevel.ToString(),
            CheckOverflow = options.CheckOverflow,
            AllowUnsafe = csharpOptions?.AllowUnsafe ?? false,
            NullableContextOptions = csharpOptions?.NullableContextOptions.ToString() ?? "Unknown",
            LanguageVersion = parseOptions?.LanguageVersion.ToString() ?? "Unknown",
            PreprocessorSymbols = parseOptions?.PreprocessorSymbolNames.ToList() ?? new List<string>(),
            ReferencedAssemblies = compilation.ReferencedAssemblyNames.Select(a => a.Name).ToList()
        };

        var result = new QueryResult<CompilationOptionsData>(
            $"Retrieved compilation options for {command.ProjectName}",
            data);

        GetCompilationOptionsTranslatorLog.Retrieved(Logger, command.ProjectName);

        return GenericResult<QueryResult<CompilationOptionsData>>.Success(result);
    }
}
