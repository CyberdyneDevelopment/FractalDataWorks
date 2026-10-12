using System.Text.RegularExpressions;

namespace Fdw.Services.Data.Abstractions.Tests;

/// <summary>
/// The names of the connections a host declares (platform, server, auth) are declared by the host, in its
/// <c>configurationSchema.json</c>, and nowhere in code. There is no declaration site in source and no default: every
/// reader takes the name from <c>PlatformConnection</c>, <c>ServerConnection</c> or <c>AuthConnection</c> (or from the
/// request's configuration gateway), and those hold only what the host declared.
/// </summary>
public partial class PlatformConnectionLiteralTests
{
    private static readonly string[] Literals = ["\"PlatformConfiguration\"", "\"ServerConfiguration\"", "\"AuthDb\"", "\"AegisConfiguration\""];

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Convention")]
    public void NoCodeLiteralAnywhere()
    {
        FilesWithCodeLiteral().ShouldBeEmpty("read PlatformConnection/ServerConnection/AuthConnection .Name (or the request's configuration gateway ConnectionName) instead of a literal; the host declares the names in configurationSchema.json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Convention")]
    public void EveryDefaultDataStoreNameReadsAHolderWhenAsked()
    {
        var offenders = SourceFiles()
            .Where(file => !file.EndsWith("ServiceTypeBase.cs", StringComparison.Ordinal))
            .SelectMany(file => File.ReadLines(file).Where(line => IsCode(line) && line.Contains("defaultDataStoreName:", StringComparison.Ordinal) && !LazyHolderRead().IsMatch(line)).Select(line => $"{Path.GetFileName(file)}: {line.Trim()}"))
            .ToList();

        offenders.ShouldBeEmpty("a type option is built before the host reads its schema, so defaultDataStoreName must be () => <holder>.Name, read when asked, never a string captured at construction");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Convention")]
    public void NoHolderOrDomainCollectionHasAWritableName()
    {
        var offenders = SourceFiles()
            .Where(file => File.ReadLines(file).Any(line => IsCode(line) && WritableHolder().IsMatch(line)))
            .Select(file => Path.GetFileName(file))
            .ToList();

        offenders.ShouldBeEmpty("a connection name is declared by the host's schema; no code sets it");
    }

    [GeneratedRegex(@"defaultDataStoreName:\s*\(\)\s*=>\s*(Platform|Server|Auth)Connection\.Name\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex LazyHolderRead();

    [GeneratedRegex(@"\b(Platform|Server|Auth)Connection\.Name\s*=(?![=>])|set\s*=>\s*\w+\.ConfigurationConnection\s*=", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex WritableHolder();

    private static IEnumerable<string> FilesWithCodeLiteral()
    {
        var src = SourceRoot();
        return SourceFiles()
            .Where(file => File.ReadLines(file).Any(line => IsCode(line) && Literals.Any(literal => line.Contains(literal, StringComparison.Ordinal))))
            .Select(file => Path.GetRelativePath(src, file).Replace('\\', '/'));
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"));

    private static bool IsCode(string line) => !Regex.IsMatch(line, @"^\s*//");

    private static string SourceRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Fdw.Services.Data.Abstractions", "PlatformConnection.cs")))
            {
                return Path.Combine(directory.FullName, "src");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the public/src folder above the test output directory.");
    }
}
