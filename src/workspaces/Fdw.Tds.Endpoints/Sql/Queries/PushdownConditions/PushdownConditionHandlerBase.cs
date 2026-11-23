using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>The base of every handler of a kind of WHERE condition, through <see cref="PushdownConditionHandlerBase{TCondition}"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class PushdownConditionHandlerBase : TypeOptionBase<PushdownConditionHandlerBase>, IPushdownConditionHandler
{
    /// <summary>Initializes a new instance of the <see cref="PushdownConditionHandlerBase"/> class.</summary>
    /// <param name="name">The option's name: the name of the parsed condition type it translates.</param>
    protected PushdownConditionHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<QueryFilterNode> Translate(BooleanExpression condition, SqlTable table, string qualifier, SqlScope scope);
}

/// <summary>The base of a handler of one parsed condition type.</summary>
/// <typeparam name="TCondition">The parsed condition type the handler translates.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class PushdownConditionHandlerBase<TCondition> : PushdownConditionHandlerBase
    where TCondition : BooleanExpression
{
    /// <summary>Initializes a new instance of the <see cref="PushdownConditionHandlerBase{TCondition}"/> class.</summary>
    /// <param name="name">The option's name: the name of <typeparamref name="TCondition"/>.</param>
    protected PushdownConditionHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public sealed override IGenericResult<QueryFilterNode> Translate(BooleanExpression condition, SqlTable table, string qualifier, SqlScope scope)
        => Handle((TCondition)condition, table, qualifier, scope);

    /// <summary>Translates a condition of this handler's type.</summary>
    /// <param name="condition">The condition.</param>
    /// <param name="table">The table it filters.</param>
    /// <param name="qualifier">The alias or name the query qualifies the table's columns by.</param>
    /// <param name="scope">The scope its values are evaluated in.</param>
    /// <returns>The filter tree, or the refusal.</returns>
    protected abstract IGenericResult<QueryFilterNode> Handle(TCondition condition, SqlTable table, string qualifier, SqlScope scope);
}
