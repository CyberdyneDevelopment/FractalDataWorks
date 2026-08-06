using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.MsSql.Results;

/// <summary>
/// Delete command missing required filter.
/// </summary>
[TypeOption(typeof(MsSqlDataResultCodes), "MissingDeleteFilter", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MissingDeleteFilterCode : MsSqlDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingDeleteFilterCode"/> class.
    /// </summary>
    public MissingDeleteFilterCode()
        : base(21000, "MissingDeleteFilter",
            ResultSeverities.ByName("Error"),
            "DeleteCommand must have valid Filter with Root node - delete without WHERE clause not allowed for safety",
            isRetryable: false)
    {
    }
}