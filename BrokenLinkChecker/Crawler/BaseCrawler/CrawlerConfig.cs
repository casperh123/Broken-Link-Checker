using BrokenLinkChecker.models;

namespace BrokenLinkChecker.crawler;

public class CrawlerConfig(int concurrentRequests, CrawlMode crawlMode, bool jitter = true)
{
    public readonly int ConcurrentRequests = concurrentRequests;
    public readonly SemaphoreSlim Semaphore = new(concurrentRequests);
    public readonly CrawlMode CrawlMode = crawlMode;
    private bool Jitter => ConcurrentRequests > 1 && jitter;
    private int JitterFrequency => ConcurrentRequests * 100;
    
    public async Task ApplyJitterAsync()
    {
        if (!Jitter)
        {
            return;
        }
        
        int delay = Random.Shared.Next(0, JitterFrequency);
        await Task.Delay(delay);
    }
}

