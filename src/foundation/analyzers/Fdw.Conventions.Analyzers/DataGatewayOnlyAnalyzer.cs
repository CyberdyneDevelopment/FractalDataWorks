using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// FDW052: data access goes through the DataGateway. Reports creating a DbConnection, DbCommand,
/// DbConnectionStringBuilder or DbDataSource (or any type deriving from one), and calling a DbCommand's
/// ExecuteReader, ExecuteScalar or ExecuteNonQuery (sync or async), in an assembly that does not declare
/// <c>[assembly: Fdw.Conventions.DataAccessAdapter]</c>.
/// </summary>
/// <remarks>
/// Why base types and not provider names: matching what a type derives from covers SqlClient, Npgsql, Sqlite and
/// any provider added later, with no list to keep up to date (FDW-3579).
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataGatewayOnlyAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for data access outside the DataGateway.
    /// </summary>
    public const string DiagnosticId = "FDW052";

    private const string AdapterAttributeName = "Fdw.Conventions.DataAccessAdapterAttribute";

    private static readonly string[] DataAccessBaseTypes =
    [
        "System.Data.Common.DbConnection",
        "System.Data.Common.DbCommand",
        "System.Data.Common.DbConnectionStringBuilder",
        "System.Data.Common.DbDataSource",
    ];

    private static readonly ImmutableHashSet<string> ExecuteMethods = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "ExecuteReader",
        "ExecuteReaderAsync",
        "ExecuteScalar",
        "ExecuteScalarAsync",
        "ExecuteNonQuery",
        "ExecuteNonQueryAsync");

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Data access outside the DataGateway",
        "'{0}' opens data access outside the DataGateway: use IDataGateway.Execute with a command object, or declare [assembly: DataAccessAdapter] if this assembly is a connection or storage adapter",
        "Design",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All data operations go through IDataGateway.Execute with command objects. Only an assembly that declares itself a connection or storage adapter with [assembly: Fdw.Conventions.DataAccessAdapter] may create ADO.NET connections and commands or execute them.");

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(start =>
        {
            if (start.Compilation.Assembly.GetAttributes()
                .Any(a => string.Equals(a.AttributeClass?.ToDisplayString(), AdapterAttributeName, StringComparison.Ordinal)))
                return;

            var baseTypes = DataAccessBaseTypes
                .Select(name => start.Compilation.GetTypeByMetadataName(name))
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();

            if (baseTypes.IsEmpty)
                return;

            var dbCommand = start.Compilation.GetTypeByMetadataName("System.Data.Common.DbCommand");

            start.RegisterOperationAction(
                operationContext => AnalyzeCreation(operationContext, baseTypes),
                OperationKind.ObjectCreation);

            if (dbCommand != null)
            {
                start.RegisterOperationAction(
                    operationContext => AnalyzeInvocation(operationContext, dbCommand),
                    OperationKind.Invocation);
            }
        });
    }

    private static void AnalyzeCreation(OperationAnalysisContext context, ImmutableArray<INamedTypeSymbol> baseTypes)
    {
        var created = ((IObjectCreationOperation)context.Operation).Type;
        if (created is null || !baseTypes.Any(baseType => IsOrDerivesFrom(created, baseType)))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Operation.Syntax.GetLocation(), created.Name));
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol dbCommand)
    {
        var method = ((IInvocationOperation)context.Operation).TargetMethod;
        if (!ExecuteMethods.Contains(method.Name) || !IsOrDerivesFrom(method.ContainingType, dbCommand))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Rule,
            context.Operation.Syntax.GetLocation(),
            method.ContainingType.Name + "." + method.Name));
    }

    private static bool IsOrDerivesFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType))
                return true;
        }

        return false;
    }
}
