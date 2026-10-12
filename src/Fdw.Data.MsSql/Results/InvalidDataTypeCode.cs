using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Input data type is invalid for the operation.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidDataType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidDataTypeCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDataTypeCode"/> class.
    /// </summary>
    public InvalidDataTypeCode()
        : base(21005, "InvalidDataType",
            ResultSeverities.ByName("Error"),
            "{TranslatorName} requires IEnumerable data, got {ActualType}",
            isRetryable: false)
    {
    }
}