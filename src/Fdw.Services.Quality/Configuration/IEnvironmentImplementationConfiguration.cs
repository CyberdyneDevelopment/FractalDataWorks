using Fdw.Configuration;
using Fdw.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Quality.Configuration;

/// <summary>The contract every Environment implementation carries.</summary>
public interface IEnvironmentImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record\'s durable id.</summary>
    Guid EnvironmentId { get; set; }

    /// <summary>Gets or sets the Environment PromotionOrder.</summary>
    int PromotionOrder { get; set; }

    /// <summary>Gets or sets the Environment ConnectionName.</summary>
    string ConnectionName { get; set; }

    /// <summary>Gets or sets the Environment RequiresApproval.</summary>
    bool RequiresApproval { get; set; }

    /// <summary>Gets or sets the Environment Approvers.</summary>
    IList<EnvironmentApproverConfiguration> Approvers { get; set; }

    /// <summary>Gets or sets the Environment Description.</summary>
    string? Description { get; set; }
}
