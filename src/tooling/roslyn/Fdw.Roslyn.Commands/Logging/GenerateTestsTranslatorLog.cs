using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Logging;

/// <summary>
/// MessageLogging for <see cref="Generation.Translators.GenerateTestsTranslator"/>.
/// </summary>
[MessageLoggingTypeCode("RCMD")]
public static partial class GenerateTestsTranslatorLog
{
    /// <summary>Trace: test generation starting.</summary>
    [MessageLogging(EventId = 11082, Level = LogLevel.Trace,
        Message = "GenerateTestsTranslator generating tests for '{filePath}' {line}:{column} (framework={testFramework})")]
    public static partial IGenericMessage Generating(ILogger logger, string filePath, int line, int column, string testFramework);

    /// <summary>Error: the target document was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.DocumentNotFound</c> (31001).</remarks>
    [MessageLogging(EventId = 31001, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: document not found at '{filePath}'")]
    public static partial IGenericMessage DocumentNotFound(ILogger logger, string filePath);

    /// <summary>Error: the document id resolved but the document could not be loaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadDocument</c> (91009).</remarks>
    [MessageLogging(EventId = 91009, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: failed to load document at '{filePath}'")]
    public static partial IGenericMessage FailedToLoadDocument(ILogger logger, string filePath);

    /// <summary>Error: the semantic model or syntax root could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToAnalyzeDocument</c> (91003).</remarks>
    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: failed to analyze document '{filePath}'")]
    public static partial IGenericMessage FailedToAnalyzeDocument(ILogger logger, string filePath);

    /// <summary>Error: no type declaration was found at the requested position.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoTypeDeclarationFoundAtPosition</c> (31013).</remarks>
    [MessageLogging(EventId = 31013, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: no type declaration found at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage NoTypeDeclarationFoundAtPosition(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the type declaration resolved but its symbol could not be obtained.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToGetTypeSymbol</c> (91008).</remarks>
    [MessageLogging(EventId = 91008, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: failed to get type symbol at {line}:{column} in '{filePath}'")]
    public static partial IGenericMessage FailedToGetTypeSymbol(ILogger logger, string filePath, int line, int column);

    /// <summary>Error: the target type has no public methods to generate tests for.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoPublicMethodsFoundToGenerateTests</c> (31007).</remarks>
    [MessageLogging(EventId = 31007, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: type '{typeName}' has no public methods to generate tests for")]
    public static partial IGenericMessage NoPublicMethodsFoundToGenerateTests(ILogger logger, string typeName);

    /// <summary>Error: the named test project was not found in the solution.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.TestProjectNotFound</c> (31018).</remarks>
    [MessageLogging(EventId = 31018, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: test project '{testProjectName}' not found")]
    public static partial IGenericMessage TestProjectNotFound(ILogger logger, string testProjectName);

    /// <summary>Error: no test project was supplied and the solution has no projects.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.NoProjectsFoundInSolution</c> (31006).</remarks>
    [MessageLogging(EventId = 31006, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: no projects found in solution")]
    public static partial IGenericMessage NoProjectsFoundInSolution(ILogger logger);

    /// <summary>Error: a document with the target file name exists but could not be reloaded.</summary>
    /// <remarks>Mirrors <c>RoslynResultCodes.FailedToLoadExistingDocument</c> (91010).</remarks>
    [MessageLogging(EventId = 91010, Level = LogLevel.Error,
        Message = "GenerateTestsTranslator: failed to load existing document '{fileName}'")]
    public static partial IGenericMessage FailedToLoadExistingDocument(ILogger logger, string fileName);

    /// <summary>Information: tests were generated.</summary>
    [MessageLogging(EventId = 11083, Level = LogLevel.Information,
        Message = "GenerateTestsTranslator generated {testCount} unit test(s) for '{typeName}'")]
    public static partial IGenericMessage Generated(ILogger logger, string typeName, int testCount);
}
