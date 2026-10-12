using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Service type does not match the expected type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "ServiceTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ServiceTypeMismatchCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceTypeMismatchCode"/> class.
    /// </summary>
    public ServiceTypeMismatchCode()
        : base(40000, "ServiceTypeMismatch", ResultSeverities.ByName("Error"),
            "Service type mismatch: expected '{ExpectedType}', got '{ActualType}'",
            isRetryable: false)
    {
    }
}