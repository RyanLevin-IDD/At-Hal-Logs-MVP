using At_Hal_Logs;
using AutomationHoistinger;

internal class TimeManager
{
    private readonly AppWindow _form;
    private readonly System.Windows.Forms.Timer _timer;
    private bool _isRunningAutomation = false;
    public bool isTimerRunning;
    private DateTime _nextRunTime = DateTime.MinValue;

    public TimeManager(AppWindow form)
    {
        _form = form;
        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 1000; // check every second
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        ScheduleNextRun(DateTime.Now); // schedule on next rounded hour
        isTimerRunning = true;
        _timer.Start();
    }

    public void Stop()
    {
        isTimerRunning = false;
        _timer.Stop();
    }

    public void Reset()
    {
        _nextRunTime = DateTime.MinValue;
        ScheduleNextRun(DateTime.Now);
    }

    public void ResetNextRunBasedOnSelection() // optional, can still call ResetNextRun
    {
        ScheduleNextRun(DateTime.Now);
    }

    private async void Timer_Tick(object sender, EventArgs e)
    {
        if (_isRunningAutomation) return;

        DateTime now = DateTime.Now;

        if (_nextRunTime != DateTime.MinValue && now >= _nextRunTime)
        {
            _isRunningAutomation = true;
            DateTime start = DateTime.Now;
            await _form.RunAutomationAsync(_form.CancellationTokenSource.Token);
            DateTime end = DateTime.Now;

            // Schedule next run on the next rounded hour
            ScheduleNextRun(end);

            _isRunningAutomation = false;
        }

        UpdateCountdownLabel(now);
    }

    private void ScheduleNextRun(DateTime fromTime)
    {
        // Schedule for the next rounded hour
        DateTime nextRun = new DateTime(fromTime.Year, fromTime.Month, fromTime.Day, fromTime.Hour, 0, 0).AddHours(1);

        _nextRunTime = nextRun;
        UpdateCountdownLabel(DateTime.Now);
    }

    private void UpdateCountdownLabel(DateTime now)
    {
        _form.Invoke((Action)(() =>
        {
            if (_nextRunTime == DateTime.MinValue)
            {
                _form.lblTimer.Text = "00:00:00";
                return;
            }

            TimeSpan remaining = _nextRunTime - now;
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            _form.lblTimer.Text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }));
    }

    public async Task RunAutomationManually(CancellationToken token)
    {
        if (_isRunningAutomation) return;

        Stop();
        _isRunningAutomation = true;

        DateTime start = DateTime.Now;
        await RunAutomationAsync(token);
        DateTime end = DateTime.Now;

        // Schedule next run on the next rounded hour
        ScheduleNextRun(end);

        _isRunningAutomation = false;

        if (isTimerRunning)
            Start();
    }

    private async Task RunAutomationAsync(CancellationToken token)
    {
        await _form.RunAutomationAsync(token);
    }
}
