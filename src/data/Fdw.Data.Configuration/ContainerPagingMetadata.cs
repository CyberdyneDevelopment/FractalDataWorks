namespace Fdw.Services.Connections;

/// <summary>
/// The keys under which a container's own paging reaches the runtime container's <c>Metadata</c> (FDW-5562), declared once for the code that writes them
/// (<c>ContainerComposition</c>) and the code that reads them (the HTTP connection).
/// </summary>
public static class ContainerPagingMetadata
{
    /// <summary>The metadata key of the container's paging style. Absent when the container states none and takes its connection's.</summary>
    public const string Style = "PagingStyle";

    /// <summary>The prefix of the metadata key of each paging setting the container states: the prefix, then the setting's name.</summary>
    public const string SettingPrefix = "PagingSetting.";
}
