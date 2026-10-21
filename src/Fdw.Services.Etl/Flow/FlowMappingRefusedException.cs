using System;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A row could not be mapped on an input (FDW-2013). A component reads an input through a call that returns rows and nothing else, so the refusal
/// leaves it as an exception; the executor ends the run with the exception's message, which names the component, the input and the field.
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
    /// <param name="message">Why the row was refused.</param>
    /// <param name="innerException">What caused it.</param>
    public FlowMappingRefusedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
