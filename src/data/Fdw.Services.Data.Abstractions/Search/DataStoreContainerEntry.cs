using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>A container of a data store with the names of the path and the store it sits under (FDW-2311).</summary>
/// <param name="Id">The logical id of the container.</param>
/// <param name="DataStoreName">The name of the store.</param>
/// <param name="PathName">The name of the path.</param>
/// <param name="ContainerName">The name of the container.</param>
/// <param name="ContainerType">The kind of container.</param>
/// <param name="ClassificationId">The data classification of the container.</param>
public sealed record DataStoreContainerEntry(Guid Id, string DataStoreName, string PathName, string ContainerName, string ContainerType, int ClassificationId);
