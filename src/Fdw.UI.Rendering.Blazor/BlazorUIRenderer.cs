using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.UI.Abstractions.Components;
using Fdw.UI.Abstractions.Pages;
using Fdw.UI.Abstractions.Rendering;
using Fdw.UI.Rendering.Blazor.Components;
using Fdw.UI.Rendering.Blazor.Messages;
using Fdw.UI.Rendering.Blazor.Results;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.UI.Rendering.Blazor;

/// <summary>
/// Blazor implementation of <see cref="IUIRenderer"/>: maps headless component models to
/// retained-mode <see cref="RenderFragment"/>s composed into a <see cref="BlazorRenderContext"/>.
/// </summary>
/// <remarks>
/// Where Spectre renders imperatively and blocks on console prompts, this renderer composes
/// fragments and completes the returned <see cref="Task"/> when the user acts on the bound
/// components. The behavioral contract (action rules, validation flow, result shapes) matches
/// <c>SpectreUIRenderer</c> so both backends are interchangeable behind the seam.
/// </remarks>
public sealed class BlazorUIRenderer : IUIRenderer
{
    private readonly ILogger<BlazorUIRenderer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorUIRenderer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public BlazorUIRenderer(ILogger<BlazorUIRenderer> logger)
    {
        _logger = logger ?? NullLogger<BlazorUIRenderer>.Instance;
    }

    /// <inheritdoc />
    public bool SupportsInteractiveMode => true;

    /// <inheritdoc />
    public bool SupportsAnsiColors => false;

    /// <inheritdoc />
    public bool SupportsFocusManagement => true;

    /// <inheritdoc />
    public bool SupportsHotReload => true;

    /// <inheritdoc />
    public Task<RenderResult> Render(
        IComponentModel model,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not BlazorRenderContext blazorContext)
        {
            return Task.FromResult(RenderResult.Failure(BlazorUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            BlazorRenderingMessages.RenderingComponent(_logger, model.Id, model.GetType().Name);
            blazorContext.AddFragment(BuildComponentFragment(model, blazorContext));
            return Task.FromResult(RenderResult.Ok());
        }
        catch (Exception ex)
        {
            BlazorRenderingMessages.RenderError(_logger, model.Id, ex.Message);
            return Task.FromResult(RenderResult.Failure($"Render failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<PromptResult<T>> Prompt<T>(
        IInputComponentModel<T> model,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not BlazorRenderContext blazorContext)
        {
            return Task.FromResult(PromptResult<T>.Failure(BlazorUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        var completion = new TaskCompletionSource<PromptResult<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        cancellationToken.Register(() =>
        {
            if (completion.TrySetResult(PromptResult<T>.Cancel()))
            {
                BlazorRenderingMessages.PromptCancelled(_logger, model.Id);
            }
        });

        blazorContext.SetActiveFragment(builder =>
        {
            builder.OpenComponent<FdwPrompt>(0);
            builder.AddComponentParameter(1, nameof(FdwPrompt.Model), model);
            builder.AddComponentParameter(2, nameof(FdwPrompt.Mode), blazorContext.Mode);
            builder.AddComponentParameter(3, nameof(FdwPrompt.OnSubmit), (Func<ValidationResult?>)(() =>
            {
                var validation = model.Validate();
                if (!validation.IsValid)
                {
                    BlazorRenderingMessages.ValidationFailed(_logger, model.Id,
                        validation.Messages.Count > 0 ? validation.Messages[0].Message : "invalid");
                    return validation;
                }

                BlazorRenderingMessages.PromptCompleted(_logger, model.Id);
                completion.TrySetResult(PromptResult<T>.Ok(model.ValueAsObject is T value ? value : default!));
                return null;
            }));
            builder.AddComponentParameter(4, nameof(FdwPrompt.OnCancel), (Action)(() =>
            {
                BlazorRenderingMessages.PromptCancelled(_logger, model.Id);
                completion.TrySetResult(PromptResult<T>.Cancel());
            }));
            builder.CloseComponent();
        });

        return completion.Task;
    }

    /// <inheritdoc />
    public Task<ListPageResult> RenderListPage(
        IListPageModel page,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not BlazorRenderContext blazorContext)
        {
            return Task.FromResult(ListPageResult.Failure(BlazorUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            var completion = new TaskCompletionSource<ListPageResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => completion.TrySetResult(ListPageResult.Cancel()));

            blazorContext.SetActiveFragment(builder =>
            {
                builder.OpenComponent<FdwListPage>(0);
                builder.AddComponentParameter(1, nameof(FdwListPage.Page), page);
                builder.AddComponentParameter(2, nameof(FdwListPage.OnAction),
                    (Action<Fdw.UI.Abstractions.Pages.IPageAction, object?, int?>)((action, rowId, rowIndex) =>
                        completion.TrySetResult(ListPageResult.Selected(action, rowId, rowIndex))));
                builder.AddComponentParameter(3, nameof(FdwListPage.OnBack), (Action)(() =>
                    completion.TrySetResult(ListPageResult.Exit())));
                builder.CloseComponent();
            });

            BlazorRenderingMessages.PageRendered(_logger, page.Title, page.Rows.Count);
            return completion.Task;
        }
        catch (Exception ex)
        {
            BlazorRenderingMessages.RenderError(_logger, page.Id, ex.Message);
            return Task.FromResult(ListPageResult.Failure($"List page render failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public Task<PageResult> RenderPage(
        IPageModel page,
        IRenderContext context,
        CancellationToken cancellationToken = default)
    {
        if (context is not BlazorRenderContext blazorContext)
        {
            return Task.FromResult(PageResult.Failure(BlazorUIResultCodes.ByName("InvalidRenderContext").MessageTemplate));
        }

        try
        {
            var completion = new TaskCompletionSource<PageResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            cancellationToken.Register(() => completion.TrySetResult(PageResult.Cancel()));

            blazorContext.SetActiveFragment(builder =>
            {
                builder.OpenComponent<FdwPage>(0);
                builder.AddComponentParameter(1, nameof(FdwPage.Page), page);
                builder.AddComponentParameter(2, nameof(FdwPage.Mode), blazorContext.Mode);
                builder.AddComponentParameter(3, nameof(FdwPage.OnSave), (Func<ValidationResult?>)(() =>
                {
                    var validation = page.Validate();
                    if (!validation.IsValid)
                    {
                        foreach (var error in validation.Messages.Where(m => string.Equals(m.Severity.Name, "Error", StringComparison.Ordinal)))
                        {
                            BlazorRenderingMessages.ValidationFailed(_logger, page.Id, error.Message);
                        }
                        completion.TrySetResult(PageResult.ValidationFailed(validation));
                        return validation;
                    }

                    BlazorRenderingMessages.ConfigurationSaved(_logger, page.Title);
                    completion.TrySetResult(PageResult.Save(page));
                    return null;
                }));
                builder.AddComponentParameter(4, nameof(FdwPage.OnCancel), (Action)(() =>
                    completion.TrySetResult(PageResult.Cancel())));
                builder.AddComponentParameter(5, nameof(FdwPage.OnDelete), (Action)(() =>
                {
                    BlazorRenderingMessages.DeletionRequested(_logger, page.Title);
                    completion.TrySetResult(PageResult.Delete());
                }));
                builder.CloseComponent();
            });

            BlazorRenderingMessages.PageRendered(_logger, page.Title, page.Sections.Count);
            return completion.Task;
        }
        catch (Exception ex)
        {
            BlazorRenderingMessages.RenderError(_logger, page.Id, ex.Message);
            return Task.FromResult(PageResult.Failure($"Page render failed: {ex.Message}"));
        }
    }

    private RenderFragment BuildComponentFragment(IComponentModel model, BlazorRenderContext context)
    {
        if (!IsSupported(model))
        {
            BlazorRenderingMessages.UnsupportedComponentType(_logger, model.GetType().Name, model.Id);
        }

        return builder =>
        {
            builder.OpenComponent<FdwComponent>(0);
            builder.AddComponentParameter(1, nameof(FdwComponent.Model), model);
            builder.AddComponentParameter(2, nameof(FdwComponent.Mode), context.Mode);
            builder.CloseComponent();
        };
    }

    private static bool IsSupported(IComponentModel model) =>
        Dispatch.BlazorComponentRendererExtensions.ResolveFor(model) is not null;
}
