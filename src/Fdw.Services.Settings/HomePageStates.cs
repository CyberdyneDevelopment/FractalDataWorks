namespace Fdw.Services.Settings;

/// <summary>The states a home page is held in: the draft being edited and the published page people see (FDW-2325).</summary>
public static class HomePageStates
{
    /// <summary>The page being edited; nobody but its editors sees it.</summary>
    public const string Draft = "Draft";

    /// <summary>The page everyone it applies to sees.</summary>
    public const string Published = "Published";
}
