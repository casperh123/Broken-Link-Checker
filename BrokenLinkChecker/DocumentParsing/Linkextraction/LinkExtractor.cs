using System.Net;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using BrokenLinkChecker.crawler;
using BrokenLinkChecker.DocumentParsing.Browsing;
using BrokenLinkChecker.models;
using BrokenLinkChecker.Models.Links;
using BrokenLinkChecker.utility;

namespace BrokenLinkChecker.DocumentParsing.Linkextraction
{
    public class LinkExtractor
    {
        private readonly HtmlParserPool _htmlParserPool;
        private readonly CrawlerConfig _crawlerConfig;

        public LinkExtractor(CrawlerConfig crawlerConfig)
        {
            _crawlerConfig = crawlerConfig;
            HtmlParserOptions htmlParsingOptions = new HtmlParserOptions { IsKeepingSourceReferences = true };
            _htmlParserPool = new HtmlParserPool(htmlParsingOptions, crawlerConfig.ConcurrentRequests);
        }

        public async Task<IEnumerable<Link>> GetLinksFromResponseAsync(HttpResponseMessage response, Link url)
        {
            if (!response.IsSuccessStatusCode || url.Type is not ResourceType.Page)
            {
                byte[] responseContent = await response.Content.ReadAsByteArrayAsync();
                return Enumerable.Empty<Link>();
            } 

            await using Stream document = await response.Content.ReadAsStreamAsync();
            return await ExtractLinksFromDocumentAsync(document, url);
        }

        private async Task<List<Link>> ExtractLinksFromDocumentAsync(Stream document, Link checkingUrl)
        {
            IDocument doc;

            using (PooledHtmlParser pooledHtmlParser = await _htmlParserPool.GetParserAsync())
            {
                doc = await pooledHtmlParser.Parser.ParseDocumentAsync(document);
            }

            return GetLinksFromDocument(doc, checkingUrl);
        }

        private List<Link> GetLinksFromDocument(IDocument document, Link checkingUrl)
        {
            List<Link> links = [];
            Uri thisUrl = new Uri(checkingUrl.Target);
            
            foreach (IElement link in document.Links)
            {
                string? href = link.GetAttribute("href");
                if (!string.IsNullOrEmpty(href) && !IsExcluded(href))
                {
                    Link newLink = GenerateLinkNode(link, checkingUrl.Target, "href", ResourceType.Page);
                    links.Add(newLink);
                }
            }

            if (_crawlerConfig.CrawlMode is CrawlMode.CacheWarm)
            {
                return links.Where(link => Uri.TryCreate(link.Target, UriKind.Absolute, out Uri uri) && uri.Host == thisUrl.Host).ToList();
            }
            
            foreach (IStyleSheet stylesheet in document.StyleSheets)
            {
                string href = stylesheet.Href;
                if (!string.IsNullOrEmpty(href))
                {
                    Link newLink = GenerateLinkNode(stylesheet.OwnerNode, checkingUrl.Target, "href", ResourceType.Stylesheet);
                    links.Add(newLink);
                }
            }

            foreach (IHtmlScriptElement script in document.Scripts)
            {
                string? src = script.Source;
                if (!string.IsNullOrEmpty(src))
                {
                    Link newLink = GenerateLinkNode(script, checkingUrl.Target, "src", ResourceType.Script);
                    links.Add(newLink);
                }
            }

            foreach (IHtmlImageElement image in document.Images)
            {
                string? src = image.Source;
                if (!string.IsNullOrEmpty(src))
                {
                    Link newLink = GenerateLinkNode(image, checkingUrl.Target, "src", ResourceType.Image);
                    links.Add(newLink);
                }
            }

            return links.Where(link => Uri.TryCreate(link.Target, UriKind.Absolute, out Uri uri) && uri.Host == thisUrl.Host).ToList();
        }

        private Link GenerateLinkNode(IElement element, string target, string attribute, ResourceType resourceType = ResourceType.Resource)
        {
            string href = element.GetAttribute(attribute) ?? string.Empty;
            string resolvedUrl = Utilities.GetUrl(target, href);
            string text = element.TextContent;
            int line = element.SourceReference?.Position.Line ?? -1;

            return new Link(target, resolvedUrl, text, line, resourceType);
        }
        
        private static bool IsExcluded(string url)
        {
            HashSet<string> excludedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".js", ".css" };
            HashSet<string> asyncKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ajax", "async", "action=async" };

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                if (excludedExtensions.Contains(Path.GetExtension(uri.LocalPath)))
                {
                    return false;
                }
            }

            return asyncKeywords.Any(url.Contains) || url.Contains('#') || url.Contains('?') || url.Contains("mailto");
        }
    }
}
