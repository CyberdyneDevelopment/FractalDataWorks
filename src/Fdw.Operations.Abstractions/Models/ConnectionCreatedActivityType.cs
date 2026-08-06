using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>A new connection was created.</summary>
[TypeOption(typeof(ActivityTypes), "ConnectionCreated")]
[ExcludeFromCodeCoverage]
public sealed class ConnectionCreatedActivityType : ActivityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="ConnectionCreatedActivityType"/>.</summary>
    public ConnectionCreatedActivityType() : base(4, "ConnectionCreated") { }
}
