using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// A row of <c>auth.AuthenticationFlow</c> — one named login.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationFlow")]
public partial class AuthenticationFlowConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identity this flow's steps are keyed on.</summary>
    public int RowId { get; set; }

    /// <inheritdoc />
    /// <remarks>What a caller selects. The button pressed picks the flow.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "AuthenticationFlows";

    /// <inheritdoc />
    public string ServiceType => "AuthenticationFlow";

    /// <inheritdoc />
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the audience tokens from this flow are minted for.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Gets or sets the assurance level this flow demands before issuing.</summary>
    /// <remarks>Null demands none — the terminal check separately refuses a flow that proved no one.</remarks>
    public string? MinimumAcr { get; set; }

    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
}
