using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>Text that reads as a GUID.</summary>
[TypeOption(typeof(StoredIdKinds), "Text")]
[ExcludeFromCodeCoverage]
public sealed class TextStoredIdKind : StoredIdKindBase
{
    /// <summary>Initializes a new instance of the <see cref="TextStoredIdKind"/> class.</summary>
    public TextStoredIdKind()
        : base("Text")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is string text && Guid.TryParse(text, out _);

    /// <inheritdoc />
    public override Guid? Read(object value) => Guid.Parse((string)value);
}
