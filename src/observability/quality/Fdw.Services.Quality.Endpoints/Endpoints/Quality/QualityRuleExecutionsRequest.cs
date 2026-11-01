using System;

namespace Fdw.Services.Quality.Endpoints;

/// <summary>Request for the recorded executions of one quality rule.</summary>
public class QualityRuleExecutionsRequest
{
    /// <summary>Gets or sets the unique identifier of the quality rule.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets how many of the newest executions to answer, from 1 to 500. Required.</summary>
    public int Take { get; set; }
}
