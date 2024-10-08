using BrokenLinkChecker.Crawler.ExtendedCrawlers;
using BrokenLinkChecker.models.Links;

namespace BrokenLinkChecker.DocumentParsing.LinkProcessors;

public interface ILinkProcessor<T> where T : NavigationLink
{
    public Task<IEnumerable<T>> ProcessLinkAsync(T link, ModularCrawlResult<T> crawlResult);
}