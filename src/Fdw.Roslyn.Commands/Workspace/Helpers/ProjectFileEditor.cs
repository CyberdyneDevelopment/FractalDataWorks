using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Fdw.Roslyn.Commands.Workspace.Helpers;

/// <summary>
/// Adds project and package references to a .csproj, honouring central package management.
/// </summary>
/// <remarks>
/// Edits the XML directly rather than shelling out to <c>dotnet add package</c>. The CLI always writes a
/// literal version and cannot express a property pin such as <c>$(FdwVersion)</c>, which is how every
/// consumer repo in this ecosystem pins the framework — and it writes immediately, which would defeat the
/// preview-then-approve flow this exists to serve.
/// </remarks>
public static class ProjectFileEditor
{
    /// <summary>
    /// Adds a ProjectReference to a project file if it is not already present.
    /// </summary>
    /// <param name="projectFilePath">The consuming .csproj.</param>
    /// <param name="referencedProjectFilePath">The .csproj being referenced.</param>
    /// <returns>The outcome.</returns>
    public static ProjectFileEditResult AddProjectReference(
        string projectFilePath,
        string referencedProjectFilePath)
    {
        if (!File.Exists(projectFilePath))
            return ProjectFileEditResult.Failed($"Project file not found: {projectFilePath}");

        var relative = MakeRelative(projectFilePath, referencedProjectFilePath);
        var document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);

        if (HasItem(document, "ProjectReference", relative))
            return ProjectFileEditResult.AlreadyPresent(projectFilePath);

        AddItem(document, new XElement("ProjectReference", new XAttribute("Include", relative)));
        document.Save(projectFilePath);

        return ProjectFileEditResult.Written(projectFilePath, $"ProjectReference Include=\"{relative}\"");
    }

    /// <summary>
    /// Adds a PackageReference, putting the version wherever this repo's convention says it belongs.
    /// </summary>
    /// <param name="projectFilePath">The consuming .csproj.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="versionPin">A literal version, or an MSBuild property such as "$(FdwVersion)".</param>
    /// <param name="centralPackageManagement">Whether central package management is in force.</param>
    /// <param name="packagesPropsPath">The Directory.Packages.props path, required under CPM.</param>
    /// <returns>The outcome.</returns>
    /// <remarks>
    /// Under CPM the csproj carries a version-less PackageReference and the version lives in
    /// Directory.Packages.props; writing a version in both places is what NuGet errors on (NU1008).
    /// </remarks>
    public static ProjectFileEditResult AddPackageReference(
        string projectFilePath,
        string packageId,
        string versionPin,
        bool centralPackageManagement,
        string? packagesPropsPath)
    {
        if (!File.Exists(projectFilePath))
            return ProjectFileEditResult.Failed($"Project file not found: {projectFilePath}");
        if (string.IsNullOrWhiteSpace(packageId))
            return ProjectFileEditResult.Failed("Package id is required");
        if (string.IsNullOrWhiteSpace(versionPin))
            return ProjectFileEditResult.Failed($"A version pin is required for package '{packageId}'");

        var document = XDocument.Load(projectFilePath, LoadOptions.PreserveWhitespace);
        var describe = $"PackageReference Include=\"{packageId}\"";

        if (!HasItem(document, "PackageReference", packageId))
        {
            var element = new XElement("PackageReference", new XAttribute("Include", packageId));
            if (!centralPackageManagement)
            {
                element.Add(new XAttribute("Version", versionPin));
                describe += $" Version=\"{versionPin}\"";
            }

            AddItem(document, element);
            document.Save(projectFilePath);
        }
        else if (!centralPackageManagement)
        {
            return ProjectFileEditResult.AlreadyPresent(projectFilePath);
        }

        if (!centralPackageManagement)
            return ProjectFileEditResult.Written(projectFilePath, describe);

        if (string.IsNullOrWhiteSpace(packagesPropsPath) || !File.Exists(packagesPropsPath))
            return ProjectFileEditResult.Failed(
                $"Central package management is on but Directory.Packages.props was not found for '{packageId}'");

        return AddPackageVersion(packagesPropsPath!, packageId, versionPin, projectFilePath, describe);
    }

    /// <summary>
    /// Finds the nearest Directory.Packages.props at or above a directory.
    /// </summary>
    /// <param name="startDirectory">The directory to search upward from.</param>
    /// <returns>The path, or <see langword="null"/> when none exists.</returns>
    public static string? FindPackagesProps(string? startDirectory)
    {
        var directory = startDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            var candidate = Path.Combine(directory!, "Directory.Packages.props");
            if (File.Exists(candidate)) return candidate;
            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    /// <summary>
    /// Determines whether central package management is switched on for a props file.
    /// </summary>
    /// <param name="packagesPropsPath">The Directory.Packages.props path.</param>
    /// <returns><see langword="true"/> when CPM is enabled.</returns>
    public static bool IsCentralPackageManagement(string? packagesPropsPath)
    {
        if (string.IsNullOrWhiteSpace(packagesPropsPath) || !File.Exists(packagesPropsPath)) return false;

        return XDocument.Load(packagesPropsPath)
            .Descendants()
            .Any(e => string.Equals(e.Name.LocalName, "ManagePackageVersionsCentrally", StringComparison.Ordinal)
                   && string.Equals(e.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase));
    }

    private static ProjectFileEditResult AddPackageVersion(
        string packagesPropsPath,
        string packageId,
        string versionPin,
        string projectFilePath,
        string describe)
    {
        var props = XDocument.Load(packagesPropsPath, LoadOptions.PreserveWhitespace);

        if (HasItem(props, "PackageVersion", packageId))
            return ProjectFileEditResult.Written(projectFilePath, describe + " (version already pinned centrally)");

        AddItem(props, new XElement(
            "PackageVersion",
            new XAttribute("Include", packageId),
            new XAttribute("Version", versionPin)));

        props.Save(packagesPropsPath);

        return ProjectFileEditResult.Written(
            projectFilePath,
            describe + $" + PackageVersion Include=\"{packageId}\" Version=\"{versionPin}\" in {Path.GetFileName(packagesPropsPath)}");
    }

    private static bool HasItem(XDocument document, string itemName, string include) =>
        document.Descendants()
            .Where(e => string.Equals(e.Name.LocalName, itemName, StringComparison.Ordinal))
            .Any(e => string.Equals((string?)e.Attribute("Include"), include, StringComparison.OrdinalIgnoreCase));

    // Why: append into the last matching ItemGroup rather than creating a new one each time, so repeated
    // repairs do not leave a project file littered with single-entry groups.
    private static void AddItem(XDocument document, XElement item)
    {
        var root = document.Root ?? throw new InvalidOperationException("Project file has no root element");
        var name = item.Name.LocalName;

        var group = root.Elements()
            .LastOrDefault(e => string.Equals(e.Name.LocalName, "ItemGroup", StringComparison.Ordinal)
                             && e.Elements().Any(c => string.Equals(c.Name.LocalName, name, StringComparison.Ordinal)));

        if (group is null)
        {
            group = new XElement("ItemGroup");
            root.Add(group);
        }

        group.Add(item);
    }

    private static string MakeRelative(string fromProjectFile, string toProjectFile)
    {
        var fromDirectory = Path.GetDirectoryName(Path.GetFullPath(fromProjectFile));
        if (string.IsNullOrEmpty(fromDirectory)) return toProjectFile;

        return Path.GetRelativePath(fromDirectory!, Path.GetFullPath(toProjectFile))
            .Replace('/', '\\');
    }
}
