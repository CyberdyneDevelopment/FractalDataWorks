using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Descriptions.Abstractions;

/// <summary>The description of a hand-picked sample of a data set (FDW-6208). The thing is the child's logical Id and its owner is the data set; it is read by whoever holds <c>datasets:read</c>, as the data set it belongs to is.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DescriptionTypes), OptionName)]
public sealed class DataSetSampleDescriptionType : DescriptionTypeBase
{
    /// <summary>The type's name, which the data set provider writes and reads its descriptions under.</summary>
    public const string OptionName = "DataSetSample";

    /// <summary>Initializes a new instance of the <see cref="DataSetSampleDescriptionType"/> class.</summary>
    public DataSetSampleDescriptionType()
        : base(OptionName, "datasets:read", hasOwner: true)
    {
    }
}
