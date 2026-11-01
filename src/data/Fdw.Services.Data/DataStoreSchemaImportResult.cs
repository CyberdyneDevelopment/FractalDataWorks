namespace Fdw.Services.Data;

/// <summary>What a schema discovery found, and what it added to the data store.</summary>
/// <param name="ContainersDiscovered">The containers the connection's schema discovery returned.</param>
/// <param name="FieldsDiscovered">The fields across those containers.</param>
/// <param name="PathsAdded">The paths added to the store.</param>
/// <param name="ContainersAdded">The containers added to the store.</param>
/// <param name="FieldsAdded">The fields added, in new containers and, on a refresh, in existing ones.</param>
/// <param name="FieldsUpdated">The existing fields whose data type or nullability a refresh changed to match the source.</param>
public sealed record DataStoreSchemaImportResult(
    int ContainersDiscovered,
    int FieldsDiscovered,
    int PathsAdded,
    int ContainersAdded,
    int FieldsAdded,
    int FieldsUpdated);
