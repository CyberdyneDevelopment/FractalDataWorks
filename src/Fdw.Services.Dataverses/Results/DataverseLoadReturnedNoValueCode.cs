using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>
/// A read succeeded but carried no dataverse, after existence had already been established.
/// </summary>
/// <remarks>
/// This is an internal inconsistency, not a missing row — a caller asking for something that does
/// not exist gets a not-found path instead. It exists so that case fails loudly with its own code
/// rather than being absorbed by an empty list or a substituted record. HTTP 500.
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseLoadReturnedNoValue", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseLoadReturnedNoValueCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseLoadReturnedNoValueCode"/> class.</summary>
    public DataverseLoadReturnedNoValueCode()
        : base(90000, "DataverseLoadReturnedNoValue",
            ResultSeverities.ByName("Error"),
            "Dataverse '{name}' reported success but returned no value",
            isRetryable: false)
    {
    }
}
