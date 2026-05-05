using Sellorio.Generators.OpenApiClient;

namespace Sellorio.Generators.Tests.OpenApiClient.Filtered;

[GenerateOpenApiClient("open-api-client-basic.yaml", GenerateImplementation = false, IncludedTags = ["widgets"])]
public partial interface IGeneratedFilteredClient
{
}
