/// <summary>
/// A flexible and thread-safe rate limiter that wraps any async action with one or more rate limit rules.
/// Ensures all rules are respected before execution.
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

    /// <summary>
    /// Performs the wrapped action after all rate limit rules allow execution.
    /// Ensures timestamps are registered only after all checks pass.
    /// </summary>
    public async Task Perform(TArg arg, CancellationToken cancellationToken = default)
    {
        if (arg == null) throw new ArgumentNullException(nameof(arg));

        List<DateTime> timestamps = new();

        // Wait for each rule to allow execution, one at a time
        foreach (var rule in _rules)
        {
            var approvedTime = await rule.WaitUntilAllowedAsync(cancellationToken);
            timestamps.Add(approvedTime);
        }

        // Only after all rules have approved, register the timestamp in each
        for (int i = 0; i < _rules.Count; i++)
        {
            _rules[i].RegisterTimestamp(timestamps[i]);
        }

        // Execute the original action
        await _action(arg);
    }
}
