using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>The notification of a Notify stage for tests.</summary>
internal sealed record FakeNotificationSpec(string RuleName, string RunPoint, string Message) : INotificationSpec;
