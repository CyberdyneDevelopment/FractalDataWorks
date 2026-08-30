using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// A row of <c>auth.AuthenticationFlowStep</c> — one step of one flow, and its settings.
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "AuthenticationFlowStep")]
public partial class AuthenticationFlowStepConfiguration : IGenericConfiguration
{
    /// <inheritdoc />
    public Guid Id { get; set; }

    /// <inheritdoc />
    /// <remarks>The step's registered name, which is also what addresses this row.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc />
    public string SectionName => "AuthenticationFlowSteps";

    /// <inheritdoc />
    public string ServiceType => "AuthenticationFlowStep";

    /// <inheritdoc />
    public string? ServiceOptionType => null;

    /// <summary>Gets or sets the flow this step belongs to.</summary>
    /// <remarks>
    /// Keyed on the parent's RowId, which is the column auth.AuthenticationFlowStep actually
    /// carries. A Guid AuthenticationFlowId here bound to no column, so it stayed empty and every
    /// flow loaded with zero steps.
    /// </remarks>
    public int AuthenticationFlowRowId { get; set; }

    /// <summary>Gets or sets where in the flow this step runs.</summary>
    public int StepOrder { get; set; }

    /// <summary>Gets or sets the registered name of the step to run.</summary>
    public string StepName { get; set; } = string.Empty;

    /// <summary>Gets or sets that step's own settings, as JSON.</summary>
    /// <remarks>
    /// Holds no secret. A client secret is named here and resolved from the secret manager, because
    /// a secret in a row is a secret in every backup and query result that touches it.
    /// </remarks>
    public string? Configuration { get; set; }
}
