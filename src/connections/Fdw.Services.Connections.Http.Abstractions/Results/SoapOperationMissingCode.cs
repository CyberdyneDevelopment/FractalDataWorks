using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// A SOAP command named no operation: the body element is the operation, so the request cannot be built without one
/// (FDW-3580: the command type used to stand in for it).
/// </summary>
[TypeOption(typeof(HttpResultCodes), "SoapOperationMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SoapOperationMissingCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapOperationMissingCode"/> class.
    /// </summary>
    public SoapOperationMissingCode()
        : base(90005, "SoapOperationMissing",
            ResultSeverities.ByName("Error"),
            "The SOAP command '{CommandType}' names no Operation in its metadata; the body element is the operation",
            isRetryable: false)
    {
    }
}
