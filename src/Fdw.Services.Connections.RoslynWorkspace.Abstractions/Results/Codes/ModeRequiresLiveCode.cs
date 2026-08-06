using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.Results.Codes;

/// <summary>
/// The requested operation requires Live mode but the connection is in Snapshot mode.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceResultCodes), "ModeRequiresLive", RestrictToCurrentCompilation = true)]
public sealed class ModeRequiresLiveCode : RoslynWorkspaceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModeRequiresLiveCode"/> class.
    /// </summary>
    public ModeRequiresLiveCode()
        : base(
            40000,
            "ModeRequiresLive",
            ResultSeverities.ByName("Error"),
            "Operation requires Live mode; current Mode is Snapshot")
    {
    }
}
