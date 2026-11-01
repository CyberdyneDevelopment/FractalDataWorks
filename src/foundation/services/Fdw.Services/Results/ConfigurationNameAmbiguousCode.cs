using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Results;

/// <summary>
/// A configuration was asked for by name, and more than one current row the caller can see carries that name.
/// </summary>
/// <remarks>
/// A name is unique within a tenant, and a caller can see more than one tenant: shared rows are readable from every
/// tenant, and the global tenant reads every tenant's. Answering with whichever row came back first would act on a
/// configuration the caller did not choose, so the read is refused and the caller addresses the row by id, or acts in
/// the tenant that owns it (FDW-1101). HTTP 400.
/// </remarks>
[TypeOption(typeof(ServicesResultCodes), "ConfigurationNameAmbiguous", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ConfigurationNameAmbiguousCode : ServicesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationNameAmbiguousCode"/> class.</summary>
    public ConfigurationNameAmbiguousCode()
        : base(20001, "ConfigurationNameAmbiguous",
            ResultSeverities.ByName("Error"),
            "{type} name '{name}' matches {count} configurations you can see ({ids}); address it by id, or act in the tenant that owns it",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the read was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
