using System;
using Fdw.Configuration;

namespace Fdw.Services.Authentication.Flow;

/// <summary>The contract every AuthenticationFlow implementation carries.</summary>
public interface IAuthenticationFlowImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record's durable id.</summary>
    Guid AuthenticationFlowId { get; set; }

    /// <summary>Gets or sets the domain record's row id -- the foreign key the constraint is on.</summary>
    int AuthenticationFlowRowId { get; set; }

    /// <summary>Gets or sets the human-readable description.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets the audience a token issued by this flow is for.</summary>
    string Audience { get; set; }

    /// <summary>Gets or sets the least authentication-context class this flow accepts.</summary>
    string? MinimumAcr { get; set; }

    /// <summary>Gets or sets how long one execution of this flow lives.</summary>
    string ExecutionLifetime { get; set; }
}
