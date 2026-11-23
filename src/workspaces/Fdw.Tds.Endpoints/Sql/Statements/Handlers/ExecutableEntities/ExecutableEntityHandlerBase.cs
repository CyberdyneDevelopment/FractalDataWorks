using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>The base of every handler of a thing an EXEC names, through <see cref="ExecutableEntityHandlerBase{TEntity}"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class ExecutableEntityHandlerBase : TypeOptionBase<ExecutableEntityHandlerBase>, IExecutableEntityHandler
{
    /// <summary>Initializes a new instance of the <see cref="ExecutableEntityHandlerBase"/> class.</summary>
    /// <param name="name">The option's name: the name of the parsed entity type it runs.</param>
    protected ExecutableEntityHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract Task<IGenericResult> Run(ExecutableEntity entity, SqlExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>The base of a handler of one parsed entity type.</summary>
/// <typeparam name="TEntity">The parsed entity type the handler runs.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class ExecutableEntityHandlerBase<TEntity> : ExecutableEntityHandlerBase
    where TEntity : ExecutableEntity
{
    /// <summary>Initializes a new instance of the <see cref="ExecutableEntityHandlerBase{TEntity}"/> class.</summary>
    /// <param name="name">The option's name: the name of <typeparamref name="TEntity"/>.</param>
    protected ExecutableEntityHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public sealed override Task<IGenericResult> Run(ExecutableEntity entity, SqlExecutionContext context, CancellationToken cancellationToken = default)
        => Handle((TEntity)entity, context, cancellationToken);

    /// <summary>Runs an entity of this handler's type.</summary>
    /// <param name="entity">The entity.</param>
    /// <param name="context">The request.</param>
    /// <param name="cancellationToken">A token the client's attention signal cancels.</param>
    /// <returns>How it ended, or the refusal.</returns>
    protected abstract Task<IGenericResult> Handle(TEntity entity, SqlExecutionContext context, CancellationToken cancellationToken);
}
