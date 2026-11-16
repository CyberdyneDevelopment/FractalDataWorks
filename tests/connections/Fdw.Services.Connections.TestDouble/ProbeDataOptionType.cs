using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Options;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// An option type of <c>DataOptions</c> that only the tests register, with no options of its own (FDW-5665). It is declared in
/// this referenced library, the way a package adds one, so a host collects it with no other change.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataOptions), ProbeName)]
public sealed class ProbeDataOptionType : DataOptionTypeBase
{
    /// <summary>The probe option type's name.</summary>
    public const string ProbeName = "ProbeOptionType";

    /// <summary>Initializes a new instance of the <see cref="ProbeDataOptionType"/> class.</summary>
    public ProbeDataOptionType()
        : base(ProbeName)
    {
    }
}
