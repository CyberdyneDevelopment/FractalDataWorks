
namespace Fdw.Services.Data.Clients.Models;

/// <summary>The body that switches a data option rule on or off (FDW-6339).</summary>
public sealed class DataOptionRuleSwitchBody
{
    /// <summary>Gets or sets whether the rule is switched on.</summary>
    public bool? Active { get; set; }
}
