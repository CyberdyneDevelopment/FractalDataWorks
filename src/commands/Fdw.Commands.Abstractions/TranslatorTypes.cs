using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Commands.Abstractions;

/// <summary>
/// Collection of translator types.
/// </summary>
/// <remarks>
/// This collection is populated by the source generator with all types
/// that inherit from TranslatorTypeBase and implement ITranslatorType.
/// Provides high-performance lookups for translator discovery and routing.
/// </remarks>
[TypeCollection(typeof(TranslatorTypeBase), typeof(ITranslatorType), typeof(TranslatorTypes))]
public abstract partial class TranslatorTypes : TypeCollectionBase<TranslatorTypeBase, ITranslatorType>
{
    // Source generator will add:
    // - public static IReadOnlyList<ITranslatorType> All { get; }
    // - public static ITranslatorType ById(int id)
    // - public static ITranslatorType ByName(string name)
    // - Individual static properties for each translator type

    /// <summary>
    /// Finds translators that can convert from the source format to the target format.
    /// </summary>
    /// <param name="sourceFormat">The input format to translate from.</param>
    /// <param name="targetFormat">The output format to translate to.</param>
    /// <returns>Collection of compatible translators, ordered by priority.</returns>
    /// <remarks>
    /// Integration-testable only: The inner loop requires actual TypeOption implementations
    /// which are registered by downstream projects. Unit tests in the Abstractions project
    /// only verify empty-collection behavior; full behavior is tested in integration tests.
    /// </remarks>
    // Coverage exclusion: Integration-testable only - requires TypeOption implementations from downstream projects
    [ExcludeFromCodeCoverage]
    public static ITranslatorType[] FindTranslators(IDataFormat sourceFormat, IDataFormat targetFormat)
    {
        var translators = new System.Collections.Generic.List<ITranslatorType>();

        foreach (var translator in All())
        {
            if (translator.SourceFormat.Id == sourceFormat.Id &&
                translator.TargetFormat.Id == targetFormat.Id)
            {
                translators.Add(translator);
            }
        }

        translators.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return translators.ToArray();
    }
}