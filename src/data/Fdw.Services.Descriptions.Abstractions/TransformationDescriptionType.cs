using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A transformation's description, whatever its kind (Field, Map, Filter, Calculate, Built, Aggregate or Lookup): what the transformation is for, in the words of whoever defined it (FDW-6208). Read by whoever holds <c>datasets:read</c>, the permission the transformation routes ask. The transformation is the thing described, so the type has no owner: a transformation is removed with its own descriptions.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class TransformationDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the transformation provider writes and reads its descriptions under.</summary>
    public const string OptionName = "Transformation";

    /// <summary>Initializes a new instance of the <see cref="TransformationDescriptionType"/> class.</summary>
    public TransformationDescriptionType()
        : base(OptionName, "datasets:read", hasOwner: false)
    {
    }
}
