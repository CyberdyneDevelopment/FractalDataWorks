using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Results;

/// <summary>
/// Factory was null.
/// </summary>
[TypeOption(typeof(ServicesResultCodes), "FactoryRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FactoryRequiredCode : ServicesResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FactoryRequiredCode"/> class.
    /// </summary>
    public FactoryRequiredCode()
        : base(21000, "FactoryRequired",
            ResultSeverities.ByName("Error"),
            "Factory cannot be null",
            isRetryable: false)
    {
    }
}