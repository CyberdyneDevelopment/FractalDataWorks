using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.UI.Canvas.Blazor.Tests.Fakes;

/// <summary>
/// Simple mutable canvas port for testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeCanvasPort : ICanvasPort
{
    /// <inheritdoc />
    public string Id { get; set; } = "fake-port";

    /// <inheritdoc />
    public string Name { get; set; } = "Fake Port";

    /// <inheritdoc />
    public IPortDirection Direction { get; set; } = PortDirections.ByName("In");

    /// <inheritdoc />
    public string? DataType { get; set; }
}
