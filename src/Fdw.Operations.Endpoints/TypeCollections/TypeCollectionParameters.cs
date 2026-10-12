using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.OptionTypes;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Reads the parameters a TypeCollection option takes from what the option declares about itself (FDW-1110, contract
/// item 4): the configuration fields of a connection capability, the option shape of a load mode or load step kind, and
/// the requirements of a value source kind. Nothing is listed for an option that declares none.
/// </summary>
internal static class TypeCollectionParameters
{
    private const string ColumnList = "ColumnList";

    public static List<TypeCollectionValueParameterDto> For(ITypeOption typeOption)
    {
        var parameters = new List<TypeCollectionValueParameterDto>();
        AddConfigurationFields(typeOption, parameters);
        if (typeOption is ILoadOptionShape shape)
            AddLoadOptions(shape, parameters);
        if (typeOption is ILoadStepKind kind)
            AddStepKindParameters(kind, parameters);
        if (typeOption is IValueSourceKind source)
            AddValueSourceParameters(source, parameters);
        if (typeOption is IStageMemberShape stage)
            AddStageMembers(stage, parameters);
        return parameters;
    }

    private static void AddLoadOptions(ILoadOptionShape shape, List<TypeCollectionValueParameterDto> parameters)
    {
        if (shape.UsesKeyColumns)
            parameters.Add(Parameter("keyColumns", ColumnList, true, "Destination columns that match a row"));
        if (shape.UsesCompareColumns)
            parameters.Add(Parameter("compareColumns", ColumnList, false, "Columns that make a matched row count as changed; none means every matched row does"));
        if (shape.UsesWriteColumns)
            parameters.Add(Parameter("writeColumns", ColumnList, true, "Destination columns written"));
        if (shape.AllowsVersioning)
            parameters.Add(Parameter("versioning", "Versioning", false, "Close the current row and open a new one when a compare column differs"));
        if (shape.AllowsSoftDelete)
            parameters.Add(Parameter("softDelete", "SoftDelete", false, "Set a deleted flag instead of removing the row"));
    }

    private static void AddStepKindParameters(ILoadStepKind kind, List<TypeCollectionValueParameterDto> parameters)
    {
        if (kind.UsesSourceDataSet)
        {
            parameters.Add(Parameter("sourceDataSet", "DataSet", true, "Data set the rows are copied from, on the destination's connection"));
            parameters.Add(Parameter("filterExpression", "Expression", false, "Filter on the source; may reference {{run parameters}}"));
        }

        if (kind.UsesDestinationContainer)
        {
            parameters.Add(Parameter("destinationContainer", "Container", true, "Executable container of one of the destination data set's data stores"));
            parameters.Add(Parameter("parameters", "ExecuteParameters", false, "Values passed to the container's parameters: constants or the current timestamp"));
        }
    }

    // A transform kind lists the members of a pipeline stage body it owns, so the console builds a stage's form from them.
    private static void AddStageMembers(IStageMemberShape stage, List<TypeCollectionValueParameterDto> parameters)
    {
        foreach (var member in stage.Members)
            parameters.Add(Parameter(member.Name, member.Kind, member.IsRequired, member.HelpText));
    }

    private static void AddValueSourceParameters(IValueSourceKind source, List<TypeCollectionValueParameterDto> parameters)
    {
        if (source.RequiresSourceField)
            parameters.Add(Parameter("sourceField", "Field", true, "Source field that supplies the column"));
        if (source.RequiresConstant)
        {
            parameters.Add(Parameter("valueType", "DataType", true, "Type of the constant"));
            parameters.Add(Parameter("value", "Text", true, "The constant, as text"));
        }

        if (source.RequiresSequenceName)
        {
            parameters.Add(Parameter("sequenceName", "Text", true, "Name of the sequence"));
            parameters.Add(Parameter("schema", "Text", false, "Schema of the sequence"));
            parameters.Add(Parameter("database", "Text", false, "Database of the sequence"));
        }

        if (source.RequiresSequenceOrder)
            parameters.Add(Parameter("orderBy", ColumnList, true, "Columns the sequenced batch is ordered by"));
    }

    // Connection capabilities describe their configuration as ConfigurationFieldDescriptor records; the property is read
    // by reflection because this assembly does not depend on the connection packages.
    private static void AddConfigurationFields(ITypeOption typeOption, List<TypeCollectionValueParameterDto> parameters)
    {
        var fields = typeOption.GetType().GetProperty("ConfigurationFields", BindingFlags.Public | BindingFlags.Instance)?.GetValue(typeOption);
        if (fields is not System.Collections.IEnumerable descriptors)
            return;

        foreach (var descriptor in descriptors.Cast<object>())
        {
            var type = descriptor.GetType();
            parameters.Add(Parameter(
                string.Concat(type.GetProperty("Key")?.GetValue(descriptor) as string),
                string.Concat(type.GetProperty("InputKind")?.GetValue(descriptor) as string),
                type.GetProperty("IsRequired")?.GetValue(descriptor) is true,
                string.Concat(type.GetProperty("Label")?.GetValue(descriptor) as string)));
        }
    }

    private static TypeCollectionValueParameterDto Parameter(string name, string kind, bool isRequired, string helpText) =>
        new() { Name = name, Kind = kind, IsRequired = isRequired, HelpText = helpText };
}
