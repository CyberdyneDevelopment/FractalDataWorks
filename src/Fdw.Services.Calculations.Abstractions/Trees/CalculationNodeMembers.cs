namespace Fdw.Services.Calculations.Abstractions.Trees;

/// <summary>
/// The scalar members of a node as they were read from JSON, before the operands are read.
/// </summary>
internal sealed class CalculationNodeMembers
{
    public string? Name { get; set; }

    public decimal? Value { get; set; }

    public bool? Flag { get; set; }

    public int? Places { get; set; }

    public string? Mode { get; set; }
}
