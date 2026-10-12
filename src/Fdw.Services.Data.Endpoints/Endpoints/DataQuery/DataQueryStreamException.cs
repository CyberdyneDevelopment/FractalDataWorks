using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Results;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Thrown from the rows of a <see cref="DataQueryStream"/> when a batch fails after the response has started (FDW-1209).
/// </summary>
/// <remarks>
/// Why an exception and not an early end of the rows: the rows end an array, and an array that ends closes the JSON, so a response
/// cut short by a failure read as a whole one with a 200 on it. The serializer cannot close what an exception has torn through,
/// so the body is left unfinished and the endpoint that catches this aborts the connection: an HTTP client or a parser throws
/// instead of taking a part of the rows for all of them. It is the signal between the stream and the endpoint that wrote it and
/// is caught there; it is not part of what a caller sees.
/// </remarks>
[Serializable]
[ExcludeFromCodeCoverage]
public class DataQueryStreamException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DataQueryStreamException"/> class.</summary>
    public DataQueryStreamException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataQueryStreamException"/> class with a message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public DataQueryStreamException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataQueryStreamException"/> class with a message and the exception that caused it.</summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this one.</param>
    public DataQueryStreamException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataQueryStreamException"/> class for a batch that failed.</summary>
    /// <param name="dataSetName">The data set being read.</param>
    /// <param name="rowsWritten">How many rows had been handed to the response before the failure.</param>
    /// <param name="failure">The failed batch's result.</param>
    public DataQueryStreamException(string dataSetName, int rowsWritten, IGenericResult failure)
        : base($"The stream of data set '{dataSetName}' failed after {rowsWritten} rows: {failure.CurrentMessage}")
    {
        DataSetName = dataSetName;
        RowsWritten = rowsWritten;
        Failure = failure;
    }

    /// <summary>Gets the data set being read, or null when the exception was not made for a failed batch.</summary>
    public string? DataSetName { get; }

    /// <summary>Gets how many rows had been handed to the response before the failure.</summary>
    public int RowsWritten { get; }

    /// <summary>Gets the failed batch's result, which maps to the status the caller is told when nothing has been sent yet.</summary>
    public IGenericResult? Failure { get; }
}
