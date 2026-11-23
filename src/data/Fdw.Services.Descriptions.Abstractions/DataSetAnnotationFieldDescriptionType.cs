using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description an annotation gives one field of its data set (FDW-6208). Its owner is the annotation; the thing is the annotation's own field description row, named by the field, so a row whose field name matches no current field loses nothing. Read by whoever holds <c>datastores:read</c>, the permission the annotation routes ask.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataSetAnnotationFieldDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the owning domain's provider writes and reads its descriptions under.</summary>
    public const string OptionName = "DataSetAnnotationField";

    /// <summary>Initializes a new instance of the <see cref="DataSetAnnotationFieldDescriptionType"/> class.</summary>
    public DataSetAnnotationFieldDescriptionType()
        : base(OptionName, "datastores:read", hasOwner: true)
    {
    }
}
