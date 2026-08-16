using Fdw.Data.Abstractions;
using Fdw.Data.RowSources.Abstractions;

namespace Fdw.Data.RowSources.Abstractions.Tests;

public class RowMappingContextBaseTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsFieldOrdinals()
    {
        var ordinals = new[] { 0, 1, 2 };
        var names = new[] { "A", "B", "C" };
        var converters = new IDataTypeConverter?[] { null, null, null };

        var sut = CreateContext(ordinals, names, converters);

        sut.FieldOrdinals.ShouldBe(ordinals);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsFieldNames()
    {
        var ordinals = new[] { 0, 1 };
        var names = new[] { "Name", "Age" };
        var converters = new IDataTypeConverter?[] { null, null };

        var sut = CreateContext(ordinals, names, converters);

        sut.FieldNames.ShouldBe(names);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsFieldConverters()
    {
        var mockConverter = new Mock<IDataTypeConverter>();
        var ordinals = new[] { 0 };
        var names = new[] { "Name" };
        var converters = new IDataTypeConverter?[] { mockConverter.Object };

        var sut = CreateContext(ordinals, names, converters);

        sut.FieldConverters[0].ShouldBe(mockConverter.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorSetsFieldCount()
    {
        var ordinals = new[] { 0, 1, 2 };
        var names = new[] { "A", "B", "C" };
        var converters = new IDataTypeConverter?[] { null, null, null };

        var sut = CreateContext(ordinals, names, converters);

        sut.FieldCount.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenOrdinalsAndNamesLengthMismatch()
    {
        var ordinals = new[] { 0, 1 };
        var names = new[] { "A" };
        var converters = new IDataTypeConverter?[] { null, null };

        Should.Throw<ArgumentException>(() => CreateContext(ordinals, names, converters));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorThrowsWhenOrdinalsAndConvertersLengthMismatch()
    {
        var ordinals = new[] { 0, 1 };
        var names = new[] { "A", "B" };
        var converters = new IDataTypeConverter?[] { null };

        Should.Throw<ArgumentException>(() => CreateContext(ordinals, names, converters));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void ConstructorAcceptsEmptyArrays()
    {
        var sut = CreateContext([], [], []);

        sut.FieldCount.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void FieldConvertersCanContainNullEntries()
    {
        var mockConverter = new Mock<IDataTypeConverter>();
        var ordinals = new[] { 0, 1, 2 };
        var names = new[] { "A", "B", "C" };
        var converters = new IDataTypeConverter?[] { mockConverter.Object, null, mockConverter.Object };

        var sut = CreateContext(ordinals, names, converters);

        sut.FieldConverters[0].ShouldNotBeNull();
        sut.FieldConverters[1].ShouldBeNull();
        sut.FieldConverters[2].ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void NegativeOrdinalsAreAllowed()
    {
        var ordinals = new[] { -1, 0, -1 };
        var names = new[] { "Missing1", "Found", "Missing2" };
        var converters = new IDataTypeConverter?[] { null, null, null };

        var sut = CreateContext(ordinals, names, converters);

        sut.FieldOrdinals[0].ShouldBe(-1);
        sut.FieldOrdinals[1].ShouldBe(0);
        sut.FieldOrdinals[2].ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateBuildsContextFromSourceAndContainer()
    {
        var mockSource = new Mock<IRecordCursor>();
        mockSource.Setup(s => s.GetFieldOrdinal("Name")).Returns(0);
        mockSource.Setup(s => s.GetFieldOrdinal("Age")).Returns(1);

        var mockField1 = new Mock<IField>();
        mockField1.Setup(f => f.Name).Returns("Name");
        mockField1.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var mockField2 = new Mock<IField>();
        mockField2.Setup(f => f.Name).Returns("Age");
        mockField2.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(new List<IField> { mockField1.Object, mockField2.Object }.AsReadOnly());
        mockSchema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { mockField1.Object, mockField2.Object }.AsReadOnly());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        var result = RowMappingContextBase.Create(mockSource.Object, mockContainer.Object);

        result.FieldCount.ShouldBe(2);
        result.FieldNames[0].ShouldBe("Name");
        result.FieldNames[1].ShouldBe("Age");
        result.FieldOrdinals[0].ShouldBe(0);
        result.FieldOrdinals[1].ShouldBe(1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSetsNegativeOrdinalForMissingFields()
    {
        var mockSource = new Mock<IRecordCursor>();
        mockSource.Setup(s => s.GetFieldOrdinal("Missing")).Returns(-1);

        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns("Missing");
        mockField.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(new List<IField> { mockField.Object }.AsReadOnly());
        mockSchema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { mockField.Object }.AsReadOnly());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        var result = RowMappingContextBase.Create(mockSource.Object, mockContainer.Object);

        result.FieldOrdinals[0].ShouldBe(-1);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithoutConverterCollectionLeavesConvertersNull()
    {
        var mockSource = new Mock<IRecordCursor>();
        mockSource.Setup(s => s.GetFieldOrdinal("Name")).Returns(0);

        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns("Name");
        mockField.Setup(f => f.ConverterTypeId).Returns(42);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(new List<IField> { mockField.Object }.AsReadOnly());
        mockSchema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { mockField.Object }.AsReadOnly());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        var result = RowMappingContextBase.Create(mockSource.Object, mockContainer.Object);

        result.FieldConverters[0].ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithConverterCollectionLooksUpConvertersByTypeId()
    {
        var mockConverter = new Mock<IDataTypeConverter>();
        var mockConverters = new Mock<IDataTypeConverters>();
        mockConverters.Setup(c => c.ById(42)).Returns(mockConverter.Object);

        var mockSource = new Mock<IRecordCursor>();
        mockSource.Setup(s => s.GetFieldOrdinal("Name")).Returns(0);

        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns("Name");
        mockField.Setup(f => f.ConverterTypeId).Returns(42);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(new List<IField> { mockField.Object }.AsReadOnly());
        mockSchema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { mockField.Object }.AsReadOnly());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        var result = RowMappingContextBase.Create(mockSource.Object, mockContainer.Object, mockConverters.Object);

        result.FieldConverters[0].ShouldBe(mockConverter.Object);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateSkipsConverterLookupWhenConverterTypeIdIsNull()
    {
        var mockConverters = new Mock<IDataTypeConverters>();

        var mockSource = new Mock<IRecordCursor>();
        mockSource.Setup(s => s.GetFieldOrdinal("Name")).Returns(0);

        var mockField = new Mock<IField>();
        mockField.Setup(f => f.Name).Returns("Name");
        mockField.Setup(f => f.ConverterTypeId).Returns((int?)null);

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(new List<IField> { mockField.Object }.AsReadOnly());
        mockSchema.Setup(s => s.GetProjectableFields()).Returns(new List<IField> { mockField.Object }.AsReadOnly());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        var result = RowMappingContextBase.Create(mockSource.Object, mockContainer.Object, mockConverters.Object);

        result.FieldConverters[0].ShouldBeNull();
        mockConverters.Verify(c => c.ById(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataIntegrity")]
    public void CreateWithEmptySchemaReturnsZeroFieldContext()
    {
        var mockSource = new Mock<IRecordCursor>();

        var mockSchema = new Mock<IContainerSchema>();
        mockSchema.Setup(s => s.Fields).Returns(new List<IField>().AsReadOnly());
        mockSchema.Setup(s => s.GetProjectableFields()).Returns(new List<IField>().AsReadOnly());

        var mockContainer = new Mock<IStorageContainer>();
        mockContainer.Setup(c => c.Schema).Returns(mockSchema.Object);

        var result = RowMappingContextBase.Create(mockSource.Object, mockContainer.Object);

        result.FieldCount.ShouldBe(0);
    }

    private static RowMappingContextBase CreateContext(int[] ordinals, string[] names, IDataTypeConverter?[] converters)
    {
        // Use the static Create method path by constructing via reflection since
        // DefaultRowMappingContext is internal. Instead, we test via the Create factory.
        // But for direct constructor testing, we need a test-only subclass.
        return new TestRowMappingContext(ordinals, names, converters);
    }

    private sealed class TestRowMappingContext : RowMappingContextBase
    {
        public TestRowMappingContext(int[] ordinals, string[] names, IDataTypeConverter?[] converters)
            : base(ordinals, names, converters)
        {
        }
    }
}
