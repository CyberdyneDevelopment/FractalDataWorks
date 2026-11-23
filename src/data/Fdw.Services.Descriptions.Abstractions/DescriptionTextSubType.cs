using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The text a thing's own <c>Description</c> column holds.</summary>
/// <remarks>
/// Named <c>Text</c>, not <c>Description</c>: the collection generator emits a member for each option named as the option is, and a member named <c>Description</c> hides the
/// one every type option already has (CS0108).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionSubTypes), OptionName)]
public sealed class DescriptionTextSubType : DescriptionSubTypeBase
{
    /// <summary>The name of this option, the sub-type the providers that read or write "the description" of a thing use.</summary>
    public const string OptionName = "Text";

    /// <summary>Initializes a new instance of the <see cref="DescriptionTextSubType"/> class.</summary>
    public DescriptionTextSubType()
        : base(OptionName)
    {
    }
}
