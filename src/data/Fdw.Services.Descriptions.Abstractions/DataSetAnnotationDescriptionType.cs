using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a data set annotation: the business description of the data set it annotates (FDW-6208). The annotation is the thing described, so the type has no owner. Read by whoever holds <c>datastores:read</c>, the permission the annotation routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataSetAnnotationDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the owning domain's provider writes and reads its descriptions under.</summary>
    public const string OptionName = "DataSetAnnotation";

    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationDescriptionType"/> class.</summary>
    public DataSetAnnotationDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: false)
    {
    }
}
