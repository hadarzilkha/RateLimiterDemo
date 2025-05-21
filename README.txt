# RateLimiter (C#)

This is a simple and thread-safe rate limiter implementation in C# that supports multiple rate limit rules (like "10 requests per second", "100 per minute", etc.). It uses a sliding window strategy to ensure that all defined limits are respected before any action is executed.

## What it does
You pass it a function (like an API call), and it wraps it with one or more rate limits. Before each execution, it checks all the rules. If even one rule is at its limit, it waits until it’s safe to continue.

**For example:**
- 3 calls every 5 seconds
- 10 calls every 1 minute
- 1000 calls per day

All of these can be enforced at the same time.

## Sliding window vs. absolute window
I chose to use a **sliding window** approach instead of an absolute one.

### Sliding window:
- Looks at the last *N seconds* from the current moment

### Absolute window:
- Resets every fixed interval (like “at the start of the minute”)

### Why sliding window?
- It avoids bursts of requests at reset boundaries (like at 00:00)
- It gives smoother traffic control
- It’s closer to how real-world APIs work

Yes, it’s a bit more complex to implement, but it gives much better behavior — especially when dealing with concurrency or real production environments.

## How it works
- Each `RateLimitRule` keeps a queue of timestamps for recent executions
- Each rule exposes a `WaitUntilAllowedAsync` method that waits until the rule allows a new request
- Once **all** rules approve, the timestamps are registered in each rule before the action is executed
- The logic ensures that no timestamp is recorded before all rules have allowed execution — avoiding inconsistent or premature tracking
- `DateTime.UtcNow` is captured **inside each lock** to guarantee time consistency
- Delay calculations and checks are fully synchronized to avoid stale or race-prone logic
- All of this is done using async code, so threads are not blocked

## Example
```csharp
var rules = new List<RateLimitRule>
{
    new RateLimitRule(3, TimeSpan.FromSeconds(5)),
    new RateLimitRule(10, TimeSpan.FromMinutes(1))
};

var rateLimitedApi = new RateLimiter<int>(async (id) =>
{
    Console.WriteLine($"Executing request #{id} at {DateTime.Now:HH:mm:ss.fff}");
    await Task.Delay(50);
}, rules);

var tasks = Enumerable.Range(0, 20)
    .Select(i => rateLimitedApi.Perform(i))
    .ToList();

await Task.WhenAll(tasks);