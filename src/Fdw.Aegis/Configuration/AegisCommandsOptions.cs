using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Aegis.Configuration;

/// <summary>
/// IOptions wrapper around the <c>Commands</c> block loaded from <c>aegisSchema.json</c>.
/// </summary>
/// <remarks>
/// Why a dedicated wrapper: <see cref="AegisCommandConfiguration"/> is bound via a custom
/// <c>JsonSerializer.Deserialize</c> pass (the discriminator dispatch needs
/// <c>ApprovalPolicyTypes.ByName</c>, which plain <c>IConfiguration</c> binding cannot drive) —
/// there is no <c>IConfiguration</c> section to bind an <c>IOptions&lt;T&gt;</c> from directly.
/// <c>Fdw.Aegis.McpServer</c>'s <c>Program.cs</c> loads the schema once at startup and registers
/// this wrapper via <c>Options.Create(...)</c>, so both <c>PreApprovedPolicyEvaluator</c> (in this
/// package) and <c>AegisToolService</c> (in the entry-point) read the identical, single-loaded list.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class AegisCommandsOptions
{
    /// <summary>Gets or sets the declared Aegis commands for this host.</summary>
    public IReadOnlyList<AegisCommandConfiguration> Commands { get; set; } = System.Array.Empty<AegisCommandConfiguration>();
}
