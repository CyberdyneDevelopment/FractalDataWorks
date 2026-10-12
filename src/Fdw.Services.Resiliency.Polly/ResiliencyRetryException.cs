using System;

namespace Fdw.Services.Resiliency.Polly;

/// <summary>
/// Internal exception used to signal Polly to trigger a retry.
/// Thrown when a stage delegate returns a failure result.
/// </summary>
/// <remarks>
/// Why: Polly v8 retry strategy is exception-triggered. This exception bridges
/// the FDW <see cref="Fdw.Results.IGenericResult"/> failure pattern
/// into Polly's exception-based retry signal.
/// </remarks>
internal sealed class ResiliencyRetryException : Exception
{
    /// <summary>Initializes a new instance of <see cref="ResiliencyRetryException"/>.</summary>
    public ResiliencyRetryException()
    {
    }

    /// <summary>Initializes a new instance of <see cref="ResiliencyRetryException"/>.</summary>
    public ResiliencyRetryException(string message) : base(message)
    {
    }

    /// <summary>Initializes a new instance of <see cref="ResiliencyRetryException"/>.</summary>
    public ResiliencyRetryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
