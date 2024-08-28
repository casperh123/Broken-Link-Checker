using System.Net.Http.Headers;

namespace BrokenLinkChecker.Models.Headers;

public record Cache
{
    public string Age { get; set; } = "";
    public string CacheStatus = "";
    public string CacheControl = "";
    public Dictionary<string, string> CacheHeaders { get; set; } = [];

    public Cache()
    {
    }
    
    public Cache(HttpResponseHeaders headers)
    {
        Age = headers.Age.ToString() ?? string.Empty;
        CacheControl = headers.CacheControl?.ToString() ?? "";
        CacheStatus = headers.TryGetValues("x-cache", out var cacheStatus) ? string.Join(", ", cacheStatus) : "";
        CacheHeaders = [];
        
        foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
        {
            if (header.Key.Contains("cache"))
            {
                CacheHeaders[header.Key] = header.Value.Aggregate((acc, val) => acc + ", " + val);
            }
        }
    }
}