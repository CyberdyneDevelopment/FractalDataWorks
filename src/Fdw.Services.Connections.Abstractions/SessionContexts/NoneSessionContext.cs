using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// The connection kind carries no session context: it never describes the calling principal to the
/// store it opens, and nothing is written on connection open.
/// </summary>
/// <remarks>
/// The sole member of <see cref="NoSessionContextTypes"/>. A connection type whose
/// <c>SessionContextTypes</c> resolves to this collection needs no authentication-context accessor
/// registered and no boot-time elevation — a host built only from such kinds (a FileSystem-only UI
/// host, for example) demands neither.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(NoSessionContextTypes), "None")]
public sealed class NoneSessionContext() : NoSessionContextBase(1, "None");
