using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Orchestration.Pipelines.TypeCollections.ValidationData;

/// <summary>Base class for the kinds of value the DataType validation rule can check text against.</summary>
public abstract class ValidationDataKindBase : TypeOptionBase<ValidationDataKindBase>, IValidationDataKind
{
    /// <summary>Initializes a new instance of the <see cref="ValidationDataKindBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="words">The upper-case words a rule's <c>DataType</c> parameter names the kind by.</param>
    protected ValidationDataKindBase(string name, IReadOnlyList<string> words)
        : base(name)
    {
        Words = words;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> Words { get; }

    /// <inheritdoc />
    public abstract bool Accepts(string text);
}
