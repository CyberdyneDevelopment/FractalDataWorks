namespace Fdw.Documents.Records.Abstractions.Links;

/// <summary>The target kinds of the record layer itself (FDW-3397), which every <see cref="ILinkTargets"/> answers for; an application adds its own.</summary>
public static class LinkTargetKinds
{
    /// <summary>Another document of the organization.</summary>
    public const string Document = "Document";

    /// <summary>A record node of the organization.</summary>
    public const string Record = "Record";
}
