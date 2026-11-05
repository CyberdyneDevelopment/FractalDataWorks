using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Refuses two options of one collection that declare the same Id (TC008), and says which Ids cannot be read (TC013) (FDW-4045).
/// </summary>
/// <remarks>
/// Why here and not in TC008's lookup-value loop: that loop reads the value from a constructor parameter named after the property,
/// and skipped Id as "validated separately". The separate check (TC001) compares a hash of the option's type name, never the Id the
/// constructor passes, so two result codes sharing 20010 were caught only when the collection froze at its first use.
/// </remarks>
internal static class DeclaredIdValidation
{
    /// <summary>Reports duplicate and unreadable declared Ids among a collection's options.</summary>
    /// <param name="context">The production context.</param>
    /// <param name="collectionName">The collection's class name.</param>
    /// <param name="options">The collection's options.</param>
    public static void Report(SourceProductionContext context, string collectionName, ImmutableArray<TypeOptionModel> options)
    {
        var duplicates = options
            .Where(option => option.DeclaredId is not null)
            .GroupBy(option => option.DeclaredId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicates)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.DuplicateLookupValue,
                Location.None,
                collectionName,
                "Id",
                duplicate.Key,
                string.Join(", ", duplicate.Select(option => option.FullTypeName))));
        }

        foreach (var option in options.Where(option => option.DeclaredIdProblem is not null))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TypeCollectionGeneratorDiagnostics.DeclaredIdUnreadable,
                Location.None,
                collectionName,
                option.FullTypeName,
                option.DeclaredIdProblem));
        }
    }
}
