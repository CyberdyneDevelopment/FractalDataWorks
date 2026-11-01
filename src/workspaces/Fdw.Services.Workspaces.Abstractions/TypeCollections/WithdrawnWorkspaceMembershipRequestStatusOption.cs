using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Pulled back by the person who asked, before anyone reviewed it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceMembershipRequestStatuses), "Withdrawn")]
public sealed class WithdrawnWorkspaceMembershipRequestStatusOption : WorkspaceMembershipRequestStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="WithdrawnWorkspaceMembershipRequestStatusOption"/> class.</summary>
    public WithdrawnWorkspaceMembershipRequestStatusOption() : base("Withdrawn")
    {
    }
}
