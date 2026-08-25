using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Fdw.CodeBuilder.Analysis.CSharp;

/// <summary>
/// Exception thrown when expectations are not met in test assertions.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class ExpectationFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpectationFailedException"/> class.
    /// </summary>
    public ExpectationFailedException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpectationFailedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ExpectationFailedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpectationFailedException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ExpectationFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpectationFailedException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected ExpectationFailedException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
