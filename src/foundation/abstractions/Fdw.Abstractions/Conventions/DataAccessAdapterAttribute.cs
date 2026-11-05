using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Conventions;

/// <summary>
/// Declares that an assembly is a connection or storage adapter: it may open connections and run commands with
/// ADO.NET directly. Everywhere else, data access goes through IDataGateway.Execute with a command object, and
/// FDW052 reports a DbConnection, DbCommand, connection-string builder or data source created there.
/// </summary>
/// <remarks>
/// Why an assembly attribute and not a folder or name rule: an adapter says what it is, and the analyzer reads that
/// from the compilation. A project declares it in its csproj with
/// <c>&lt;AssemblyAttribute Include="Fdw.Conventions.DataAccessAdapterAttribute" /&gt;</c> (FDW-3579).
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class DataAccessAdapterAttribute : Attribute
{
}
