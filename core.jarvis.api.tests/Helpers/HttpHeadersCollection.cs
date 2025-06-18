using System.Collections;

namespace core.jarvis.api.tests.Integration.Functions;

/// <summary>
/// Test implementation of HttpHeadersCollection for mocking HTTP headers.
/// </summary>
public class HttpHeadersCollection : IEnumerable<KeyValuePair<string, IEnumerable<string>>>
{
    private readonly Dictionary<string, List<string>> _headers = new(StringComparer.OrdinalIgnoreCase);

    public void Add(string name, string value)
    {
        if (!_headers.TryGetValue(name, out var values))
        {
            values = new List<string>();
            _headers[name] = values;
        }
        values.Add(value);
    }

    public void Add(string name, IEnumerable<string> values)
    {
        if (!_headers.TryGetValue(name, out var existingValues))
        {
            existingValues = new List<string>();
            _headers[name] = existingValues;
        }
        existingValues.AddRange(values);
    }

    public IEnumerable<string>? GetValues(string name)
    {
        return _headers.TryGetValue(name, out var values) ? values : null;
    }

    public bool Contains(string name)
    {
        return _headers.ContainsKey(name);
    }

    public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
    {
        foreach (var kvp in _headers)
        {
            yield return new KeyValuePair<string, IEnumerable<string>>(kvp.Key, kvp.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}