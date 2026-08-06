using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Response type did not match expected type.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "TypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeMismatchCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeMismatchCode"/> class.
    /// </summary>
    public TypeMismatchCode()
        : base(91010, "TypeMismatch",
            ResultSeverities.ByName("Error"),
            "Protocol returned {ActualType} but expected {ExpectedType}",
            isRetryable: false)
    {
    }
}