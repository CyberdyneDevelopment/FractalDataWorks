using Fdw.Collections;
using Fdw.SourceGenerators.Services;

namespace Fdw.SourceGenerators.Models;

/// <summary>One value of <see cref="CollectionStrategies"/>: the dictionary a generated collection keeps its values in.</summary>
public interface ICollectionStrategy : ITypeOption<int, CollectionStrategyBase>
{
    /// <summary>Gets the namespace the dictionary type needs, or an empty string when System.Collections.Generic (always imported) has it.</summary>
    string RequiredNamespace { get; }

    /// <summary>Gets whether the dictionary fields are readonly.</summary>
    bool IsReadOnly { get; }

    /// <summary>Gets whether the collection gets a Register method that adds values after it is built.</summary>
    bool AllowsRegistration { get; }

    /// <summary>Gets the shape of the collection this strategy generates.</summary>
    ICollectionGenerationMode GenerationMode { get; }

    /// <summary>The type of a dictionary of this strategy.</summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="valueType">The value type.</param>
    /// <returns>The type, as written in the generated code.</returns>
    string DictionaryType(string keyType, string valueType);

    /// <summary>The statement that sets <c>_all</c> from the local <c>dictionary</c> the static constructor fills.</summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="returnType">The value type.</param>
    /// <returns>The statement.</returns>
    string AllFromDictionary(string keyType, string returnType);

    /// <summary>The statement that sets <c>_all</c> to an empty dictionary when the collection has no values.</summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="returnType">The value type.</param>
    /// <returns>The statement.</returns>
    string EmptyAll(string keyType, string returnType);

    /// <summary>The statement that fills a lookup dictionary from <c>_all</c>, keyed by one property.</summary>
    /// <param name="dictionaryName">The lookup field.</param>
    /// <param name="propertyName">The property the lookup is keyed by.</param>
    /// <param name="propertyType">The property's type.</param>
    /// <param name="returnType">The value type.</param>
    /// <returns>The statement.</returns>
    string LookupFromAll(string dictionaryName, string propertyName, string propertyType, string returnType);
}
