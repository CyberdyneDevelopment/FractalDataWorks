using System;
using Fdw.Configuration;

namespace Fdw.Services.Resiliency.Polly.Tests.TestDoubles;

/// <summary>
/// A non-Polly <see cref="IGenericConfiguration"/> implementation used to exercise the
/// "wrong configuration type" branch of <see cref="PollyRetryResiliencyType.Execute"/>.
/// </summary>
internal sealed class FakeGenericConfiguration : IGenericConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "FakeConfig";

    public string SectionName => "Fake";

    public string ServiceType => "Fake";

    public string? ServiceOptionType => null;
}
