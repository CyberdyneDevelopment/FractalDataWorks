using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;

/// <summary>
/// AwaitingApproval - the run was started but its project requires approval to run (FDW-1740), so it waits for someone who holds
/// the permission to approve it, which returns it to Scheduled and queues it, or to deny it, which cancels it.
/// </summary>
[TypeOption(typeof(ExecutionStateTypes), "AwaitingApproval", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class AwaitingApprovalStateType : ExecutionStateTypeBase
{
    private static readonly string[] ValidTransitionsArray = new[] { "Scheduled", "Cancelled" };

    /// <summary>
    /// Initializes a new instance of the <see cref="AwaitingApprovalStateType"/> class.
    /// </summary>
    public AwaitingApprovalStateType()
        : base(
            name: "AwaitingApproval",
            displayName: "Awaiting approval",
            isTerminal: false,
            isSuccess: false,
            isFailure: false,
            canTriggerEscalation: false,
            validTransitions: ValidTransitionsArray)
    {
    }
}
