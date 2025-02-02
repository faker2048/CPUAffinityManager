namespace @_.Utils;

public class Debouncer : IDisposable
{
    private readonly Action _action;
    private readonly System.Timers.Timer _timer;
    private bool _isWaiting;

    public Debouncer(Action action, int milliseconds)
    {
        _action = action;
        _timer = new System.Timers.Timer(milliseconds) { AutoReset = false };
        _timer.Elapsed += (_, _) =>
        {
            _isWaiting = false;
            _action();
        };
    }

    public void Debounce()
    {
        if (_isWaiting) return;

        _isWaiting = true;
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
