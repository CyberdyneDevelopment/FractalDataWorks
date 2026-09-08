using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Temporarily not in use, but not archived.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseStatuses), "Paused")]
public sealed class PausedDataverseStatusOption : DataverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="PausedDataverseStatusOption"/> class.</summary>
    public PausedDataverseStatusOption() : base("Paused")
    {
    }
}
