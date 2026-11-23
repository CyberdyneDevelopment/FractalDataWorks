using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>A data set's description. Read by whoever holds <c>datasets:read</c>.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), "DataSet")]
public sealed class DataSetDescriptionType : DescriptionTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataSetDescriptionType"/> class.</summary>
    public DataSetDescriptionType()
        : base("DataSet", "datasets:read", hasOwner: false)
    {
    }
}
