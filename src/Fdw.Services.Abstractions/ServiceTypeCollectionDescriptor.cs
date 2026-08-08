using System;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.ServiceTypes;

/// <summary>
/// Default <see cref="IServiceTypeCollection"/> record produced by the opt-in
/// <c>Fdw.Services.Registration.SourceGenerators</c> generator for each discovered
/// <c>[ServiceTypeCollection]</c> class. Holds the category name, the <see cref="Type"/> of the
/// generated collection, and its three-phase entry points as bare method-group delegates.
/// </summary>
/// <param name="ServiceCategory">Category name used as the registry key.</param>
/// <param name="CollectionType">CLR type of the generated ServiceTypeCollection.</param>
/// <param name="Configure">The collection's static Configure method group.</param>
/// <param name="Register">The collection's static Register method group.</param>
/// <param name="Initialize">The collection's static Initialize method group.</param>
public sealed record ServiceTypeCollectionDescriptor(
    string ServiceCategory,
    Type CollectionType,
    Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> Configure,
    Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> Register,
    Func<IHost, ILoggerFactory?, IGenericResult<IHost>> Initialize) : IServiceTypeCollection;
