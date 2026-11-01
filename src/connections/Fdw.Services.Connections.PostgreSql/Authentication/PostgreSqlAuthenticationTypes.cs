using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.PostgreSql.Authentication;

/// <summary>
/// TypeCollection of PostgreSql authentication configurations.
/// Each entry is both a TypeOption (identity + factory) and a configuration type
/// (bindable properties + behavior).
///
/// The source generator populates ByName(), ById(), All(), NotFound() at compile time.
///
/// Usage:
///   var prototype = PostgreSqlAuthenticationTypes.ByName("Password");
///   var instance = prototype.CreateInstance();
///   // bind instance properties from IConfiguration
///   var fragment = instance.BuildAuthFragment(values, resolvedPassword);
/// </summary>
[TypeCollection(
    typeof(PostgreSqlAuthenticationConfiguration),
    typeof(PostgreSqlAuthenticationConfiguration),
    typeof(PostgreSqlAuthenticationTypes))]
public abstract partial class PostgreSqlAuthenticationTypes
    : TypeCollectionBase<PostgreSqlAuthenticationConfiguration, PostgreSqlAuthenticationConfiguration>
{
}
