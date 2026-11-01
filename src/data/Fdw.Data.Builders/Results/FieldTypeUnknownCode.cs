using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Field type is not the name of a data type.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "FieldTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldTypeUnknownCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldTypeUnknownCode"/> class.
    /// </summary>
    public FieldTypeUnknownCode()
        : base(21025, "FieldTypeUnknown",
            ResultSeverities.ByName("Error"),
            "Field '{FieldName}' states data type '{DataType}', which is not the name of a DataTypes option",
            isRetryable: false)
    {
    }
}
