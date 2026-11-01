using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>A check of a Validate stage for tests.</summary>
internal sealed record FakeCheckSpec(string Name, string Expression) : ITransformCheckSpec;
