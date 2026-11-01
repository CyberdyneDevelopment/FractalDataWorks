using System;
using System.Linq;

namespace Fdw.Documents.Abstractions.Commands;

/// <summary>The base of the document commands.</summary>
public abstract class DocumentCommandBase : IDocumentCommand
{
    /// <summary>Initializes the sentinel the collection answers for an unknown name.</summary>
    protected DocumentCommandBase()
        : this("NotFound")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentCommandBase"/> class.</summary>
    /// <param name="commandType">The command's name.</param>
    protected DocumentCommandBase(string commandType)
    {
        CommandId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CommandType = commandType;
    }

    /// <inheritdoc />
    public Guid CommandId { get; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; }

    /// <inheritdoc />
    public string CommandType { get; }

    /// <inheritdoc />
    public string Category => "Document";

    /// <inheritdoc />
    public virtual string? FirstProblem() => null;

    /// <summary>The first of the problems that is one.</summary>
    /// <param name="problems">The problems found, each null when there is none.</param>
    /// <returns>The first problem, or null when there are none.</returns>
    protected static string? First(params string?[] problems) => problems.FirstOrDefault(problem => problem is not null);

    /// <summary>Says what is wrong when a path names the scope itself where a document is needed.</summary>
    /// <param name="path">The path.</param>
    /// <param name="what">What the path is, for the message.</param>
    /// <returns>The problem, or null when the path names a document.</returns>
    protected static string? NotADocument(DocumentPath path, string what) =>
        path.IsRoot ? $"the {what} names the scope itself, not a document" : null;
}

/// <summary>A document command that answers with a result of one type.</summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public abstract class DocumentCommandBase<TResult> : DocumentCommandBase, IDocumentCommand<TResult>
{
    /// <summary>Initializes the sentinel.</summary>
    protected DocumentCommandBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentCommandBase{TResult}"/> class.</summary>
    /// <param name="commandType">The command's name.</param>
    protected DocumentCommandBase(string commandType)
        : base(commandType)
    {
    }
}
