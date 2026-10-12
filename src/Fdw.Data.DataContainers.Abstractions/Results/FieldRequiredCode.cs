using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataContainers.Abstractions.Results;

/// <summary>
/// Required field value is null.
/// </summary>
[TypeOption(typeof(DataContainerResultCodes), "FieldRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldRequiredCode : DataContainerResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldRequiredCode"/> class.
    /// </summary>
    public FieldRequiredCode()
        : base(20000, "FieldRequired",
            ResultSeverities.ByName("Error"),
            "Field '{FieldName}' is required",
            isRetryable: false)
    {
    }
}