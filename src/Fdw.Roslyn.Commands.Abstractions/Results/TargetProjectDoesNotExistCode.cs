using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// The namespace names a project that does not exist, so the types have nowhere to move to.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TargetProjectDoesNotExist", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TargetProjectDoesNotExistCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetProjectDoesNotExistCode"/> class.
    /// </summary>
    public TargetProjectDoesNotExistCode()
        : base(31024, "TargetProjectDoesNotExist",
            ResultSeverities.ByName("Error"),
            "No project named '{TargetProject}' exists, so {TypeCount} type(s) in namespace '{Namespace}' cannot be moved into one. Either create project '{TargetProject}', or use MoveNamespace to rename '{Namespace}' to match where the types already live (currently '{CurrentProject}'). MoveNamespace is consumer-breaking and changes TypeOption Ids derived from the FQN; creating the project and moving is not.",
            isRetryable: false)
    {
    }
}
