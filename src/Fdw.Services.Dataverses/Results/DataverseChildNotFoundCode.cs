using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>
/// A member, resource or relationship named for a narrow write is not in that dataverse.
/// </summary>
/// <remarks>
/// Distinct from the dataverse itself being absent: the project exists and the row does not, which
/// usually means someone else removed it between the caller reading the map and writing to it.
/// HTTP 404.
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseChildNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseChildNotFoundCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseChildNotFoundCode"/> class.</summary>
    public DataverseChildNotFoundCode()
        : base(30000, "DataverseChildNotFound",
            ResultSeverities.ByName("Error"),
            "Dataverse '{name}' has no {kind} with id '{id}'",
            isRetryable: false)
    {
    }
}
