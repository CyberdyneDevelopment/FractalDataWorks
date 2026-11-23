using System.Diagnostics.CodeAnalysis;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Execution;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Tests.Execution;

/// <summary>
/// The real <see cref="PipelineRunOrigins"/> over a host environment and a slot role that answer what a test states (FDW-6519), so a test that builds a runner or a starter
/// gets origins whose answers are real, never a stand-in that says nothing.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TestRunOrigins
{
    /// <summary>Makes the origins a host named <paramref name="application"/> on slot <paramref name="slot"/> (null off a slot) gives.</summary>
    /// <param name="application">The host's application name.</param>
    /// <param name="slot">The slot's own name, or null for a host off a slot.</param>
    /// <returns>The origins.</returns>
    internal static PipelineRunOrigins Create(string application = "Reference.Etl.Server", string? slot = "a")
        => new(new Environment(application), new Slot(slot), NullLogger<PipelineRunOrigins>.Instance);

    private sealed class Environment(string applicationName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = applicationName;

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class Slot(string? name) : ISlotRole
    {
        public string? SlotName => name;

        public IGenericResult<bool> BackgroundWorkMayRun() => GenericResult<bool>.Success(true);
    }
}
