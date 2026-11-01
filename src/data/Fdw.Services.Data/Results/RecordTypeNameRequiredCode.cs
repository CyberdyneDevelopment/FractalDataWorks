using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// DataSet has no RecordTypeName configured.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "RecordTypeNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RecordTypeNameRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordTypeNameRequiredCode"/> class.
    /// </summary>
    public RecordTypeNameRequiredCode()
        : base(21014, "RecordTypeNameRequired", ResultSeverities.ByName("Error"),
            "DataSet '{DataSetName}' has no RecordTypeName configured",
            isRetryable: false)
    {
    }
}