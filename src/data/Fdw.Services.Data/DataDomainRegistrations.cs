using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Transformations;
using Microsoft.Extensions.DependencyInjection;

namespace Fdw.Services.Data;

/// <summary>
/// Attaches each implementation provider of the data domains to its domain provider (FDW-3966), after the host is built. It returns the first failure instead of dropping it,
/// so a provider that cannot be attached stops the host.
/// </summary>
internal static class DataDomainRegistrations
{
    /// <summary>Attaches every implementation provider of the data store, data set, category and transformation domains.</summary>
    /// <param name="services">The built host's services.</param>
    /// <returns>Success, or the first failed attachment.</returns>
    public static IGenericResult Attach(IServiceProvider services)
    {
        var steps = new List<Func<IGenericResult>>();
        steps.AddRange(DataStoreKinds(services));
        steps.AddRange(DataSets(services));
        steps.AddRange(Categories(services));
        steps.AddRange(Transformations(services));
        return FirstFailure(steps);
    }

    /// <summary>
    /// Attaches the implementation provider to the data store domain provider for each data store kind (FDW-3863), so a data store composed from its rows finds its kind. Called by the
    /// DataStore domain's own Initialize before it loads the stores: the category order puts "DataStore" before "DataStoreService", whose Initialize runs <see cref="Attach"/>, so the
    /// kinds were not yet attached when the stores were loaded. Attaching a kind again replaces the provider with the same one, so <see cref="Attach"/> repeating it is harmless.
    /// </summary>
    /// <param name="services">The built host's services.</param>
    /// <returns>Success, or the first failed attachment.</returns>
    public static IGenericResult AttachDataStoreKinds(IServiceProvider services)
    {
        return FirstFailure(DataStoreKinds(services));
    }

    /// <summary>Runs the steps in order and stops at the first that fails.</summary>
    /// <param name="steps">The attachments, each answering its own result.</param>
    /// <returns>Success when every step succeeded; otherwise the first failed result, with the steps after it not run.</returns>
    internal static IGenericResult FirstFailure(IEnumerable<Func<IGenericResult>> steps)
    {
        foreach (var step in steps)
        {
            var result = step();
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult.Success();
    }

    private static IEnumerable<Func<IGenericResult>> DataStoreKinds(IServiceProvider sp)
    {
        var domain = sp.GetRequiredService<DataStoreConfigurationProvider>();
        var implementation = sp.GetRequiredService<IImplementationConfigurationProvider<IDataStoreImplementationConfiguration>>();
        return DataStoreTypes.All().Select<IDataStoreType, Func<IGenericResult>>(kind => () => domain.Register(kind.Name, implementation));
    }

    private static IEnumerable<Func<IGenericResult>> DataSets(IServiceProvider sp)
    {
        var domain = sp.GetRequiredService<DataSetConfigurationProvider>();
        yield return () => domain.Register(SimpleDataSetConfiguration.ImplementationName, sp.GetRequiredService<SimpleDataSetConfigurationProvider>());
        yield return () => domain.Register(CompoundDataSetConfiguration.ImplementationName, sp.GetRequiredService<CompoundDataSetConfigurationProvider>());
        yield return () => domain.Register(FederatedDataSetConfiguration.ImplementationName, sp.GetRequiredService<FederatedDataSetConfigurationProvider>());
        yield return () => domain.Register(VariationDataSetConfiguration.ImplementationName, sp.GetRequiredService<VariationDataSetConfigurationProvider>());
    }

    private static IEnumerable<Func<IGenericResult>> Categories(IServiceProvider sp)
    {
        var domain = sp.GetRequiredService<CategoryConfigurationProvider>();
        yield return () => domain.Register(DataSetCategoryConfiguration.ImplementationName, sp.GetRequiredService<DataSetCategoryConfigurationProvider>());
        yield return () => domain.Register(CustomCategoryConfiguration.ImplementationName, sp.GetRequiredService<CustomCategoryConfigurationProvider>());
        yield return () => domain.Register(GlossaryCategoryConfiguration.ImplementationName, sp.GetRequiredService<GlossaryCategoryConfigurationProvider>());
    }

    private static IEnumerable<Func<IGenericResult>> Transformations(IServiceProvider sp)
    {
        var domain = sp.GetRequiredService<TransformationConfigurationProvider>();
        yield return () => domain.Register(FieldTransformationConfiguration.ImplementationName, sp.GetRequiredService<FieldTransformationConfigurationProvider>());
        yield return () => domain.Register(MapTransformationConfiguration.ImplementationName, sp.GetRequiredService<MapTransformationConfigurationProvider>());
        yield return () => domain.Register(FilterTransformationConfiguration.ImplementationName, sp.GetRequiredService<FilterTransformationConfigurationProvider>());
        yield return () => domain.Register(CalculateTransformationConfiguration.ImplementationName, sp.GetRequiredService<CalculateTransformationConfigurationProvider>());
        yield return () => domain.Register(BuiltTransformationConfiguration.ImplementationName, sp.GetRequiredService<BuiltTransformationConfigurationProvider>());
        yield return () => domain.Register(AggregateTransformationConfiguration.ImplementationName, sp.GetRequiredService<AggregateTransformationConfigurationProvider>());
        yield return () => domain.Register(LookupTransformationConfiguration.ImplementationName, sp.GetRequiredService<LookupTransformationConfigurationProvider>());
    }
}
