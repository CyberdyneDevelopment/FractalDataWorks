using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a join between two sources of a data set (FDW-6208). The thing is the child's logical Id and its owner is the data set; it is read by whoever holds <c>datasets:read</c>, as the data set it belongs to is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataSetJoinDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data set provider writes and reads its descriptions under.</summary>
    public const string OptionName = "DataSetJoin";

    /// <summary>Initializes a new instance of the <see cref="DataSetJoinDescriptionType"/> class.</summary>
    public DataSetJoinDescriptionType()
        : base(OptionName, "datasets:read", hasOwner: true)
    {
    }
}
