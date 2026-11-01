using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Clients.Models;

/// <summary>TypeCollection for configuration property types.</summary>
[TypeCollection(typeof(ConfigurationPropertyTypeBasePayload), typeof(IConfigurationPropertyType), typeof(ConfigurationPropertyTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class ConfigurationPropertyTypes : TypeCollectionBase<ConfigurationPropertyTypeBasePayload, IConfigurationPropertyType> { }
