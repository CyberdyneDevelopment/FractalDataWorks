using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.PostgreSql.Results;

/// <summary>
/// Input data is not the expected type.
/// </summary>
[TypeOption(typeof(PostgreSqlDataResultCodes), "InvalidDataType", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidDataTypeCode : PostgreSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidDataTypeCode"/> class.
    /// </summary>
    public InvalidDataTypeCode()
        : base(20001, "InvalidDataType",
            ResultSeverities.ByName("Error"),
            "Translator '{TranslatorName}' expected IEnumerable but received {ActualType}",
            isRetryable: false)
    {
    }
}
