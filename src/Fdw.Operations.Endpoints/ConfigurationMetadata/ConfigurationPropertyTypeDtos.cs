using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// TypeCollection for configuration property type DTOs.
/// </summary>
[TypeCollection(typeof(ConfigurationPropertyTypeDtoBase), typeof(IConfigurationPropertyTypeDto), typeof(ConfigurationPropertyTypeDtos), RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public abstract partial class ConfigurationPropertyTypeDtos : TypeCollectionBase<ConfigurationPropertyTypeDtoBase, IConfigurationPropertyTypeDto> { }
