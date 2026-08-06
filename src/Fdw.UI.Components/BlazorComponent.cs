using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Fdw.UI.Web.Abstractions;

namespace Fdw.UI.Components;

/// <summary>
/// CRTP base for Blazor components.
/// Inherits from WebComponentBase and adds Blazor rendering via RenderTreeBuilder.
/// </summary>
/// <typeparam name="TSelf">The derived Blazor component type (CRTP)</typeparam>
/// <typeparam name="TModel">The model type being rendered</typeparam>
public abstract class BlazorComponent<TSelf, TModel> : WebComponentBase<TSelf, TModel>, IComponent
    where TSelf : BlazorComponent<TSelf, TModel>
{
    private RenderHandle _renderHandle;
    private bool _initialized;

    /// <summary>
    /// Injected IJSRuntime for JavaScript interop.
    /// </summary>
    [Inject] protected IJSRuntime? BlazorJsRuntime { get; set; }

    /// <summary>
    /// Cascading EditContext for validation.
    /// </summary>
    [CascadingParameter] protected EditContext? EditContext { get; set; }

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

        await OnParametersSet();

        StateHasChanged();
    }

    /// <summary>
    /// Called when component is initialized.
    /// </summary>
    protected virtual Task OnInitialized()
    {
        // Set up JavaScript interop adapter
        if (BlazorJsRuntime != null)
        {
            JSInterop = new BlazorJavaScriptInterop(BlazorJsRuntime);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when parameters are set.
    /// </summary>
    protected virtual Task OnParametersSet()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggers a re-render of this component.
    /// </summary>
    protected void StateHasChanged()
    {
        _renderHandle.Render(BuildRenderTree);
    }

    /// <summary>
    /// Determines whether the component should render.
    /// </summary>
    protected virtual bool ShouldRender()
    {
        return true;
    }

    /// <summary>
    /// Builds the render tree for this component.
    /// Override to provide custom rendering logic.
    /// </summary>
    protected abstract void BuildRenderTree(RenderTreeBuilder builder);

    /// <summary>
    /// Gets CSS class for this component based on metadata.
    /// </summary>
    protected virtual string GetCssClass()
    {
        return $"component {typeof(TSelf).Name.ToLowerInvariant()}";
    }
}
