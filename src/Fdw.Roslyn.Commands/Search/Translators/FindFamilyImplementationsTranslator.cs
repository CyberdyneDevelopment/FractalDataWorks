using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Search.Commands;
using Fdw.Roslyn.Commands.Search.Logging;
using Fdw.Roslyn.Commands.Search.Results;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Roslyn.Commands.Search.Translators;

/// <summary>
/// Translator for the <see cref="FindFamilyImplementationsCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindFamilyImplementations")]
public sealed class FindFamilyImplementationsTranslator : RoslynCommandTranslatorBase<FindFamilyImplementationsCommand, QueryResult<IReadOnlyList<FamilyImplementation>>>
{
    private readonly ILogger<FindFamilyImplementationsTranslator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindFamilyImplementationsTranslator"/> class.
    /// </summary>
    /// <remarks>
    /// Why: a genuinely zero-parameter overload — not just an optional-parameter one — is required
    /// because the cross-assembly TypeOption module initializer instantiates every translator via a
    /// bare <c>new()</c> call and only discovers types with a constructor of exactly zero declared
    /// parameters (FDW027). An <c>(ILogger? logger = null)</c>-only constructor has Parameters.Length
    /// == 1 and is silently skipped.
    /// </remarks>
    public FindFamilyImplementationsTranslator()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FindFamilyImplementationsTranslator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger{T}.Instance"/>.</param>
    public FindFamilyImplementationsTranslator(ILogger<FindFamilyImplementationsTranslator>? logger)
        : base("FindFamilyImplementations", "Finds concrete implementations belonging to a family")
    {
        _logger = logger ?? NullLogger<FindFamilyImplementationsTranslator>.Instance;
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<FamilyImplementation>>>> Translate(
        FindFamilyImplementationsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindFamilyImplementationsTranslatorLog.TranslateStart(_logger, command.RootTypeName, command.NamespaceFilter ?? string.Empty, command.IncludeAbstract);

        if (string.IsNullOrEmpty(command.RootTypeName))
        {
            FindFamilyImplementationsTranslatorLog.ValidationFailedRootRequired(_logger);
            return GenericResult<QueryResult<IReadOnlyList<FamilyImplementation>>>.Failure(
                RoslynResultCodes.ByName("ClassNameRequired"));
        }

        var root = await FamilyTypeResolver.Resolve(command.RootTypeName, solution, cancellationToken, _logger).ConfigureAwait(false);
        if (root is null)
        {
            FindFamilyImplementationsTranslatorLog.RootNotFound(_logger, command.RootTypeName);
            return GenericResult<QueryResult<IReadOnlyList<FamilyImplementation>>>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"),
                ResultDetails.Create().With("RootTypeName", command.RootTypeName));
        }

        var rootMemberKeys = FamilyMemberHelpers.GetDeclaredPublicMembers(root, _logger)
            .Select(m => FamilyMemberHelpers.GetMemberKey(m, _logger))
            .ToHashSet(StringComparer.Ordinal);
        FindFamilyImplementationsTranslatorLog.RootResolved(_logger, root.Name, rootMemberKeys.Count);

        var namespaceMatcher = NamespaceGlobMatcher.Create(command.NamespaceFilter, _logger);
        var matches = new List<FamilyImplementation>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                FindFamilyImplementationsTranslatorLog.Cancelled(_logger);
                break;
            }

            FindFamilyImplementationsTranslatorLog.ProjectScanStart(_logger, project.Name);

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
            {
                FindFamilyImplementationsTranslatorLog.ProjectSkippedNoCompilation(_logger, project.Name);
                continue;
            }

            foreach (var type in FamilyMemberHelpers.EnumerateAllNamedTypes(compilation, _logger))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    FindFamilyImplementationsTranslatorLog.Cancelled(_logger);
                    break;
                }

                if (type.TypeKind != TypeKind.Class)
                {
                    FindFamilyImplementationsTranslatorLog.TypeFilteredOut(_logger, type.Name, "not-class");
                    continue;
                }

                if (type.IsAbstract && !command.IncludeAbstract)
                {
                    FindFamilyImplementationsTranslatorLog.TypeFilteredOut(_logger, type.Name, "abstract-excluded");
                    continue;
                }

                if (SymbolEqualityComparer.Default.Equals(type, root))
                {
                    FindFamilyImplementationsTranslatorLog.TypeFilteredOut(_logger, type.Name, "is-root");
                    continue;
                }

                if (!FamilyMemberHelpers.DerivesFrom(type, root, _logger))
                {
                    FindFamilyImplementationsTranslatorLog.TypeFilteredOut(_logger, type.Name, "does-not-derive-from-root");
                    continue;
                }

                var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                if (!namespaceMatcher.IsMatch(ns))
                {
                    FindFamilyImplementationsTranslatorLog.NamespaceFilterRejected(_logger, type.Name, ns);
                    continue;
                }

                if (!seen.Add(type.ToDisplayString()))
                {
                    FindFamilyImplementationsTranslatorLog.TypeFilteredOut(_logger, type.Name, "duplicate");
                    continue;
                }

                var declaredMembers = FamilyMemberHelpers.GetDeclaredPublicMembers(type, _logger).ToList();
                var extraCount = declaredMembers.Count(m => !rootMemberKeys.Contains(FamilyMemberHelpers.GetMemberKey(m, _logger)));

                var location = type.Locations.FirstOrDefault(l => l.IsInSource);
                var lineSpan = location?.GetLineSpan();

                matches.Add(new FamilyImplementation(
                    type.Name,
                    type.ToDisplayString(),
                    ns,
                    type.IsAbstract,
                    declaredMembers.Count,
                    extraCount,
                    lineSpan?.Path ?? string.Empty,
                    lineSpan is null ? 0 : lineSpan.Value.StartLinePosition.Line + 1));

                FindFamilyImplementationsTranslatorLog.ImplementationFound(_logger, type.Name, ns, declaredMembers.Count, extraCount);
            }
        }

        FindFamilyImplementationsTranslatorLog.TranslateSuccess(_logger, root.Name, matches.Count);

        var result = new QueryResult<IReadOnlyList<FamilyImplementation>>(
            $"Found {matches.Count} implementation(s) of '{root.Name}'",
            matches);

        return GenericResult<QueryResult<IReadOnlyList<FamilyImplementation>>>.Success(result);
    }
}
