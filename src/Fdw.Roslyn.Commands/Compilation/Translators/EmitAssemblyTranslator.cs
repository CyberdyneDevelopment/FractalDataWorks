#pragma warning disable CA1305 // Specify IFormatProvider - code compilation uses invariant strings

using System;
using System.Collections.Generic;
using System.IO;
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
using Microsoft.CodeAnalysis.Emit;

namespace Fdw.Roslyn.Commands.Compilation.Translators;

/// <summary>
/// Translator for emitting an assembly.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "EmitAssembly")]
public sealed class EmitAssemblyTranslator
    : RoslynCommandTranslatorBase<EmitAssemblyCommand, QueryResult<EmitAssemblyData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmitAssemblyTranslator"/> class.
    /// </summary>
    public EmitAssemblyTranslator()
        : base("EmitAssemblyTranslator", "Translates emit assembly commands")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: compile project, check errors, emit assembly to file
    public override async Task<IGenericResult<QueryResult<EmitAssemblyData>>> Translate(
        EmitAssemblyCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        EmitAssemblyTranslatorLog.Emitting(Logger, command.ProjectName, command.OutputPath);

        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, command.ProjectName, StringComparison.OrdinalIgnoreCase));

        if (project is null)
        {
            EmitAssemblyTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
            return GenericResult<QueryResult<EmitAssemblyData>>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName));
        }

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            EmitAssemblyTranslatorLog.FailedToGetCompilation(Logger, command.ProjectName);
            return GenericResult<QueryResult<EmitAssemblyData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetCompilation"));
        }

        var diagnostics = compilation.GetDiagnostics(cancellationToken);
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Count > 0)
        {
            var errorList = errors.Select(e =>
            {
                var lineSpan = e.Location.GetLineSpan();
                return new DiagnosticInfo
                {
                    Id = e.Id,
                    Message = e.GetMessage(),
                    Severity = e.Severity.ToString(),
                    FilePath = lineSpan.Path ?? string.Empty,
                    Line = lineSpan.StartLinePosition.Line + 1,
                    Column = lineSpan.StartLinePosition.Character + 1,
                    Category = e.Descriptor.Category
                };
            }).ToList();

            var data = new EmitAssemblyData
            {
                Success = false,
                OutputPath = command.OutputPath,
                Errors = errorList
            };

            var result = new QueryResult<EmitAssemblyData>(
                $"Compilation failed with {errors.Count} errors",
                data);

            EmitAssemblyTranslatorLog.CompilationHasErrors(Logger, command.ProjectName, errors.Count);

            return GenericResult<QueryResult<EmitAssemblyData>>.Success(result);
        }

        var pdbPath = command.EmitPdb ? Path.ChangeExtension(command.OutputPath, ".pdb") : null;

        EmitResult emitResult;
        using (var assemblyStream = File.Create(command.OutputPath))
        using (var pdbStream = pdbPath is not null ? File.Create(pdbPath) : null)
        {
            emitResult = compilation.Emit(
                assemblyStream,
                pdbStream,
                cancellationToken: cancellationToken);
        }

        if (!emitResult.Success)
        {
            var emitErrors = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(e =>
                {
                    var lineSpan = e.Location.GetLineSpan();
                    return new DiagnosticInfo
                    {
                        Id = e.Id,
                        Message = e.GetMessage(),
                        Severity = e.Severity.ToString(),
                        FilePath = lineSpan.Path ?? string.Empty,
                        Line = lineSpan.StartLinePosition.Line + 1,
                        Column = lineSpan.StartLinePosition.Character + 1,
                        Category = e.Descriptor.Category
                    };
                })
                .ToList();

            var failData = new EmitAssemblyData
            {
                Success = false,
                OutputPath = command.OutputPath,
                Errors = emitErrors
            };

            var failResult = new QueryResult<EmitAssemblyData>("Failed to emit assembly", failData);
            EmitAssemblyTranslatorLog.EmitFailed(Logger, command.OutputPath, emitErrors.Count);
            return GenericResult<QueryResult<EmitAssemblyData>>.Success(failResult);
        }

        var successData = new EmitAssemblyData
        {
            Success = true,
            OutputPath = command.OutputPath,
            PdbPath = pdbPath
        };

        var successResult = new QueryResult<EmitAssemblyData>(
            $"Assembly emitted to {command.OutputPath}",
            successData);

        EmitAssemblyTranslatorLog.Emitted(Logger, command.OutputPath);

        return GenericResult<QueryResult<EmitAssemblyData>>.Success(successResult);
    }
#pragma warning restore MA0051
}
