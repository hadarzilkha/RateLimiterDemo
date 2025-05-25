using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Manages a single rate limit rule using a sliding window strategy.
/// Thread-safe and designed for high-concurrency scenarios.
/// </summary>
/// 
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
    /// Waits until this rule allows a new request, based on the current timestamp queue.
    /// Returns the approved timestamp once allowed.
    /// </summary>
    public async Task<DateTime> WaitUntilAllowedAsync(CancellationToken cancellationToken = default)
    {
        bool allowed = false;

        while (!allowed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DateTime now = DateTime.UtcNow;
            TimeSpan? delay = null;

            lock (_lock)
            {
                // Remove timestamps that are outside the current sliding window
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                {
                    _timestamps.Dequeue();
                }

                // If we're under the limit, allow execution
                if (_timestamps.Count < _limit)
                {
                    allowed = true;
                    break;
                }

                // Otherwise, calculate how much time we need to wait
                DateTime oldest = _timestamps.Peek();
                delay = (oldest + _window) - now;
            }

            if (delay > TimeSpan.Zero)
            {
                // Wait outside the lock to avoid blocking other threads
                await Task.Delay(delay.Value, cancellationToken);
            }
            else
            {
                // Defensive yield in case of timing anomalies
                await Task.Yield();
            }
        }

        // Return the current timestamp once the rule allows it
        return DateTime.UtcNow;
    }


    /// <summary>
    /// Registers a timestamp in the queue once all rules have approved execution.
    /// </summary>
    public void RegisterTimestamp(DateTime timestamp)
    {
        lock (_lock)
        {
            _timestamps.Enqueue(timestamp);
        }
    }
}
