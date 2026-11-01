namespace Fdw.Services.Data.Clients.Models;

/// <summary>One input a Built transformation declares: a field it reads, which a Compute stage maps a field of the record to.</summary>
public sealed class TransformationInputPayload
{
    /// <summary>Gets or sets the input name, which a Compute stage's <c>inputs[].inputName</c> states.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value type the input reads: Number or Flag.</summary>
    public string ValueType { get; set; } = string.Empty;
}
