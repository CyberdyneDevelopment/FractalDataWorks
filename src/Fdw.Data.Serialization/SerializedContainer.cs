using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;

namespace Fdw.Data.Serialization;

/// <summary>
/// A simplified container implementation used for JSON serialization/deserialization.
/// This class captures the essential data of a container without the complexity of specific path types.
/// </summary>
/// <typeparam name="TProperties">The type of container-specific properties.</typeparam>
public sealed class SerializedContainer<TProperties> : IStorageContainer
    where TProperties : IContainerProperties
{
    /// <summary>
    /// Gets or initializes the container name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the container type name (e.g., "Table", "View", "Endpoint").
    /// </summary>
    public required string ContainerTypeName { get; init; }

    /// <summary>
    /// Gets or initializes the format type name (e.g., "Tabular", "Json", "Xml").
    /// </summary>
    public required string FormatTypeName { get; init; }

    /// <summary>
    /// Gets or initializes the path type name (e.g., "DatabasePath", "HttpPath", "FilePath").
    /// </summary>
    public required string PathTypeName { get; init; }

    /// <summary>
    /// Gets or initializes the full path string.
    /// </summary>
    public required string PathValue { get; init; }

    /// <summary>
    /// Gets or initializes the container schema.
    /// </summary>
    public required IContainerSchema Schema { get; init; }

    /// <summary>
    /// Gets or initializes the supported operations.
    /// </summary>
    public required string[] SupportedOperations { get; init; }

    /// <summary>
    /// Gets or initializes the metadata.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Metadata { get; init; }

    /// <summary>
    /// Gets or initializes container-specific properties.
    /// </summary>
    public TProperties? Properties { get; init; }

    // IStorageContainer implementation - these are placeholders that throw since this is a serialization container
    IContainerType IStorageContainer.ContainerType => throw new NotSupportedException("SerializedContainer does not have a resolved ContainerType. Use ContainerTypeName instead.");
    IFormatType IStorageContainer.Format => throw new NotSupportedException("SerializedContainer does not have a resolved FormatType. Use FormatTypeName instead.");
    IPath IStorageContainer.Path => throw new NotSupportedException("SerializedContainer does not have a resolved Path. Use PathTypeName and PathValue instead.");
}

/// <summary>
/// Non-generic serialized container for containers without additional properties.
/// </summary>
public sealed class SerializedContainer : IStorageContainer
{
    /// <summary>
    /// Gets or initializes the container name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initializes the container type name (e.g., "Table", "View", "Endpoint").
    /// </summary>
    public required string ContainerTypeName { get; init; }

    /// <summary>
    /// Gets or initializes the format type name (e.g., "Tabular", "Json", "Xml").
    /// </summary>
    public required string FormatTypeName { get; init; }

    /// <summary>
    /// Gets or initializes the path type name (e.g., "DatabasePath", "HttpPath", "FilePath").
    /// </summary>
    public required string PathTypeName { get; init; }

    /// <summary>
    /// Gets or initializes the full path string.
    /// </summary>
    public required string PathValue { get; init; }

    /// <summary>
    /// Gets or initializes the container schema.
    /// </summary>
    public required IContainerSchema Schema { get; init; }

    /// <summary>
    /// Gets or initializes the supported operations.
    /// </summary>
    public required string[] SupportedOperations { get; init; }

    /// <summary>
    /// Gets or initializes the metadata.
    /// </summary>
    public required IReadOnlyDictionary<string, object> Metadata { get; init; }

    // IStorageContainer implementation - these are placeholders that throw since this is a serialization container
    IContainerType IStorageContainer.ContainerType => throw new NotSupportedException("SerializedContainer does not have a resolved ContainerType. Use ContainerTypeName instead.");
    IFormatType IStorageContainer.Format => throw new NotSupportedException("SerializedContainer does not have a resolved FormatType. Use FormatTypeName instead.");
    IPath IStorageContainer.Path => throw new NotSupportedException("SerializedContainer does not have a resolved Path. Use PathTypeName and PathValue instead.");

    /// <summary>
    /// Creates a generic serialized container from this container with the specified properties.
    /// </summary>
    /// <typeparam name="TProperties">The type of container-specific properties.</typeparam>
    /// <param name="properties">The container-specific properties.</param>
    /// <returns>A new generic serialized container.</returns>
    public SerializedContainer<TProperties> WithProperties<TProperties>(TProperties properties)
        where TProperties : IContainerProperties
    {
        return new SerializedContainer<TProperties>
        {
            Name = Name,
            ContainerTypeName = ContainerTypeName,
            FormatTypeName = FormatTypeName,
            PathTypeName = PathTypeName,
            PathValue = PathValue,
            Schema = Schema,
            SupportedOperations = SupportedOperations,
            Metadata = Metadata,
            Properties = properties
        };
    }
}
