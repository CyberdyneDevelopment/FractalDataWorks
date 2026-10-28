using System.Collections.Generic;

namespace Fdw.Services.Multitenancy.Endpoints;

/// <summary>
/// What an organization's documents offer when a retention policy is set (FDW-2906).
/// </summary>
public class RetentionChoicesResponse
{
    /// <summary>Gets or sets the names of the dates a period can count from.</summary>
    public IReadOnlyList<string> StartFields { get; set; } = [];

    /// <summary>Gets or sets the actions that can end a document whose period is over.</summary>
    public IReadOnlyList<string> EndActions { get; set; } = [];

    /// <summary>Gets or sets the document types the organization has documents of.</summary>
    public IReadOnlyList<string> DocumentTypes { get; set; } = [];
}
