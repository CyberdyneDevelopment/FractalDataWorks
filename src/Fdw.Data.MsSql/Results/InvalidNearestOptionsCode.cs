using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// A nearest search cannot be built: its vector field is not a vector field of the container, its vector has no numbers,
/// its metric is not usable, or it asks for no rows (FDW-1354).
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "InvalidNearestOptions", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidNearestOptionsCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidNearestOptionsCode"/> class.
    /// </summary>
    public InvalidNearestOptionsCode()
        : base(21012, "InvalidNearestOptions",
            ResultSeverities.ByName("Error"),
            "Invalid nearest search: {Reason}",
            isRetryable: false)
    {
    }
}
