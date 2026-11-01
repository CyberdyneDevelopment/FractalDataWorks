using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.MsSql.Authentication;

/// <summary>
/// TypeCollection of MsSql authentication configurations.
/// Each entry is both a TypeOption (identity + factory) and a configuration type
/// (bindable properties + behavior).
///
/// The source generator populates ByName(), ById(), All(), NotFound() at compile time.
///
/// Usage:
///   var prototype = MsSqlAuthenticationTypes.ByName("SqlAuth");
///   var instance = prototype.CreateInstance();
///   // bind instance properties from IConfiguration
///   var fragment = instance.BuildAuthFragment(resolvedPassword);
/// </summary>
[TypeCollection(
    typeof(MsSqlAuthenticationConfiguration),
    typeof(MsSqlAuthenticationConfiguration),
    typeof(MsSqlAuthenticationTypes))]
public abstract partial class MsSqlAuthenticationTypes
    : TypeCollectionBase<MsSqlAuthenticationConfiguration, MsSqlAuthenticationConfiguration>
{
}
