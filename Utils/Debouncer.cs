namespace @_.Utils;

public class Debouncer : IDisposable
{
    private readonly Action _action;
    private readonly System.Timers.Timer _timer;
    private readonly object _lock = new();
    private bool _isWaiting;
    private DateTime _lastCallTime = DateTime.MinValue;

    public Debouncer(Action action, int milliseconds)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _timer = new System.Timers.Timer(milliseconds) { AutoReset = false };
        _timer.Elapsed += (_, _) =>
        {
            lock (_lock)
            {
                _isWaiting = false;
                ExecuteAction();
            }
        };
    }

    public void Debounce()
    {
        lock (_lock)
        {
            if (_isWaiting) return;

            var now = DateTime.Now;

            if (now - _lastCallTime > TimeSpan.FromMilliseconds(5000))
            {
                ExecuteAction();
                return;
            }

            _isWaiting = true;
            _timer.Start();
        }
    }

    private void ExecuteAction()
    {
        _action();
        _lastCallTime = DateTime.Now;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
