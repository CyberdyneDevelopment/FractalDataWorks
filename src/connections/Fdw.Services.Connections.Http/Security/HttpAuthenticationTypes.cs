using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// TypeCollection for HTTP security configurations.
/// Each entry is both a TypeOption (identity + factory) and a configuration type
/// (bindable properties).
///
/// The source generator populates ByName(), ById(), All(), NotFound() at compile time.
///
/// Usage:
///   var prototype = HttpAuthenticationTypes.ByName("WsSecurity");
///   var instance = prototype.CreateInstance();
///   instance.Bind(configSection);
/// </summary>
[TypeCollection(
    typeof(HttpAuthenticationConfiguration),
    typeof(HttpAuthenticationConfiguration),
    typeof(HttpAuthenticationTypes))]
public abstract partial class HttpAuthenticationTypes
    : TypeCollectionBase<HttpAuthenticationConfiguration, HttpAuthenticationConfiguration>
{
    /// <summary>
    /// The header an API key is sent in when the connection's <c>ApiKeyHeaderName</c> is not set.
    /// A deployment whose APIs expect another header sets this before the host registers services.
    /// </summary>
    public static string ApiKeyHeaderName { get; set; } = "X-API-Key";
}
