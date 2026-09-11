using System;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>The contract every AuthenticationFlowStep carries.</summary>
public interface IAuthenticationFlowStepImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the flow this step belongs to.</summary>
    Guid AuthenticationFlowId { get; set; }


    /// <summary>Gets or sets the position of this step within its flow.</summary>
    int StepOrder { get; set; }

    /// <summary>Gets or sets the step this row runs.</summary>
    string StepName { get; set; }

    /// <summary>Gets or sets the step's own configuration, as written.</summary>
    string? Configuration { get; set; }
}
