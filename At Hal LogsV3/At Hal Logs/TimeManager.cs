using At_Hal_Logs;
using AutomationHoistinger;

internal class TimeManager
{
    private readonly AppWindow _form;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly TimeSpan _baseInterval = TimeSpan.FromMinutes(Globals.BASE_RUNTIME_INTERVAL);
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
        ScheduleNextRun(DateTime.Now, _baseInterval);
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
        ScheduleNextRun(DateTime.Now, _baseInterval);
    }

    private async void Timer_Tick(object sender, EventArgs e)
    {
        if (_isRunningAutomation) return;

        DateTime now = DateTime.Now;

        if (_nextRunTime != DateTime.MinValue && now >= _nextRunTime)
        {
            _isRunningAutomation = true;
            DateTime start = DateTime.Now;

            await RunAutomationAsync();

            DateTime end = DateTime.Now;
            TimeSpan runtime = end - start;

            TimeSpan adjustedInterval;

            if (runtime > TimeSpan.FromMinutes(5))
            {
                adjustedInterval = TimeSpan.FromHours(1) - (runtime + TimeSpan.FromMinutes(2));

                // fail safe  makeing sure it works
                if (adjustedInterval < TimeSpan.FromMinutes(5))
                    adjustedInterval = TimeSpan.FromMinutes(5);
            }
            else
            {
                adjustedInterval = _baseInterval;
            }

            ScheduleNextRun(end, adjustedInterval);
            _isRunningAutomation = false;
        }

        UpdateCountdownLabel(now);
    }

    private void ScheduleNextRun(DateTime fromTime, TimeSpan interval)
    {
        _nextRunTime = fromTime.Add(interval);
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

    public async Task RunAutomationManually()
    {
        if (_isRunningAutomation) return;

        Stop();
        _isRunningAutomation = true;

        DateTime start = DateTime.Now;
        await RunAutomationAsync();
        DateTime end = DateTime.Now;

        TimeSpan runtime = end - start;
        TimeSpan adjustedInterval;

        if (runtime > TimeSpan.FromMinutes(5))
        {
            adjustedInterval = TimeSpan.FromHours(1) - (runtime + TimeSpan.FromMinutes(Globals.BASE_RUNTIME_OFFSET));
            if (adjustedInterval < TimeSpan.FromMinutes(5))
                adjustedInterval = TimeSpan.FromMinutes(5);
        }
        else
        {
            adjustedInterval = _baseInterval;
        }
        adjustedInterval = TimeSpan.FromMinutes(55);
        ScheduleNextRun(end, adjustedInterval);
        _isRunningAutomation = false;

        if (isTimerRunning)
            Start();
    }

    private async Task RunAutomationAsync()
    {
        await _form.RunAutomationAsync();
    }
}
