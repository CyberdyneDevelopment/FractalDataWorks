using System.Diagnostics.CodeAnalysis;
using Fdw.Results.Abstractions;

namespace Fdw.Documents.Abstractions.Results;

/// <summary>The base of the result codes of anticipated document failures (FDW-1500).</summary>
[ExcludeFromCodeCoverage]
public abstract class DocumentResultCodeBase : ResultCodeBase
{
    /// <summary>Initializes the sentinel the collection answers for an unknown name.</summary>
    protected DocumentResultCodeBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentResultCodeBase"/> class.</summary>
    /// <param name="number">The code's number; its category is the number divided by ten thousand.</param>
    /// <param name="name">The code's name.</param>
    /// <param name="severity">How serious the failure is.</param>
    /// <param name="messageTemplate">The message, with <c>{Name}</c> placeholders the details fill.</param>
    /// <param name="isRetryable">Whether trying again can succeed.</param>
    protected DocumentResultCodeBase(int number, string name, IResultSeverity severity, string messageTemplate, bool isRetryable = false)
        : base(number, name, severity, messageTemplate, "DOCUMENTS", isRetryable)
    {
    }
}
