using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Resolves the project or type a guide query is about, in the loaded solution.
/// </summary>
internal static class GuideTargetResolver
{
    /// <summary>
    /// Resolves a target from a type name, a project name, or both.
    /// </summary>
    /// <param name="solution">The loaded solution.</param>
    /// <param name="typeName">The type to resolve; a simple name finds source types, a qualified name also finds referenced ones.</param>
    /// <param name="projectName">The project to resolve, or to scope the type search to.</param>
    /// <param name="logger">The translator's logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The target, or a failure naming what was not found.</returns>
    public static async Task<IGenericResult<GuideTarget>> Resolve(
        Solution solution,
        string? typeName,
        string? projectName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(projectName))
            return await InProject(solution, typeName, projectName, logger, cancellationToken).ConfigureAwait(false);

        GuideTargetResolverLog.ResolvingType(logger, typeName!);
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null) continue;

            var type = Find(compilation, typeName!, cancellationToken);
            if (type is not null)
                return GenericResult<GuideTarget>.Success(GuideTarget.ForType(type, project));
        }

        return TypeNotFound(typeName!, logger);
    }

    private static async Task<IGenericResult<GuideTarget>> InProject(
        Solution solution,
        string? typeName,
        string projectName,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var project = solution.Projects.FirstOrDefault(p =>
            string.Equals(p.Name, projectName, StringComparison.OrdinalIgnoreCase));
        if (project is null)
        {
            GuideTargetResolverLog.ProjectNotFound(logger, projectName);
            return GenericResult<GuideTarget>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", projectName));
        }

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            GuideTargetResolverLog.FailedToGetCompilation(logger, projectName);
            return GenericResult<GuideTarget>.Failure(
                RoslynResultCodes.ByName("FailedToGetCompilation"),
                ResultDetails.Create().With("ProjectName", projectName));
        }

        if (string.IsNullOrEmpty(typeName))
        {
            GuideTargetResolverLog.ResolvingProject(logger, projectName);
            return GenericResult<GuideTarget>.Success(GuideTarget.ForProject(project, compilation));
        }

        GuideTargetResolverLog.ResolvingTypeInProject(logger, typeName, projectName);
        var type = Find(compilation, typeName, cancellationToken);
        return type is null
            ? TypeNotFound(typeName, logger)
            : GenericResult<GuideTarget>.Success(GuideTarget.ForType(type, project));
    }

    private static INamedTypeSymbol? Find(Microsoft.CodeAnalysis.Compilation compilation, string typeName, CancellationToken cancellationToken)
    {
        if (typeName.Contains('.', StringComparison.Ordinal))
            return GuideTypeResolver.Resolve(compilation, typeName);

        return compilation
            .GetSymbolsWithName(n => string.Equals(n, typeName, StringComparison.Ordinal), SymbolFilter.Type, cancellationToken)
            .OfType<INamedTypeSymbol>()
            .FirstOrDefault();
    }

    private static IGenericResult<GuideTarget> TypeNotFound(string typeName, ILogger logger)
    {
        GuideTargetResolverLog.TypeNotFound(logger, typeName);
        return GenericResult<GuideTarget>.Failure(
            RoslynResultCodes.ByName("TypeNotFound"),
            ResultDetails.Create().With("TypeName", typeName));
    }
}
