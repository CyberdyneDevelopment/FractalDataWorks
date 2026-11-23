using System;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// What a person states to answer a request (FDW-6142): the request in the route and their answer in the body, and nothing else. What they were shown and the arguments of the continuation were kept with the
/// request when the run asked; the person is the caller.
/// </summary>
public sealed class SubmitAnswerRequest
{
    /// <summary>Gets or sets the request's id.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the answer: any text that is not blank, or one of the request's options for a choice.</summary>
    public string Answer { get; set; } = string.Empty;
}
