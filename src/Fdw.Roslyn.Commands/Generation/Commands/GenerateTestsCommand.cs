using Fdw.Collections.Attributes;
using Fdw.Roslyn.Commands.Abstractions;
using static Fdw.Roslyn.Commands.Abstractions.RoslynCommands;

namespace Fdw.Roslyn.Commands.Generation.Commands;
/// <summary>
/// Command to generate unit tests for a class.
/// </summary>
[TypeOption(typeof(RoslynCommands), "GenerateTests")]
public sealed class GenerateTestsCommand : RoslynCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateTestsCommand"/> class.
    /// </summary>
    public GenerateTestsCommand()
        : base("GenerateTests", RoslynCommandCategories.Generation, "Generate a test class skeleton for a given class — one test method per public member, using the project's chosen test framework (xUnit / NUnit / MSTest detected from the test project's references). Use to bootstrap test coverage; bodies are placeholders. Returns the test file path.")
    {
    }
    /// <summary>
    /// Gets or sets the source file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int Line { get; set; }
    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int Column { get; set; }
    /// <summary>
    /// Gets or sets the test framework (xunit, nunit, mstest).
    /// </summary>
    public string TestFramework { get; set; } = "xunit";
    /// <summary>
    /// Gets or sets the project name where the tests should be added.
    /// </summary>
    public string? TestProjectName { get; set; }
}
