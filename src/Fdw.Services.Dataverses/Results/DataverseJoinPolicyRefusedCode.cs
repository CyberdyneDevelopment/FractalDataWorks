using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>
/// The dataverse's own join policy does not accept a membership request at all (Closed).
/// </summary>
/// <remarks>
/// Category 10 (Forbidden), not Category 5 (Auth) like <see cref="DataverseWriteNotPermittedCode"/>:
/// the caller is exactly who they say they are and needs no further authentication — the refusal is
/// about the dataverse's own configuration, not about who is asking. HTTP 403, not 401.
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseJoinPolicyRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseJoinPolicyRefusedCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseJoinPolicyRefusedCode"/> class.</summary>
    public DataverseJoinPolicyRefusedCode()
        : base(100000, "DataverseJoinPolicyRefused",
            ResultSeverities.ByName("Error"),
            "Dataverse '{name}' does not accept membership requests: {reason}",
            isRetryable: false)
    {
    }
}
