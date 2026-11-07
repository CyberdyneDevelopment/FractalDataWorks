using System;
using Fdw.Results;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A row could not be mapped on an input (FDW-2013). A component reads an input through a call that returns rows and nothing else, so the refusal
/// leaves it as an exception; the executor ends the run with the exception's message, which names the component, the input and the field.
/// It carries the failed result too (FDW-4197), so the executor keeps the cause of the refusal and the run is classified by it.
/// </summary>
internal sealed class FlowMappingRefusedException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="FlowMappingRefusedException"/> class.</summary>
    public FlowMappingRefusedException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowMappingRefusedException"/> class.</summary>
    /// <param name="message">Why the row was refused, as the log of the refusal says it.</param>
    public FlowMappingRefusedException(string? message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="FlowMappingRefusedException"/> class.</summary>
    /// <param name="message">Why the row was refused, as the log of the refusal says it.</param>
    /// <param name="failure">The failed result of the mapping, with the cause's messages first.</param>
    public FlowMappingRefusedException(string? message, IGenericResult failure)
        : base(message)
    {
        Failure = failure ?? throw new ArgumentNullException(nameof(failure));
    }

    /// <summary>Initializes a new instance of the <see cref="FlowMappingRefusedException"/> class.</summary>
    /// <param name="message">Why the row was refused.</param>
    /// <param name="innerException">What caused it.</param>
    public FlowMappingRefusedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Gets the failed result of the mapping; none for an exception made without one.</summary>
    public IGenericResult? Failure { get; }
}
