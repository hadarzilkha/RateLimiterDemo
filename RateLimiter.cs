/// <summary>
/// A flexible rate limiter that can wrap any async action.
/// Evolved through multiple iterations of API integration projects.
/// </summary>
public class RateLimiter<TArg>
{
    private readonly Func<TArg, Task> _action;
    private readonly List<RateLimitRule> _rules;

    public RateLimiter(Func<TArg, Task> action, IEnumerable<RateLimitRule> rules)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));

        if (!_rules.Any())
        {
            throw new ArgumentException("At least one rate limit rule is required.", nameof(rules));
        }
    }

    public async Task Perform(TArg arg, CancellationToken cancellationToken = default)
    {
        if (arg == null)
        {
            throw new ArgumentNullException(nameof(arg));
        }

        // Wait for all rules to allow the request (in parallel)
        await Task.WhenAll(_rules.Select(rule => rule.WaitUntilAllowedAsync(cancellationToken)));
        // Once allowed, perform the actual action
        await _action(arg);
    }
}
