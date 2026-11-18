
namespace Fdw.Services.Data.Clients.Models;

/// <summary>What a data option leaf takes (FDW-6339).</summary>
public sealed class DataOptionLeafPayload
{
    /// <summary>Gets or sets the method the path ends in, such as To.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the fewest values the leaf takes: 1 for Equals and In, 2 for Between.</summary>
    public int MinValues { get; set; }

    /// <summary>Gets or sets the most values the leaf takes: 1 for Equals, 2 for Between; absent for a leaf that takes a list of any length, such as In (a single value is a list of one).</summary>
    public int? MaxValues { get; set; }
}
