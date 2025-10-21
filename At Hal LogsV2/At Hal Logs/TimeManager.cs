using At_Hal_Logs;
using AutomationHoistinger;

internal class TimeManager
{
    private readonly AppWindow _form;
    private readonly System.Windows.Forms.Timer _timer;
    private TimeSpan _interval = TimeSpan.FromHours(1);
    private bool _isRunningAutomation = false;
    public bool isTimerRunning;
    private DateTime _lastRunTime = DateTime.MinValue;

    public TimeManager(AppWindow form)
    {
        _form = form;
        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = 1000; // check every second
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        UpdateCountdownLabel(DateTime.Now);
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
        _lastRunTime = DateTime.MinValue;
        UpdateCountdownLabel(DateTime.Now);
    }

    private async void Timer_Tick(object sender, EventArgs e)
    {
        if (_isRunningAutomation) return;

        DateTime now = DateTime.Now;

        bool shouldRun = ShouldTrigger(now);

        if (shouldRun)
        {
            _isRunningAutomation = true;
            _lastRunTime = now;

            await RunAutomationAsync();

            _isRunningAutomation = false;
        }

        UpdateCountdownLabel(now);
    }

    public void SetInterval(TimeSpan newInterval)
    {
        _timer.Stop();
        _interval = newInterval;
        _lastRunTime = DateTime.MinValue; // reset tracking
        UpdateCountdownLabel(DateTime.Now);

        if (isTimerRunning)
            _timer.Start();
    }

    private void UpdateCountdownLabel(DateTime now)
    {
        _form.Invoke((Action)(() =>
        {
            DateTime nextTrigger = GetNextTriggerTime(now);
            TimeSpan remaining = nextTrigger - now;

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

        await RunAutomationAsync();

        _isRunningAutomation = false;

        if (isTimerRunning)
        {
            UpdateCountdownLabel(DateTime.Now);
            Start();
        }
    }

    private async Task RunAutomationAsync()
    {
        await _form.RunAutomationAsync();
    }

    private bool ShouldTrigger(DateTime now)
    {
        //If its the first run, skip until we reach a rounded hour
        if (_lastRunTime == DateTime.MinValue)
        {
            //Run immediately if the current time has already passed a rounded point
            if (IsRoundedTime(now))
                return true;
            return false;
        }

        //Prevent multiple triggers
        if (now - _lastRunTime < TimeSpan.FromSeconds(1))
            return false;

        return IsRoundedTime(now);
    }

    private bool IsRoundedTime(DateTime now)
    {
        if (_interval == TimeSpan.FromHours(1))
            return now.Minute == 0 && now.Second == 0;

        if (_interval == TimeSpan.FromHours(6))
            return now.Minute == 0 && now.Second == 0 && now.Hour % 6 == 0;

        if (_interval == TimeSpan.FromHours(24))
            return now.Hour == 0 && now.Minute == 0 && now.Second == 0;

        if (_interval == TimeSpan.FromDays(7))
            return now.DayOfWeek == DayOfWeek.Monday && now.Hour == 0 && now.Minute == 0 && now.Second == 0;

        return false;
    }

    private DateTime GetNextTriggerTime(DateTime now)
    {
        if (_interval == TimeSpan.FromHours(1))
            return now.Date.AddHours(now.Hour + 1);

        if (_interval == TimeSpan.FromHours(6))
            return now.Date.AddHours(((now.Hour / 6) + 1) * 6);

        if (_interval == TimeSpan.FromHours(24))
            return now.Date.AddDays(1);

        if (_interval == TimeSpan.FromDays(7))
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)now.DayOfWeek + 7) % 7;
            return now.Date.AddDays(daysUntilMonday).AddDays(daysUntilMonday == 0 ? 7 : 0);
        }

        return now.Add(_interval);
    }
}
