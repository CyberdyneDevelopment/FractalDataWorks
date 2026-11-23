using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>A kind of value the DataType validation rule can check text against.</summary>
public interface IValidationDataKind : ITypeOption<int, ValidationDataKindBase>
{
    /// <summary>Gets the upper-case words a rule's <c>DataType</c> parameter names the kind by.</summary>
    IReadOnlyList<string> Words { get; }

    /// <summary>Whether the text reads as a value of this kind.</summary>
    /// <param name="text">The text of the value.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Accepts(string text);
}
