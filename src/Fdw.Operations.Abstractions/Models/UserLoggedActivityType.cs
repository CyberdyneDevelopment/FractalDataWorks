using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>A user logged in.</summary>
[TypeOption(typeof(ActivityTypes), "UserLogged")]
[ExcludeFromCodeCoverage]
public sealed class UserLoggedActivityType : ActivityTypeBase
{
    /// <summary>Initializes a new instance of <see cref="UserLoggedActivityType"/>.</summary>
    public UserLoggedActivityType() : base(5, "UserLogged") { }
}
