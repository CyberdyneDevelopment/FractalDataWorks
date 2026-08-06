using Microsoft.Extensions.DependencyInjection;
using Fdw.UI.Services.Formula;
using Fdw.UI.Services.Pipeline;
using Fdw.UI.Services.UndoRedo;

namespace Fdw.UI.Services.Extensions;

/// <summary>
/// Extension methods for registering UI services.
/// </summary>
public static class UIServiceCollectionExtensions
{
    /// <summary>
    /// Adds Fdw UI services.
    /// </summary>
    public static IServiceCollection AddFrameworkUIServices(this IServiceCollection services)
    {
        services.AddScoped<IUndoRedoManager, UndoRedoManager>();
        services.AddSingleton<IPipelineValidator, PipelineValidator>();
        services.AddSingleton<IFormulaTokenizer, FormulaTokenizer>();
        return services;
    }
}
