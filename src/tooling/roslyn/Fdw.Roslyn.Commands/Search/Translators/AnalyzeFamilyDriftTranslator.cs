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
/// Translator for the <see cref="AnalyzeFamilyDriftCommand"/>.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AnalyzeFamilyDrift")]
public sealed class AnalyzeFamilyDriftTranslator : RoslynCommandTranslatorBase<AnalyzeFamilyDriftCommand, QueryResult<FamilyDriftReport>>
{
    private readonly ILogger<AnalyzeFamilyDriftTranslator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeFamilyDriftTranslator"/> class.
    /// </summary>
    /// <remarks>
    /// Why: a genuinely zero-parameter overload — not just an optional-parameter one — is required
    /// because the cross-assembly TypeOption module initializer instantiates every translator via a
    /// bare <c>new()</c> call and only discovers types with a constructor of exactly zero declared
    /// parameters (FDW027). An <c>(ILogger? logger = null)</c>-only constructor has Parameters.Length
    /// == 1 and is silently skipped.
    /// </remarks>
    public AnalyzeFamilyDriftTranslator()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnalyzeFamilyDriftTranslator"/> class.
    /// </summary>
    /// <param name="logger">Optional logger; defaults to <see cref="NullLogger{T}.Instance"/>.</param>
    public AnalyzeFamilyDriftTranslator(ILogger<AnalyzeFamilyDriftTranslator>? logger)
        : base("AnalyzeFamilyDrift", "Analyzes structural drift across implementations of a family root")
    {
        _logger = logger ?? NullLogger<AnalyzeFamilyDriftTranslator>.Instance;
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<FamilyDriftReport>>> Translate(
        AnalyzeFamilyDriftCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        AnalyzeFamilyDriftTranslatorLog.TranslateStart(_logger, command.RootTypeName, command.NamespaceFilter ?? string.Empty, command.IncludeExtensionMethods);

        if (string.IsNullOrEmpty(command.RootTypeName))
        {
            AnalyzeFamilyDriftTranslatorLog.ValidationFailedRootRequired(_logger);
            return GenericResult<QueryResult<FamilyDriftReport>>.Failure(
                RoslynResultCodes.ByName("ClassNameRequired"));
        }

        var root = await FamilyTypeResolver.Resolve(command.RootTypeName, solution, cancellationToken, _logger).ConfigureAwait(false);
        if (root is null)
        {
            AnalyzeFamilyDriftTranslatorLog.RootNotFound(_logger, command.RootTypeName);
            return GenericResult<QueryResult<FamilyDriftReport>>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"),
                ResultDetails.Create().With("RootTypeName", command.RootTypeName));
        }

        AnalyzeFamilyDriftTranslatorLog.RootResolved(_logger, root.Name);

        var implementations = await CollectImplementations(root, command.NamespaceFilter, solution, _logger, cancellationToken).ConfigureAwait(false);
        var driftMembers = ComputeDrift(root, implementations, _logger);

        var extensionMethods = command.IncludeExtensionMethods
            ? await CollectExtensionMethods(root, solution, _logger, cancellationToken).ConfigureAwait(false)
            : new List<FamilyExtensionMethod>();

        var report = new FamilyDriftReport(
            root.ToDisplayString(),
            implementations.Count,
            implementations.Select(i => i.Name).ToList(),
            driftMembers,
            extensionMethods);

        var result = new QueryResult<FamilyDriftReport>(
            $"Family '{root.Name}': {implementations.Count} implementation(s), {driftMembers.Count} drift member(s), {extensionMethods.Count} extension method(s)",
            report);

        AnalyzeFamilyDriftTranslatorLog.TranslateSuccess(_logger, root.Name, implementations.Count, driftMembers.Count, extensionMethods.Count);
        return GenericResult<QueryResult<FamilyDriftReport>>.Success(result);
    }

    private static async Task<List<INamedTypeSymbol>> CollectImplementations(
        INamedTypeSymbol root,
        string? namespaceFilter,
        Solution solution,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        AnalyzeFamilyDriftTranslatorLog.CollectImplStart(logger, namespaceFilter ?? string.Empty);
        var matcher = NamespaceGlobMatcher.Create(namespaceFilter, logger);
        var matches = new List<INamedTypeSymbol>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AnalyzeFamilyDriftTranslatorLog.Cancelled(logger);
                break;
            }

            AnalyzeFamilyDriftTranslatorLog.CollectImplProject(logger, project.Name);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                continue;

            foreach (var type in FamilyMemberHelpers.EnumerateAllNamedTypes(compilation, logger))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    AnalyzeFamilyDriftTranslatorLog.Cancelled(logger);
                    break;
                }

                if (type.TypeKind != TypeKind.Class || type.IsAbstract)
                {
                    AnalyzeFamilyDriftTranslatorLog.CollectImplSkipped(logger, type.Name, "not-concrete-class");
                    continue;
                }
                if (SymbolEqualityComparer.Default.Equals(type, root))
                {
                    AnalyzeFamilyDriftTranslatorLog.CollectImplSkipped(logger, type.Name, "is-root");
                    continue;
                }
                if (!FamilyMemberHelpers.DerivesFrom(type, root, logger))
                {
                    AnalyzeFamilyDriftTranslatorLog.CollectImplSkipped(logger, type.Name, "does-not-derive-from-root");
                    continue;
                }

                var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                if (!matcher.IsMatch(ns))
                {
                    AnalyzeFamilyDriftTranslatorLog.CollectImplSkipped(logger, type.Name, "namespace-filter-rejected");
                    continue;
                }

                if (seen.Add(type.ToDisplayString()))
                {
                    matches.Add(type);
                    AnalyzeFamilyDriftTranslatorLog.CollectImplAccepted(logger, type.Name, ns);
                }
                else
                {
                    AnalyzeFamilyDriftTranslatorLog.CollectImplSkipped(logger, type.Name, "duplicate");
                }
            }
        }

        AnalyzeFamilyDriftTranslatorLog.CollectImplDone(logger, matches.Count);
        return matches;
    }

    private static List<FamilyDriftMember> ComputeDrift(INamedTypeSymbol root, List<INamedTypeSymbol> implementations, ILogger logger)
    {
        AnalyzeFamilyDriftTranslatorLog.ComputeDriftStart(logger, implementations.Count);
        var rootMemberKeys = FamilyMemberHelpers.GetDeclaredPublicMembers(root, logger)
            .Select(m => FamilyMemberHelpers.GetMemberKey(m, logger))
            .ToHashSet(StringComparer.Ordinal);

        // memberKey → (sample member symbol for display, set of implementation names that have it)
        var memberPresence = new Dictionary<string, (IMethodSymbol? SampleMethod, ISymbol SampleSymbol, HashSet<string> PresentIn)>(StringComparer.Ordinal);

        foreach (var impl in implementations)
        {
            foreach (var member in FamilyMemberHelpers.GetDeclaredPublicMembers(impl, logger))
            {
                var key = FamilyMemberHelpers.GetMemberKey(member, logger);
                if (rootMemberKeys.Contains(key))
                    continue;

                if (!memberPresence.TryGetValue(key, out var entry))
                {
                    entry = (member as IMethodSymbol, member, new HashSet<string>(StringComparer.Ordinal));
                    memberPresence[key] = entry;
                }
                entry.PresentIn.Add(impl.Name);
                AnalyzeFamilyDriftTranslatorLog.MemberPresenceAdded(logger, key, impl.Name);
            }
        }

        var n = implementations.Count;
        var implNames = implementations.Select(i => i.Name).ToHashSet(StringComparer.Ordinal);
        var drift = new List<FamilyDriftMember>();

        foreach (var (_, entry) in memberPresence)
        {
            var present = entry.PresentIn;
            var missing = implNames.Except(present, StringComparer.Ordinal).ToList();

            string bucket;
            if (present.Count == n)
                bucket = "Hoist";
            else if (present.Count == n - 1)
                bucket = "MostHave";
            else if (present.Count == 1)
                bucket = "Bloat";
            else
                bucket = "Mixed";

            AnalyzeFamilyDriftTranslatorLog.MemberBucketed(logger, entry.SampleSymbol.Name, bucket, present.Count, n);

            drift.Add(new FamilyDriftMember(
                entry.SampleSymbol.Name,
                entry.SampleSymbol.ToDisplayString(),
                bucket,
                present.OrderBy(s => s, StringComparer.Ordinal).ToList(),
                missing.OrderBy(s => s, StringComparer.Ordinal).ToList()));
        }

        var sorted = drift
            .OrderBy(d => BucketRank(d.Bucket))
            .ThenBy(d => d.MemberName, StringComparer.Ordinal)
            .ToList();
        AnalyzeFamilyDriftTranslatorLog.ComputeDriftDone(logger, sorted.Count);
        return sorted;
    }

    private static int BucketRank(string bucket) => bucket switch
    {
        "Hoist" => 0,
        "MostHave" => 1,
        "Mixed" => 2,
        "Bloat" => 3,
        _ => 4,
    };

    private static async Task<List<FamilyExtensionMethod>> CollectExtensionMethods(
        INamedTypeSymbol root,
        Solution solution,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        AnalyzeFamilyDriftTranslatorLog.CollectExtStart(logger);
        var matches = new List<FamilyExtensionMethod>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var project in solution.Projects)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AnalyzeFamilyDriftTranslatorLog.Cancelled(logger);
                break;
            }

            AnalyzeFamilyDriftTranslatorLog.CollectExtProject(logger, project.Name);
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation is null)
                continue;

            foreach (var type in FamilyMemberHelpers.EnumerateAllNamedTypes(compilation, logger))
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    AnalyzeFamilyDriftTranslatorLog.Cancelled(logger);
                    break;
                }

                if (type.TypeKind != TypeKind.Class || !type.IsStatic)
                    continue;
                if (!type.MightContainExtensionMethods)
                    continue;

                foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
                {
                    if (!member.IsExtensionMethod || member.Parameters.Length == 0)
                    {
                        AnalyzeFamilyDriftTranslatorLog.CollectExtSkipped(logger, member.Name, "not-extension-method");
                        continue;
                    }
                    if (member.DeclaredAccessibility != Accessibility.Public)
                    {
                        AnalyzeFamilyDriftTranslatorLog.CollectExtSkipped(logger, member.Name, "not-public");
                        continue;
                    }

                    var thisNamed = member.Parameters[0].Type as INamedTypeSymbol;
                    if (thisNamed is null)
                    {
                        AnalyzeFamilyDriftTranslatorLog.CollectExtSkipped(logger, member.Name, "this-param-not-named-type");
                        continue;
                    }

                    if (!SymbolEqualityComparer.Default.Equals(thisNamed, root) &&
                        !FamilyMemberHelpers.DerivesFrom(thisNamed, root, logger))
                    {
                        AnalyzeFamilyDriftTranslatorLog.CollectExtSkipped(logger, member.Name, "this-param-not-in-family");
                        continue;
                    }

                    var location = member.Locations.FirstOrDefault(l => l.IsInSource);
                    var lineSpan = location?.GetLineSpan();
                    var fullName = member.ToDisplayString();

                    if (!seen.Add(fullName))
                    {
                        AnalyzeFamilyDriftTranslatorLog.CollectExtSkipped(logger, member.Name, "duplicate");
                        continue;
                    }

                    matches.Add(new FamilyExtensionMethod(
                        member.Name,
                        fullName,
                        type.ToDisplayString(),
                        thisNamed.ToDisplayString(),
                        member.ToDisplayString(),
                        lineSpan?.Path ?? string.Empty,
                        lineSpan is null ? 0 : lineSpan.Value.StartLinePosition.Line + 1));

                    AnalyzeFamilyDriftTranslatorLog.CollectExtAccepted(logger, member.Name, type.ToDisplayString());
                }
            }
        }

        AnalyzeFamilyDriftTranslatorLog.CollectExtDone(logger, matches.Count);
        return matches;
    }
}
