using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>In use.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseStatuses), "Active")]
public sealed class ActiveDataverseStatusOption : DataverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ActiveDataverseStatusOption"/> class.</summary>
    public ActiveDataverseStatusOption() : base("Active")
    {
    }
}
