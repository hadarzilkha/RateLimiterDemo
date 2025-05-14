using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages a single rate limit rule with a sliding window approach.
/// </summary>

public class RateLimitRule
{
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly Queue<DateTime> _timestamps = new Queue<DateTime>();
    private readonly object _lock = new object();

    public RateLimitRule(int limit, TimeSpan window)
    {
        if (limit <= 0) throw new ArgumentException("Limit must be positive.", nameof(limit));
        if (window <= TimeSpan.Zero) throw new ArgumentException("Window must be positive.", nameof(window));

        _limit = limit;
        _window = window;
    }

    /// <summary>
    /// Waits until the rule allows a new request, based on sliding window.
    /// </summary>
    public async Task WaitUntilAllowedAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DateTime now = DateTime.UtcNow;
            TimeSpan delay = TimeSpan.Zero;

            lock (_lock)
            {
                // Remove timestamps that are outside the current sliding window
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                {
                    _timestamps.Dequeue();
                }

                // If we're under the limit, allow the request immediately
                if (_timestamps.Count < _limit)
                {
                    // Only enqueue after checking inside the lock to avoid race conditions
                    // where multiple threads might think they are under the limit simultaneously
                    _timestamps.Enqueue(now);
                    return;
                }

                // Otherwise, calculate how much time we need to wait until the earliest timestamp expires
                DateTime oldest = _timestamps.Peek();
                delay = (oldest + _window) - now;
            }

            if (delay > TimeSpan.Zero)
            {
                // Wait outside the lock to avoid blocking other threads
                await Task.Delay(delay, cancellationToken);
            }

            // After waiting, loop again to re-check the rate limit condition
        }
    }
}
