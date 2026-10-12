using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Protocols.Tds;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Statements.Handlers;

/// <summary>
/// DECLARE @name type [= value], ...: variables for the rest of the batch.
/// </summary>
[TypeOption(typeof(SqlStatementHandlers), "DeclareVariable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeclareVariableHandler : SqlStatementHandlerBase<DeclareVariableStatement>
{
    /// <summary>Initializes a new instance of the <see cref="DeclareVariableHandler"/> class.</summary>
    public DeclareVariableHandler()
        : base("DeclareVariable")
    {
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<SqlStatementOutcome>> Run(DeclareVariableStatement statement, SqlExecutionContext context, CancellationToken cancellationToken)
    {
        foreach (var declaration in statement.Declarations)
        {
            var declared = Declare(declaration, context);
            if (declared.IsFailure)
                return Task.FromResult(declared.ToNewResult<SqlStatementOutcome>());
        }

        return Ran();
    }

    /// <summary>Declares one variable, converting its initial value to its type.</summary>
    /// <param name="declaration">The declaration as parsed.</param>
    /// <param name="context">The request.</param>
    /// <returns>Success, error 134 for a name already declared, or the conversion's refusal.</returns>
    public static IGenericResult Declare(DeclareVariableElement declaration, SqlExecutionContext context)
    {
        System.ArgumentNullException.ThrowIfNull(declaration);
        System.ArgumentNullException.ThrowIfNull(context);
        var name = declaration.VariableName.Value;
        if (context.Variables.IsDeclared(name))
        {
            return SqlRefusal.Refuse(new SqlServerMessage(134, 1, 15,
                $"The variable name '{name}' has already been declared. Variable names must be unique within a query batch or stored procedure.",
                declaration.StartLine), context.Logger);
        }

        var type = SqlConversions.Resolve(declaration.DataType, inCast: false, declaration.StartLine, context.Logger);
        if (type.IsFailure)
            return type;

        context.Variables.Declare(name, type.Value!, null);
        return declaration.Value is null
            ? GenericResult.Success()
            : SqlAssignment.Assign(new VariableReference { Name = name }, declaration.Value, SqlScope.Of(context));
    }
}
