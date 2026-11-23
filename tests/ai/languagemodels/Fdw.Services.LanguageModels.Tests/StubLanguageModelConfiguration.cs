using Fdw.Services.LanguageModels.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>A language model row stated in a test: what the model carries is plain data, as it is on a configuration row.</summary>
internal sealed class StubLanguageModelConfiguration : ILanguageModelImplementationConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "Stub";

    public string Domain { get; set; } = "LanguageModel";

    public string Implementation { get; set; } = "Stub";

    public string? ModelId { get; set; }

    public string? SecretManagerName { get; set; }

    public string? ApiKeySecretName { get; set; }

    public List<string> Capabilities { get; } = [];

    public List<string> ReasoningEfforts { get; } = [];

    public IReadOnlyCollection<string> CapabilityNames => Capabilities;

    public IReadOnlyCollection<string> ReasoningEffortNames => ReasoningEfforts;

    public static StubLanguageModelConfiguration Carrying(string[] capabilities, string[] efforts)
    {
        var configuration = new StubLanguageModelConfiguration
        {
            ModelId = "test-model",
            SecretManagerName = "TestSecrets",
            ApiKeySecretName = "TestKey",
        };
        configuration.Capabilities.AddRange(capabilities);
        configuration.ReasoningEfforts.AddRange(efforts);
        return configuration;
    }
}
