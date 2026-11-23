using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.CommandVerbs;

/// <summary>
/// What an HTTP protocol writes for each data command type, found by the command's own type name (<c>ByName(command.CommandType)</c>).
/// A command type from another assembly adds an option; a type with no option gets <c>NotFound</c> and the request is refused as
/// CommandTypeUnsupported.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(HttpCommandVerbBase), typeof(IHttpCommandVerb), typeof(HttpCommandVerbs))]
public abstract partial class HttpCommandVerbs : TypeCollectionBase<HttpCommandVerbBase, IHttpCommandVerb>
{
}
