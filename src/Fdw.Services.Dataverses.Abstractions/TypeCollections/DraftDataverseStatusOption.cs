using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Being set up; not yet in use.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseStatuses), "Draft")]
public sealed class DraftDataverseStatusOption : DataverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="DraftDataverseStatusOption"/> class.</summary>
    public DraftDataverseStatusOption() : base("Draft")
    {
    }
}
