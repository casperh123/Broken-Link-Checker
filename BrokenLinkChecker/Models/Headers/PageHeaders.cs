using System.Net.Http.Headers;
using BrokenLinkChecker.models;
using System.Linq;

namespace BrokenLinkChecker.Models.Headers
{
    public record PageHeaders
    {
        public string ContentEncoding { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        public string Server { get; set; } = string.Empty;
        public Cache Cache { get; set; } = new Cache();

        public PageHeaders()
        {
        }

        public PageHeaders(HttpResponseHeaders headers, HttpContentHeaders contentHeaders)
        {
            ContentEncoding = contentHeaders.ContentEncoding.Any()
                ? string.Join(", ", contentHeaders.ContentEncoding)
                : string.Empty;
            ContentType = contentHeaders.ContentType?.MediaType ?? string.Empty;
            LastModified = contentHeaders.LastModified?.ToString() ?? string.Empty;
            Server = headers.Server?.ToString() ?? string.Empty; 
            Cache = new Cache(headers);
        }
    }
}