using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Sellorio.Clients.Rest;

public static class ServiceCollectionExtensions
{
    public static IHttpClientBuilder? TryAddRestClient<TInterface, TImplementation>(this IServiceCollection services, Action<HttpClient>? configureClient = null, JsonSerializerOptions? jsonSerializerOptions = null)
        where TImplementation : TInterface
        where TInterface : class
    {
        if (services.Any(x => x.ServiceType == typeof(TInterface)))
        {
            return null;
        }

        var httpClientName = ">>" + typeof(TImplementation).Name;

        var httpClientBuilder =
            configureClient == null
                ? services.AddHttpClient(httpClientName)
                : services.AddHttpClient(httpClientName, configureClient);

        TryAddRestClient<TInterface, TImplementation>(services, httpClientName, jsonSerializerOptions);

        return httpClientBuilder;
    }

    public static IServiceCollection TryAddRestClient<TInterface, TImplementation>(this IServiceCollection services, string httpClientName, JsonSerializerOptions? jsonOptions)
        where TImplementation : TInterface
        where TInterface : class
    {
        if (services.Any(x => x.ServiceType == typeof(TInterface)))
        {
            return services;
        }

        var constructors = typeof(TImplementation).GetConstructors();

        if (constructors.Length != 1)
        {
            throw new InvalidOperationException("Only a single constructor is supported.");
        }

        var constructor = constructors[0];

        services.AddSingleton<TInterface>(svc =>
        {
            var httpClientFactory = svc.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);
            var authorizationProvider = svc.GetRequiredService<IRestClientAuthorizationProvider>();
            var restClient = new RestClient(httpClient, authorizationProvider, jsonOptions ?? Constants.DefaultJsonOptions);

            var constructorParameters =
                constructor.GetParameters()
                    .Select(x =>
                    {
                        if (x.ParameterType == typeof(IRestClient))
                        {
                            return restClient;
                        }

                        return
                            x.ParameterType == typeof(JsonSerializerOptions)
                                ? jsonOptions ?? Constants.DefaultJsonOptions
                                : svc.GetRequiredService(x.ParameterType);
                    })
                    .ToArray();

            return (TImplementation)constructor.Invoke(constructorParameters);
        });

        return services;
    }
}
