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
/// Translator for the <see cref="FindFamilyDerivedTypesCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "FindFamilyDerivedTypes")]
public sealed class FindFamilyDerivedTypesTranslator : RoslynCommandTranslatorBase<FindFamilyDerivedTypesCommand, QueryResult<IReadOnlyList<FamilyDerivedType>>>
{
    private readonly ILogger<FindFamilyDerivedTypesTranslator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FindFamilyDerivedTypesTranslator"/> class.
    /// </summary>
    /// <remarks>
    /// Why: a genuinely zero-parameter overload — not just an optional-parameter one — is required
    /// because the cross-assembly TypeOption module initializer instantiates every translator via a
    /// bare <c>new()</c> call and only discovers types with a constructor of exactly zero declared
    /// parameters (FDW027). An <c>(ILogger? logger = null)</c>-only constructor has Parameters.Length
    /// == 1 and is silently skipped.
    /// </remarks>
    public FindFamilyDerivedTypesTranslator()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FindFamilyDerivedTypesTranslator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger; falls back to <see cref="NullLogger{T}.Instance"/> if not injected.</param>
    public FindFamilyDerivedTypesTranslator(ILogger<FindFamilyDerivedTypesTranslator>? logger)
        : base("FindFamilyDerivedTypes", "Finds interfaces and abstract bases that derive from a family root")
    {
        _logger = logger ?? NullLogger<FindFamilyDerivedTypesTranslator>.Instance;
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<IReadOnlyList<FamilyDerivedType>>>> Translate(
        FindFamilyDerivedTypesCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        FindFamilyDerivedTypesTranslatorLog.TranslateStart(_logger, command.RootTypeName);

        if (string.IsNullOrEmpty(command.RootTypeName))
        {
            FindFamilyDerivedTypesTranslatorLog.ValidationFailedRootRequired(_logger);
            return GenericResult<QueryResult<IReadOnlyList<FamilyDerivedType>>>.Failure(
                RoslynResultCodes.ByName("ClassNameRequired"));
        }

        var root = await FamilyTypeResolver.Resolve(command.RootTypeName, solution, cancellationToken, _logger).ConfigureAwait(false);
        if (root is null)
        {
            FindFamilyDerivedTypesTranslatorLog.RootNotFound(_logger, command.RootTypeName);
            return GenericResult<QueryResult<IReadOnlyList<FamilyDerivedType>>>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"),
                ResultDetails.Create().With("RootTypeName", command.RootTypeName));
        }

        var rootMemberKeys = FamilyMemberHelpers.GetDeclaredPublicMembers(root, _logger)
            .Select(m => FamilyMemberHelpers.GetMemberKey(m, _logger))
            .ToHashSet(StringComparer.Ordinal);
        FindFamilyDerivedTypesTranslatorLog.RootResolved(_logger, root.Name, rootMemberKeys.Count);

        var matches = new List<FamilyDerivedType>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                FindFamilyDerivedTypesTranslatorLog.Cancelled(_logger);
                break;
            }

            FindFamilyDerivedTypesTranslatorLog.ProjectScanStart(_logger, project.Name);

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
            {
                FindFamilyDerivedTypesTranslatorLog.ProjectSkippedNoCompilation(_logger, project.Name);
                continue;
            }

            foreach (var type in FamilyMemberHelpers.EnumerateAllNamedTypes(compilation, _logger))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    FindFamilyDerivedTypesTranslatorLog.Cancelled(_logger);
                    break;
                }

                if (SymbolEqualityComparer.Default.Equals(type, root))
                {
                    FindFamilyDerivedTypesTranslatorLog.TypeFilteredOut(_logger, type.Name, "is-root");
                    continue;
                }

                // Filter to interfaces + abstract classes only (this is "intermediate" tier)
                var isInterface = type.TypeKind == TypeKind.Interface;
                var isAbstractClass = type.TypeKind == TypeKind.Class && type.IsAbstract;
                if (!isInterface && !isAbstractClass)
                {
                    FindFamilyDerivedTypesTranslatorLog.TypeFilteredOut(_logger, type.Name, "not-interface-or-abstract-class");
                    continue;
                }

                if (!FamilyMemberHelpers.DerivesFrom(type, root, _logger))
                {
                    FindFamilyDerivedTypesTranslatorLog.TypeFilteredOut(_logger, type.Name, "does-not-derive-from-root");
                    continue;
                }

                if (!seen.Add(type.ToDisplayString()))
                {
                    FindFamilyDerivedTypesTranslatorLog.TypeFilteredOut(_logger, type.Name, "duplicate");
                    continue;
                }

                var extras = FamilyMemberHelpers.GetDeclaredPublicMembers(type, _logger)
                    .Where(m => !rootMemberKeys.Contains(FamilyMemberHelpers.GetMemberKey(m, _logger)))
                    .Select(m => m.Name)
                    .ToList();

                var kindLabel = FamilyMemberHelpers.DescribeTypeKind(type, _logger);
                var location = type.Locations.FirstOrDefault(l => l.IsInSource);
                var lineSpan = location?.GetLineSpan();

                matches.Add(new FamilyDerivedType(
                    type.Name,
                    type.ToDisplayString(),
                    type.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    kindLabel,
                    type.IsAbstract,
                    extras.Count,
                    extras,
                    lineSpan?.Path ?? string.Empty,
                    lineSpan is null ? 0 : lineSpan.Value.StartLinePosition.Line + 1));

                FindFamilyDerivedTypesTranslatorLog.DerivedMatchFound(_logger, type.Name, kindLabel, extras.Count);
            }
        }

        FindFamilyDerivedTypesTranslatorLog.TranslateSuccess(_logger, root.Name, matches.Count);

        var result = new QueryResult<IReadOnlyList<FamilyDerivedType>>(
            $"Found {matches.Count} derived interface/abstract type(s) for '{root.Name}'",
            matches);

        return GenericResult<QueryResult<IReadOnlyList<FamilyDerivedType>>>.Success(result);
    }
}
