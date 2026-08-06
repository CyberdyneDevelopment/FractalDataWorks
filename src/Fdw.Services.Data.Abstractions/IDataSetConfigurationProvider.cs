// Why: IDataSetConfigurationProvider is defined in Fdw.Data.DataSets
// (namespace Fdw.Data.DataSets.Abstractions) because it returns DataSetConfiguration,
// which lives in that project. Services.Data.Abstractions cannot reference Data.DataSets without
// creating a circular dependency (Data.DataSets already references Services.Data.Abstractions).
