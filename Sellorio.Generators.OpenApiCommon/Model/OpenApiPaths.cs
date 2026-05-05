using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiPaths : OpenApiExtensibleObject
    {
        private readonly IDictionary<string, OpenApiPathItem> _items = new Dictionary<string, OpenApiPathItem>();

        public IEnumerable<KeyValuePair<string, OpenApiPathItem>> Items => _items;

        public OpenApiPathItem this[string path]
        {
            get => _items[path];
            set => _items[path] = value;
        }

        public int Count => _items.Count;

        public bool ContainsKey(string path)
        {
            return _items.ContainsKey(path);
        }

        public bool TryGetValue(string path, out OpenApiPathItem pathItem)
        {
            return _items.TryGetValue(path, out pathItem);
        }
    }
}
