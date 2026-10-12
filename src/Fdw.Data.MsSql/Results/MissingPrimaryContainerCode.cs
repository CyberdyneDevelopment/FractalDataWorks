using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Compound query missing primary container metadata.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "MissingPrimaryContainer", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingPrimaryContainerCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingPrimaryContainerCode"/> class.
    /// </summary>
    public MissingPrimaryContainerCode()
        : base(21003, "MissingPrimaryContainer",
            ResultSeverities.ByName("Error"),
            "CompoundQueryCommand must have PrimaryContainer in metadata",
            isRetryable: false)
    {
    }
}