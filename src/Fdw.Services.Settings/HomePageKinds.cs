namespace Fdw.Services.Settings;

/// <summary>The kinds of scope that hold a home page (FDW-2325).</summary>
public static class HomePageKinds
{
    /// <summary>A person's own page.</summary>
    public const string Person = "Person";

    /// <summary>An organization's page.</summary>
    public const string Organization = "Organization";

    /// <summary>A tenant's page.</summary>
    public const string Tenant = "Tenant";

    /// <summary>What the page that applies is when no scope holds a published page.</summary>
    public const string None = "None";
}
