using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiResponses : OpenApiExtensibleObject
    {
        private readonly IDictionary<string, OpenApiReferenceOr<OpenApiResponse>> _items = new Dictionary<string, OpenApiReferenceOr<OpenApiResponse>>();

        public IEnumerable<KeyValuePair<string, OpenApiReferenceOr<OpenApiResponse>>> Items => _items;

        public OpenApiReferenceOr<OpenApiResponse> this[string responseCode]
        {
            get => _items[responseCode];
            set => _items[responseCode] = value;
        }

        public int Count => _items.Count;

        public bool ContainsKey(string responseCode)
        {
            return _items.ContainsKey(responseCode);
        }

        public bool TryGetValue(string responseCode, out OpenApiReferenceOr<OpenApiResponse> response)
        {
            return _items.TryGetValue(responseCode, out response);
        }
    }
}
