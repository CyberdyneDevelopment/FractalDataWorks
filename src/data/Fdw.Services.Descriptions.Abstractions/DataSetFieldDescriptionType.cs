using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A data set field's description. The thing is the field's logical Id and its owner is the data set; it is read by whoever holds <c>datasets:read</c>, as the data set it belongs to is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), "DataSetField")]
public sealed class DataSetFieldDescriptionType : DescriptionTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetFieldDescriptionType"/> class.</summary>
    public DataSetFieldDescriptionType()
        : base("DataSetField", "datasets:read", hasOwner: true)
    {
    }
}
