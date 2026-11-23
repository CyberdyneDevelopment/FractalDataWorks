using System.Text.RegularExpressions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>Guard rail 4: the contract carries no vendor word. The vendor's types stay in the reference-servicetypes implementation.</summary>
public sealed class NoVendorWordTests
{
    private static readonly Regex VendorWord = new(@"\b(anthropic|claude|openai|gpt|sonnet|opus|haiku|fable|mythos)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static string LanguageModelSources()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "ai", "languagemodels")))
            directory = directory.Parent;

        return directory is null
            ? throw new InvalidOperationException("The language model sources were not found above the test output folder.")
            : Path.Combine(directory.FullName, "src", "ai", "languagemodels");
    }

    [Fact]
    public void TheFdwLanguageModelProjectsCarryNoVendorWord()
    {
        var files = Directory.EnumerateFiles(LanguageModelSources(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Split(Path.DirectorySeparatorChar).Any(segment => segment is "obj" or "bin")).ToList();

        files.ShouldNotBeEmpty();
        var offenders = files.Where(path => VendorWord.IsMatch(File.ReadAllText(path))).ToList();
        offenders.ShouldBeEmpty();
    }
}
