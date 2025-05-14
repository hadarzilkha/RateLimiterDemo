class Program
{
    static async Task Main(string[] args)
    {
        // Define two rate limit rules for the API
        var apiRules = new List<RateLimitRule>
        {
            new RateLimitRule(3, TimeSpan.FromSeconds(5)),
            new RateLimitRule(10, TimeSpan.FromMinutes(1))
        };

        // Wrap the action (simulated API call) with the rate limiter
        var rateLimitedApi = new RateLimiter<int>(async (requestId) =>
        {
            Console.WriteLine($"Executing request #{requestId} at {DateTime.Now:HH:mm:ss.fff}");
            await Task.Delay(50); // Simulate latency
        }, apiRules);

        // Run 20 requests through the rate limiter
        var tasks = Enumerable.Range(0, 20)
            .Select(i => rateLimitedApi.Perform(i))
            .ToList();
            
        // Wait until all requests are completed
        await Task.WhenAll(tasks);
    }
}