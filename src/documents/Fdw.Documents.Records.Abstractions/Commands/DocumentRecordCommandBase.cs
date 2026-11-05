using System;
using System.Linq;

namespace Fdw.Documents.Records.Abstractions.Commands;

/// <summary>The base of the document record commands (FDW-3673).</summary>
public abstract class DocumentRecordCommandBase : IDocumentRecordCommand
{
    /// <summary>Initializes the sentinel the collection answers for an unknown name.</summary>
    protected DocumentRecordCommandBase()
        : this("NotFound")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentRecordCommandBase"/> class.</summary>
    /// <param name="commandType">The command's name.</param>
    protected DocumentRecordCommandBase(string commandType)
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
    public string Category => "DocumentRecord";

    /// <inheritdoc />
    public virtual string? FirstProblem() => null;

    /// <summary>The first of the problems that is one.</summary>
    /// <param name="problems">The problems found, each null when there is none.</param>
    /// <returns>The first problem, or null when there are none.</returns>
    protected static string? First(params string?[] problems) => problems.FirstOrDefault(problem => problem is not null);

    /// <summary>Says what is wrong when no organization is named.</summary>
    /// <param name="organizationId">The organization.</param>
    /// <returns>The problem, or null.</returns>
    protected static string? OrganizationProblem(Guid organizationId) =>
        organizationId == Guid.Empty ? "the organization is not named" : null;

    /// <summary>Says what is wrong when no tenant is named.</summary>
    /// <param name="tenantId">The tenant.</param>
    /// <returns>The problem, or null.</returns>
    protected static string? TenantProblem(Guid tenantId) =>
        tenantId == Guid.Empty ? "the tenant is not named" : null;

    /// <summary>Says what is wrong with a public id: it must be a UUIDv7 the application set (<see cref="PublicIds.Check"/>).</summary>
    /// <param name="id">The id.</param>
    /// <param name="entity">What the id is of.</param>
    /// <returns>The problem, or null.</returns>
    protected static string? IdProblem(Guid id, string entity) =>
        PublicIds.Check(id, entity).IsFailure ? $"the {entity} id '{id}' is not a UUIDv7 the application set" : null;

    /// <summary>Says what is wrong with an entity a write carries: missing, or a public id that is not a UUIDv7.</summary>
    /// <typeparam name="TEntity">The entity.</typeparam>
    /// <param name="entity">The entity.</param>
    /// <param name="id">Reads the entity's public id.</param>
    /// <param name="what">What the entity is.</param>
    /// <returns>The problem, or null.</returns>
    protected static string? EntityProblem<TEntity>(TEntity? entity, Func<TEntity, Guid> id, string what)
        where TEntity : class
        => entity is null ? $"the {what} is missing" : IdProblem(id(entity), what);

    /// <summary>Says what is wrong with an expected revision.</summary>
    /// <param name="expectedRevision">The revision.</param>
    /// <returns>The problem, or null.</returns>
    protected static string? RevisionProblem(long expectedRevision) =>
        expectedRevision < 0 ? $"the expected revision {expectedRevision} is negative" : null;
}

/// <summary>A document record command that answers with a result of one type.</summary>
/// <typeparam name="TResult">What the command answers with.</typeparam>
public abstract class DocumentRecordCommandBase<TResult> : DocumentRecordCommandBase, IDocumentRecordCommand<TResult>
{
    /// <summary>Initializes the sentinel.</summary>
    protected DocumentRecordCommandBase()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DocumentRecordCommandBase{TResult}"/> class.</summary>
    /// <param name="commandType">The command's name.</param>
    protected DocumentRecordCommandBase(string commandType)
        : base(commandType)
    {
    }
}
