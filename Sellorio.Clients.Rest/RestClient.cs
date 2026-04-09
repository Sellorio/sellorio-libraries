using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest;

internal class RestClient(HttpClient httpClient, IRestClientAuthorizationProvider authorizationProvider, JsonSerializerOptions jsonSerializerOptions) : IRestClient
{
    public Task<HttpResponseMessage> Get(FormattableString url)
    {
        return ExecuteRequestAsync(url, HttpMethod.Get);
    }

    public Task<HttpResponseMessage> Post(FormattableString url)
    {
        return ExecuteRequestAsync(url, HttpMethod.Post);
    }

    public Task<HttpResponseMessage> Post(FormattableString url, object body)
    {
        return ExecuteRequestAsync(url, HttpMethod.Post, body);
    }

    public Task<HttpResponseMessage> Post(FormattableString url, Stream body)
    {
        return ExecuteRequestAsync(url, HttpMethod.Post, body);
    }

    public Task<HttpResponseMessage> Put(FormattableString url)
    {
        return ExecuteRequestAsync(url, HttpMethod.Put);
    }

    public Task<HttpResponseMessage> Put(FormattableString url, object body)
    {
        return ExecuteRequestAsync(url, HttpMethod.Put, body);
    }

    public Task<HttpResponseMessage> Put(FormattableString url, Stream body)
    {
        return ExecuteRequestAsync(url, HttpMethod.Put, body);
    }

    public Task<HttpResponseMessage> Patch(FormattableString url)
    {
        return ExecuteRequestAsync(url, HttpMethod.Patch);
    }

    public Task<HttpResponseMessage> Patch(FormattableString url, object body)
    {
        return ExecuteRequestAsync(url, HttpMethod.Patch, body);
    }

    public Task<HttpResponseMessage> Patch(FormattableString url, Stream body)
    {
        return ExecuteRequestAsync(url, HttpMethod.Patch, body);
    }

    public Task<HttpResponseMessage> Delete(FormattableString url)
    {
        return ExecuteRequestAsync(url, HttpMethod.Delete);
    }

    private async Task<HttpResponseMessage> ExecuteRequestAsync(FormattableString url, HttpMethod method, object? body = null)
    {
        var uri = ParseUri(url);
        using var request = new HttpRequestMessage(method, uri);

        if (body != null)
        {
            if (body is Stream stream)
            {
                MultipartFormDataContent? content = null;
                StreamContent? streamContent = null;

                try
                {
                    content = new MultipartFormDataContent();
                    streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new(MediaTypeNames.Application.Octet);
                    content.Add(streamContent, "file", "file");
                    request.Content = content;
                    content = null;
                    streamContent = null;
                }
                finally
                {
                    streamContent?.Dispose();
                    content?.Dispose();
                }
            }
            else
            {
                var bodyJson = JsonSerializer.Serialize(body, jsonSerializerOptions);
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
            }
        }

        try
        {
            var sessionToken = await authorizationProvider.GetAuthorizationHeaderAsync();

            if (sessionToken != null)
            {
                request.Headers.Authorization = sessionToken;
            }

            return await httpClient.SendAsync(request);
        }
        finally
        {
            request.Content?.Dispose();
        }
    }

    private static Uri ParseUri(FormattableString url)
    {
        var arguments = url.GetArguments().Select(x => UriSerialize(x, true)).ToArray();
        var uriString = string.Format(url.Format.TrimStart('/'), arguments);
        return new Uri(uriString, UriKind.Relative);
    }

    private static string? UriSerialize(object? value, bool allowObjects)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case bool:
                return value.ToString()!.ToLower();
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                return value.ToString();
            case string stringValue:
                return WebUtility.UrlEncode(stringValue);
            case DateOnly dateValue:
                return dateValue.ToString("yyyy-MM-dd");
            case TimeOnly timeValue:
                return timeValue.ToString(@"HH\:mm\:ss");
            case DateTime dateTimeValue:
                return dateTimeValue.ToString(@"yyyy-MM-dd'T'HH\:mm\:ss.ffffffzzz");
            case DateTimeOffset dateTimeOffsetValue:
                return dateTimeOffsetValue.ToString(@"yyyy-MM-dd'T'HH\:mm\:ss.ffffffzzz");
            case TimeSpan timeSpanValue:
                return timeSpanValue.ToString(@"HH\:mm\:ss");
            case Enum enumValue:
                var enumValues = Enum.GetValues(enumValue.GetType()).Cast<Enum>().ToArray();

                if (enumValues.Any(x => x.Equals(enumValue)))
                {
                    return enumValue.ToString();
                }
                else
                {
                    return
                        string.Join(
                            ',',
                            enumValues
                                .Where(x => (int)Convert.ChangeType(x, typeof(int)) != 0)
                                .Where(enumValue.HasFlag)
                                .Select(x => x.ToString())
                                .ToArray());
                }
        }

        if (!allowObjects)
        {
            throw new InvalidOperationException("Unexpected value type in url pattern.");
        }

        var type = value.GetType();
        var queryString = new Dictionary<string, string?>();

        foreach (var property in type.GetProperties())
        {
            var propertyValue = property.GetValue(value);

            if (propertyValue != null)
            {
                queryString.Add(property.Name, UriSerialize(propertyValue, false));
            }
        }

        if (queryString.Count != 0)
        {
            return "?" + string.Join('&', queryString.Select(x => $"{x.Key}={x.Value}"));
        }

        return string.Empty;
    }
}
