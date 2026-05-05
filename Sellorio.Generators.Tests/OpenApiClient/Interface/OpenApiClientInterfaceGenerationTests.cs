using System.Reflection;
using Sellorio.Generators.Tests.OpenApiClient.Filtered;
using Sellorio.Results;
using static Sellorio.Generators.Tests.OpenApiClient.Support.OpenApiClientTestSupport;

namespace Sellorio.Generators.Tests.OpenApiClient.Interface;

public sealed class OpenApiClientInterfaceGenerationTests
{
    [Fact]
    public void GeneratedInterfaces_ExposeExpectedOperationsByClient()
    {
        var basicInterface = typeof(IGeneratedBasicClient);
        var filteredInterface = typeof(IGeneratedFilteredClient);

        Assert.Equal(
            ["CreateWidget", "CreateWidgetModel", "GetAdminReport", "GetWidget", "SearchWidgets"],
            basicInterface
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());

        Assert.Equal(
            ["CreateWidget", "CreateWidgetModel", "GetWidget", "SearchWidgets"],
            filteredInterface
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedReturnTypes()
    {
        Assert.Equal(
            "System.Threading.Tasks.Task`1[Sellorio.Results.ValueResult`1[Sellorio.Generators.Tests.OpenApiClient.GetWidgetResponse]]",
            typeof(IGeneratedBasicClient).GetMethod("GetWidget")!.ReturnType.ToString());

        Assert.Equal(
            typeof(Task<ValueResult<IReadOnlyList<string>>>),
            typeof(IGeneratedBasicClient).GetMethod("SearchWidgets")!.ReturnType);

        Assert.Equal(
            "System.Threading.Tasks.Task`1[Sellorio.Results.Result`1[System.Collections.Generic.IReadOnlyList`1[System.String]]]",
            typeof(IGeneratedBasicClient).GetMethod("CreateWidget")!.ReturnType.ToString());

        Assert.Equal(
            "System.Threading.Tasks.Task`1[Sellorio.Results.ValueResult`2[Sellorio.Generators.Tests.OpenApiClient.WidgetModel,Sellorio.Generators.Tests.OpenApiClient.CreateWidgetModelResponse]]",
            typeof(IGeneratedBasicClient).GetMethod("CreateWidgetModel")!.ReturnType.ToString());

        Assert.Equal(
            "System.Threading.Tasks.Task`1[Sellorio.Results.ValueResult`1[Sellorio.Generators.Tests.OpenApiClient.GetAdminReportResponse]]",
            typeof(IGeneratedBasicClient).GetMethod("GetAdminReport")!.ReturnType.ToString());

        Assert.Equal(
            typeof(Task<ValueResult<int>>),
            typeof(IGeneratedResponseClient).GetMethod("GetReport")!.ReturnType);

        Assert.Equal(
            typeof(Task<ValueResult<string>>),
            typeof(IGeneratedResponseClient).GetMethod("GetDefaultReport")!.ReturnType);

        Assert.Equal(
            typeof(Task<Result>),
            typeof(IGeneratedResponseClient).GetMethod("GetPlainReport")!.ReturnType);

        Assert.Equal(
            "System.Threading.Tasks.Task`1[Sellorio.Results.ValueResult`1[Sellorio.Generators.Tests.OpenApiClient.IGetPolymorphicReportResponse]]",
            typeof(IGeneratedPolymorphicClient).GetMethod("GetPolymorphicReport")!.ReturnType.ToString());
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedParameterOrderAndOptionalDefaults_ForGetWidget()
    {
        var parameters = typeof(IGeneratedBasicClient).GetMethod("GetWidget")!.GetParameters();

        Assert.Collection(
            parameters,
            parameter =>
            {
                Assert.Equal("widgetId", parameter.Name);
                Assert.Equal(typeof(Guid), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("includeDetails", parameter.Name);
                Assert.Equal(typeof(bool?), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("xCorrelationId", parameter.Name);
                Assert.Equal(typeof(string), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedParameterOrderAndOptionalDefaults_ForResponseClientMethods()
    {
        Assert.Collection(
            typeof(IGeneratedResponseClient).GetMethod("GetReport")!.GetParameters(),
            parameter =>
            {
                Assert.Equal("reportId", parameter.Name);
                Assert.Equal(typeof(int), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });

        Assert.Collection(
            typeof(IGeneratedResponseClient).GetMethod("GetDefaultReport")!.GetParameters(),
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });

        Assert.Collection(
            typeof(IGeneratedResponseClient).GetMethod("GetPlainReport")!.GetParameters(),
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedParameterOrderAndOptionalDefaults_ForPolymorphicClientMethod()
    {
        Assert.Collection(
            typeof(IGeneratedPolymorphicClient).GetMethod("GetPolymorphicReport")!.GetParameters(),
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedParameterOrderAndOptionalDefaults_ForSearchWidgets()
    {
        var parameters = typeof(IGeneratedBasicClient).GetMethod("SearchWidgets")!.GetParameters();

        Assert.Collection(
            parameters,
            parameter =>
            {
                Assert.Equal("xRequestId", parameter.Name);
                Assert.Equal(typeof(int), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("createdAfter", parameter.Name);
                Assert.Equal(typeof(DateTimeOffset?), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("score", parameter.Name);
                Assert.Equal(typeof(decimal?), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedParameterOrderAndOptionalDefaults_ForCreateWidget()
    {
        var parameters = typeof(IGeneratedBasicClient).GetMethod("CreateWidget")!.GetParameters();

        Assert.Collection(
            parameters,
            parameter =>
            {
                Assert.Equal("widgetPayload", parameter.Name);
                Assert.Equal(typeof(IReadOnlyList<string>), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });
    }

    [Fact]
    public void GeneratedInterfaces_UseExpectedParameterOrderAndOptionalDefaults_ForCreateWidgetModel()
    {
        var parameters = typeof(IGeneratedBasicClient).GetMethod("CreateWidgetModel")!.GetParameters();

        Assert.Collection(
            parameters,
            parameter =>
            {
                Assert.Equal("widgetModel", parameter.Name);
                Assert.Equal("WidgetModel", parameter.ParameterType.Name);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });
    }

    [Fact]
    public void GeneratedImplementationTypes_ArePresentOrOmittedAsExpected()
    {
        var assembly = typeof(IGeneratedBasicClient).Assembly;

        Assert.NotNull(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient"));
        Assert.NotNull(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient"));
        Assert.NotNull(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedPolymorphicClient"));
        Assert.Null(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedFilteredClient"));
    }

    [Fact]
    public void GeneratedTypes_ExposeExpectedPolymorphicContracts()
    {
        var assembly = typeof(IGeneratedBasicClient).Assembly;
        var interfaceType = assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.IGetPolymorphicReportResponse");
        var objectType = assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GetPolymorphicReport200response");
        var listType = assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GetPolymorphicReport201response");

        Assert.NotNull(interfaceType);
        Assert.NotNull(objectType);
        Assert.NotNull(listType);
        Assert.Contains(interfaceType, objectType.GetInterfaces());
        Assert.Contains(interfaceType, listType.GetInterfaces());
        Assert.Equal(typeof(List<string>), listType.BaseType);
    }

    [Fact]
    public void GeneratedTypes_ExposeExpectedModelProperties()
    {
        var widgetResponse = GetGeneratedType("Sellorio.Generators.Tests.OpenApiClient.GetWidgetResponse");
        var widgetModel = GetGeneratedType("Sellorio.Generators.Tests.OpenApiClient.WidgetModel");
        var createWidgetModelResponse = GetGeneratedType("Sellorio.Generators.Tests.OpenApiClient.CreateWidgetModelResponse");

        Assert.Equal(typeof(int), widgetResponse.GetProperty("Id")!.PropertyType);
        Assert.Equal(typeof(string), widgetResponse.GetProperty("Name")!.PropertyType);
        Assert.Equal(typeof(string), widgetModel.GetProperty("Name")!.PropertyType);
        Assert.Equal(typeof(int?), widgetModel.GetProperty("Quantity")!.PropertyType);
        Assert.Equal(typeof(int), createWidgetModelResponse.GetProperty("Id")!.PropertyType);
        Assert.Equal(typeof(string), createWidgetModelResponse.GetProperty("Name")!.PropertyType);
    }
}
