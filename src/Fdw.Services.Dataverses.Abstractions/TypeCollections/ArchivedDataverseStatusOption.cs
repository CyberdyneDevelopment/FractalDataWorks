using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Dataverses.Abstractions;

/// <summary>Finished. Retained for reference, and what the dataverse owns goes with it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataverseStatuses), "Archived")]
public sealed class ArchivedDataverseStatusOption : DataverseStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="ArchivedDataverseStatusOption"/> class.</summary>
    public ArchivedDataverseStatusOption() : base("Archived")
    {
    }
}
