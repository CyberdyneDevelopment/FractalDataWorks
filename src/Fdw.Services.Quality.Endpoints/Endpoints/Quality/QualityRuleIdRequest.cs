using System;
using System.Collections.Generic;
namespace Fdw.Services.Quality.Endpoints;

/// <summary>Request containing a quality rule identifier.</summary>
public class QualityRuleIdRequest
{
    /// <summary>Gets or sets the unique identifier of the quality rule.</summary>
    public Guid Id { get; set; }
}