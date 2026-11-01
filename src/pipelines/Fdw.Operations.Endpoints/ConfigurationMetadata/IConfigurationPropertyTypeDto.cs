using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Interface for configuration property type DTOs for UI rendering.
/// </summary>
public interface IConfigurationPropertyTypeDto : ITypeOption<int, ConfigurationPropertyTypeDtoBase> { }
