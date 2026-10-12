using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataStores.SqlServer.Results;

/// <summary>
/// Extended properties query failed (non-fatal).
/// </summary>
[TypeOption(typeof(SqlServerDataStoreResultCodes), "ExtendedPropertiesUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ExtendedPropertiesUnavailableCode : SqlServerDataStoreResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtendedPropertiesUnavailableCode"/> class.
    /// </summary>
    public ExtendedPropertiesUnavailableCode()
        : base(10001, "ExtendedPropertiesUnavailable",
            ResultSeverities.ByName("Warning"),
            "Extended properties query failed — properties are optional and will be skipped")
    {
    }
}
