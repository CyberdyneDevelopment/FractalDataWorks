
namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>
/// A tool offered to a model. It states who runs it: the host (which then answers the call with a tool result) or the provider
/// (which runs it itself and reports its activity). The two are made by the two factory methods, so a definition is never half of each.
/// </summary>
public sealed class LanguageModelToolDefinition
{
    private LanguageModelToolDefinition(string name, string description, string? inputSchema, IToolExecutor executor, IHostedToolKind? hostedKind)
    {
        Name = name;
        Description = description;
        InputSchema = inputSchema;
        Executor = executor;
        HostedKind = hostedKind;
    }

    /// <summary>Gets the name the model calls the tool by.</summary>
    public string Name { get; }

    /// <summary>Gets what the tool is for, in words the model reads.</summary>
    public string Description { get; }

    /// <summary>Gets the JSON schema of the tool's input, or <see langword="null"/> for a tool the provider runs.</summary>
    public string? InputSchema { get; }

    /// <summary>Gets who runs the tool.</summary>
    public IToolExecutor Executor { get; }

    /// <summary>Gets the kind of hosted tool this is, or <see langword="null"/> for a tool the host runs.</summary>
    public IHostedToolKind? HostedKind { get; }

    /// <summary>Makes the definition of a tool the host runs.</summary>
    /// <param name="name">The name the model calls the tool by.</param>
    /// <param name="description">What the tool is for, in words the model reads.</param>
    /// <param name="inputSchema">The JSON schema of the tool's input.</param>
    /// <returns>The definition.</returns>
    public static LanguageModelToolDefinition RunByHost(string name, string description, string inputSchema)
        => new LanguageModelToolDefinition(
            name,
            description,
            inputSchema,
            ToolExecutors.ByName("Host"),
            null);

    /// <summary>Makes the definition of a tool the provider runs itself.</summary>
    /// <param name="hostedKind">The kind of hosted tool.</param>
    /// <returns>The definition. Its name and description are the kind's.</returns>
    public static LanguageModelToolDefinition RunByProvider(IHostedToolKind hostedKind)
        => new LanguageModelToolDefinition(
            hostedKind.Name,
            hostedKind.Name,
            null,
            ToolExecutors.ByName("Provider"),
            hostedKind);
}
