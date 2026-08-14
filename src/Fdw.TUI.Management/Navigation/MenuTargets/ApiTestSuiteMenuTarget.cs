using Fdw.Collections.Attributes;
using Fdw.TUI.Management.Screens;

namespace Fdw.TUI.Management.Navigation;

/// <summary>
/// Main-menu entry for the generated API test suite.
/// </summary>
/// <remarks>
/// Why no connection is required: the suite talks to the API over its own base URL and signs
/// in for itself. Gating it behind this tool's instance connection would stop an operator
/// using the one thing that says whether the API is answering at all.
/// </remarks>
[TypeOption(typeof(MenuTargets), "apitests", RestrictToCurrentCompilation = true)]
public sealed class ApiTestSuiteMenuTarget : MenuTargetBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiTestSuiteMenuTarget"/> class.
    /// </summary>
    public ApiTestSuiteMenuTarget() : base(
        id: 6,
        name: "apitests",
        label: "API Test Suite",
        group: "Main",
        order: 4)
    {
    }

    /// <inheritdoc />
    public override NavigationResult Navigate(IScreenFactory screenFactory)
    {
        return NavigationResult.Push(screenFactory.Create<ApiTestSuiteScreen>());
    }
}
