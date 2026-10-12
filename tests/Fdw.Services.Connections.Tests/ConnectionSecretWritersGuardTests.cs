using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fdw.Services.Connections.Tests;

/// <summary>
/// FDW-1202: a secret is added, replaced and deleted only through <c>ConnectionCredentialStore</c>, inside creating,
/// updating or deleting a connection. No other source file builds a command that writes or deletes a secret; the secret
/// manager project defines them, and the store is the one place a connection uses them.
/// </summary>
public partial class ConnectionSecretWritersGuardTests
{
    private const string StoreFile = "ConnectionCredentialStore.cs";
    private const string DefiningProject = "Fdw.Services.SecretManagers";

    [Fact]
    [Trait("Category", "Convention")]
    public void OnlyTheCredentialStoreWritesOrDeletesSecrets()
    {
        var offenders = SourceFiles()
            .Where(file => !Path.GetFileName(file).Equals(StoreFile, StringComparison.Ordinal))
            .Where(file => !IsInDefiningProject(file))
            .Where(file => File.ReadLines(file).Any(line => IsCode(line) && SecretWriter().IsMatch(line)))
            .Select(file => Path.GetRelativePath(SourceRoot(), file).Replace('\\', '/'))
            .ToList();

        offenders.ShouldBeEmpty("add, replace and delete a connection's secrets through ConnectionCredentialStore inside the connection create, update or delete; no other code writes or deletes a secret");
    }

    [GeneratedRegex(@"\b(Set|Delete)SecretManagerCommand\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex SecretWriter();

    private static bool IsInDefiningProject(string file) =>
        Path.GetRelativePath(SourceRoot(), file).Replace('\\', '/').StartsWith(DefiningProject + "/", StringComparison.Ordinal);

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                           && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static bool IsCode(string line) => !Regex.IsMatch(line, @"^\s*//", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static string SourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Fdw.Services.Connections.Endpoints", "Endpoints", "Connections", StoreFile)))
                return Path.Combine(directory.FullName, "src");

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the public/src folder above the test output directory.");
    }
}
