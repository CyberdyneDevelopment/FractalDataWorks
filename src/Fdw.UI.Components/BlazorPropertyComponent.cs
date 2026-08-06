using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Fdw.UI.Abstractions;

namespace Fdw.UI.Components;

/// <summary>
/// CRTP base for Blazor property-level components.
/// </summary>
/// <typeparam name="TSelf">The derived property component type (CRTP)</typeparam>
/// <typeparam name="TProperty">The property value type</typeparam>
public abstract class BlazorPropertyComponent<TSelf, TProperty> : PropertyComponent<TSelf, TProperty>, IComponent
    where TSelf : BlazorPropertyComponent<TSelf, TProperty>
{
    private RenderHandle _renderHandle;
    private bool _initialized;

    /// <summary>
    /// Attaches this component to the render tree.
    /// </summary>
    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <summary>
    /// Sets parameters from parent component.
    /// </summary>
    async Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (!_initialized)
        {
            await OnInitialized();
            _initialized = true;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Called when component is initialized.
    /// </summary>
    protected virtual Task OnInitialized()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggers a re-render.
    /// </summary>
    protected void StateHasChanged()
    {
        _renderHandle.Render(BuildRenderTree);
    }

    /// <summary>
    /// Builds the render tree for this property component.
    /// </summary>
    protected abstract void BuildRenderTree(RenderTreeBuilder builder);
}
