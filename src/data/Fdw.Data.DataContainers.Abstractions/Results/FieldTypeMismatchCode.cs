using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions.Results;

/// <summary>
/// Field value has incorrect type.
/// </summary>
[TypeOption(typeof(DataContainerResultCodes), "FieldTypeMismatch", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldTypeMismatchCode : DataContainerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldTypeMismatchCode"/> class.
    /// </summary>
    public FieldTypeMismatchCode()
        : base(20001, "FieldTypeMismatch",
            ResultSeverities.ByName("Error"),
            "Field '{FieldName}' expects type {ExpectedType} but got {ActualType}",
            isRetryable: false)
    {
    }
}