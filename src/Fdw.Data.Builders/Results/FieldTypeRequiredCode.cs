using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Field type is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "FieldTypeRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldTypeRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldTypeRequiredCode"/> class.
    /// </summary>
    public FieldTypeRequiredCode()
        : base(21011, "FieldTypeRequired",
            ResultSeverities.ByName("Error"),
            "Field type is required",
            isRetryable: false)
    {
    }
}