/*
 * DummyTimer - really dummy timer application... :D
 * Copyright (C) 2017  Federico Zanco [federico.zanco (at) gmail.com]
 * 
 * This file is part of DummyTimer.
 * 
 * DDummyTimer is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with DummyTimer; if not, If not, see <http://www.gnu.org/licenses/>.
 */

using DummyTimer.Properties;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Messages;
using ToastNotifications.Position;

namespace DummyTimer
{
    /// <summary>
    /// ViewModel
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    public class ViewModel : IdLabelValue, IDisposable
    {
        #region ILog
        private ILog _logger { get { return LogManager.GetLogger(GetType()); } }
        #endregion

        #region Constants
        private const double DefaultNotificationLifetime = 3;
        private const int DefaultMaximumNotificationCount = 5;
        private const double DefaultDispalyTimerIntervalMs = 10;

        private const int MaxDays = 30;
        private const int MaxHours = 23;
        private const int MaxMinutes = 59;
        private const int MaxSeconds = 59;
        private const int MaxMilliseconds = 999;
        #endregion

        #region Private fields
        private readonly List<IdLabelValue> _timeSpans = new List<IdLabelValue>
        {
            new IdLabelValue { Id = 0L, Label = "10 s [" + TimeSpan.FromSeconds(10.0) + "]", Value = TimeSpan.FromSeconds(10.0) },
            new IdLabelValue { Id = 1L, Label = "30 s [" + TimeSpan.FromSeconds(30.0) + "]", Value = TimeSpan.FromSeconds(30.0) },
            new IdLabelValue { Id = 2L, Label = "1 m [" + TimeSpan.FromMinutes(1.0) + "]", Value = TimeSpan.FromMinutes(1.0) },
            new IdLabelValue { Id = 3L, Label = "2 m [" + TimeSpan.FromMinutes(2.0) + "]", Value = TimeSpan.FromMinutes(2.0) },
            new IdLabelValue { Id = 4L, Label = "3 m [" + TimeSpan.FromMinutes(3.0) + "]", Value = TimeSpan.FromMinutes(3.0) },
            new IdLabelValue { Id = 5L, Label = "5 m [" + TimeSpan.FromMinutes(5.0) + "]", Value = TimeSpan.FromMinutes(5.0) },
            new IdLabelValue { Id = 6L, Label = "10 m [" + TimeSpan.FromMinutes(10.0) + "]", Value = TimeSpan.FromMinutes(10.0) },
            new IdLabelValue { Id = 7L, Label = "15 m [" + TimeSpan.FromMinutes(15.0) + "]", Value = TimeSpan.FromMinutes(15.0) },
            new IdLabelValue { Id = 8L, Label = "30 m [" + TimeSpan.FromMinutes(30.0) + "]", Value = TimeSpan.FromMinutes(30.0) },
            new IdLabelValue { Id = 9L, Label = "1 h [" + TimeSpan.FromHours(1.0) + "]", Value = TimeSpan.FromHours(1.0) },
            new IdLabelValue { Id = 10L, Label = "2 h [" + TimeSpan.FromHours(2.0) + "]", Value = TimeSpan.FromHours(2.0) },
            new IdLabelValue { Id = 11L, Label = "6 h [" + TimeSpan.FromHours(6.0) + "]", Value = TimeSpan.FromHours(6.0) },
            new IdLabelValue { Id = 12L, Label = "12 h [" + TimeSpan.FromHours(12.0) + "]", Value = TimeSpan.FromHours(12.0) },
            new IdLabelValue { Id = 13L, Label = "1 day [" + TimeSpan.FromDays(1.0) + "]", Value = TimeSpan.FromDays(1.0) },
            new IdLabelValue { Id = UndefinedId, Label = "Custom...", Value = null }
        };

        private IdLabelValue _selectedTimeSpan;

        private readonly List<IdLabelValue> _days = new List<IdLabelValue>();
        private readonly List<IdLabelValue> _hours = new List<IdLabelValue>();
        private readonly List<IdLabelValue> _minutes = new List<IdLabelValue>();
        private readonly List<IdLabelValue> _seconds = new List<IdLabelValue>();
        private readonly List<IdLabelValue> _milliseconds = new List<IdLabelValue>();

        private IdLabelValue _selectedCustomDay;
        private IdLabelValue _selectedCustomHour;
        private IdLabelValue _selectedCustomMinute;
        private IdLabelValue _selectedCustomSecond;
        private IdLabelValue _selectedCustomMillisecond;

        private DispatcherTimer _countdownTimer = new DispatcherTimer();
        private DispatcherTimer _displayTimer = new DispatcherTimer();

        private DateTime _startTime;

        private Notifier _notifier = new Notifier(cfg =>
        {
            cfg.PositionProvider = new PrimaryScreenPositionProvider(
                corner: Corner.BottomRight,
                offsetX: 10,
                offsetY: 100);

            cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                notificationLifetime: TimeSpan.FromSeconds(DefaultNotificationLifetime),
                maximumNotificationCount: MaximumNotificationCount.FromCount(DefaultMaximumNotificationCount));

            cfg.Dispatcher = Application.Current.Dispatcher;
        });

        private bool isMuted = false;
        #endregion

        #region Public properties
        public override string Label
        {
            get { return base.Label; }
            set
            {
                base.Label = value;

                Settings.Default["TextToShow"] = value;
                Settings.Default.Save();

                _logger.Info("saved text to show [" + value + "]");
            }
        }

        public string StartStopCommandLabel { get { return _countdownTimer.IsEnabled ? "Stop" : "Start"; } }

        public string CountdownLabel
        {
            get
            {
                return (_countdownTimer.Interval - (DateTime.UtcNow - _startTime)).ToString(@"hh\:mm\:ss\:fff");
            }
        }

        public List<IdLabelValue> TimeSpans
        {
            get { return _timeSpans; }
        }

        public IdLabelValue SelectedTimeSpan
        {
            get { return _selectedTimeSpan; }
            set
            {
                _selectedTimeSpan = value;

                UpdateCountdownTimer((TimeSpan)value.Value);

                Settings.Default["SelectedTimeSpanId"] = _selectedTimeSpan.Id;
                Settings.Default.Save();

                NotifyPropertyChanged(() => SelectedTimeSpan);
                NotifyPropertyChanged(() => IsCustomTimespanSelected);

                _logger.Info("selected timespan " + value);
            }
        }

        #region Custom
        public List<IdLabelValue> Days
        {
            get { return _days; }
        }

        public List<IdLabelValue> Hours
        {
            get { return _hours; }
        }

        public List<IdLabelValue> Minutes
        {
            get { return _minutes; }
        }

        public List<IdLabelValue> Seconds
        {
            get { return _seconds; }
        }

        public List<IdLabelValue> Milliseconds
        {
            get { return _milliseconds; }
        }

        public IdLabelValue SelectedCustomDay
        {
            get { return _selectedCustomDay; }
            set
            {
                _selectedCustomDay = value;

                Settings.Default["SelectedCustomDayId"] = _selectedCustomDay.Id.Value;
                Settings.Default.Save();

                _logger.Info("saved custom day " + value);

                UpdateCustomTimeSpan();

                NotifyPropertyChanged(() => SelectedCustomDay);
            }
        }

        public IdLabelValue SelectedCustomHour
        {
            get { return _selectedCustomHour; }
            set
            {
                _selectedCustomHour = value;

                Settings.Default["SelectedCustomHourId"] = _selectedCustomHour.Id.Value;
                Settings.Default.Save();

                _logger.Info("saved custom hour " + value);

                UpdateCustomTimeSpan();

                NotifyPropertyChanged(() => SelectedTimeSpan);
                NotifyPropertyChanged(() => SelectedCustomHour);
            }
        }

        public IdLabelValue SelectedCustomMinute
        {
            get { return _selectedCustomMinute; }
            set
            {
                _selectedCustomMinute = value;

                Settings.Default["SelectedCustomMinuteId"] = _selectedCustomMinute.Id.Value;
                Settings.Default.Save();

                _logger.Info("saved custom minute " + value);

                UpdateCustomTimeSpan();

                NotifyPropertyChanged(() => SelectedTimeSpan);
                NotifyPropertyChanged(() => SelectedCustomMinute);
            }
        }

        public IdLabelValue SelectedCustomSecond
        {
            get { return _selectedCustomSecond; }
            set
            {
                _selectedCustomSecond = value;

                Settings.Default["SelectedCustomSecondId"] = _selectedCustomSecond.Id.Value;
                Settings.Default.Save();

                _logger.Info("saved custom second " + value);

                UpdateCustomTimeSpan();

                NotifyPropertyChanged(() => SelectedTimeSpan);
                NotifyPropertyChanged(() => SelectedCustomSecond);
            }
        }

        public IdLabelValue SelectedCustomMillisecond
        {
            get { return _selectedCustomMillisecond; }
            set
            {
                _selectedCustomMillisecond = value;

                Settings.Default["SelectedCustomMillisecondId"] = _selectedCustomMillisecond.Id.Value;
                Settings.Default.Save();

                _logger.Info("saved custom millisecond " + value);

                UpdateCustomTimeSpan();

                NotifyPropertyChanged(() => SelectedTimeSpan);
                NotifyPropertyChanged(() => SelectedCustomMillisecond);
            }
        }

        public bool IsCustomTimespanSelected
        {
            get
            {
                return _selectedTimeSpan != null && _selectedTimeSpan.Id == UndefinedId;
            }
        }

        public bool IsCountdownTimerEnabled { get { return _countdownTimer.IsEnabled; } }
        #endregion

        public bool IsMuted
        {
            get { return isMuted; }
            set
            {
                isMuted = value;

                Settings.Default["IsMuted"] = value;
                Settings.Default.Save();

                _logger.Info("saved IsMuted [" + value + "]");

                NotifyPropertyChanged(() => IsMuted);
            }
        }
        #endregion

        #region Delegate commands
        /// <summary>
        /// Gets or sets the start stop command.
        /// </summary>
        /// <value>
        /// The start stop command.
        /// </value>
        public SimpleDelegateCommand StartStopCommand { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModel"/> class.
        /// </summary>
        public ViewModel()
        {
            _logger.Info("init...");

            StartStopCommand = new SimpleDelegateCommand(StartStopCommand_Execute);

            for (var d = 0; d <= MaxDays; d++)
                _days.Add(new IdLabelValue { Id = d, Label = d + " d", Value = d });

            for (var h = 0; h <= MaxHours; h++)
                _hours.Add(new IdLabelValue { Id = h, Label = h + " h", Value = h });

            for (var m = 0; m <= MaxMinutes; m++)
                _minutes.Add(new IdLabelValue { Id = m, Label = m + " m", Value = m });

            for (var s = 0; s <= MaxSeconds; s++)
                _seconds.Add(new IdLabelValue { Id = s, Label = s + " s", Value = s });

            // at least 1 ms
            for (var ms = 1; ms <= MaxMilliseconds; ms++)
                _milliseconds.Add(new IdLabelValue { Id = ms, Label = ms + " ms", Value = ms });

            _selectedTimeSpan = _timeSpans.FirstOrDefault(ts => ts.Id == (long)Settings.Default["SelectedTimeSpanId"]);

            _selectedCustomDay = _days.First(ts => ts.Id == (long)Settings.Default["SelectedCustomDayId"]);
            _selectedCustomHour = _hours.First(ts => ts.Id == (long)Settings.Default["SelectedCustomHourId"]);
            _selectedCustomMinute = _minutes.First(ts => ts.Id == (long)Settings.Default["SelectedCustomMinuteId"]);
            _selectedCustomSecond = _seconds.First(ts => ts.Id == (long)Settings.Default["SelectedCustomSecondId"]);
            _selectedCustomMillisecond = _milliseconds.First(ts => ts.Id == (long)Settings.Default["SelectedCustomMillisecondId"]);

            UpdateCustomTimeSpan();

            if (_selectedTimeSpan == null)
                SelectedTimeSpan = _timeSpans.First();

            _startTime = DateTime.UtcNow;
            _countdownTimer.Interval = (TimeSpan)_selectedTimeSpan.Value;
            _countdownTimer.IsEnabled = true;
            _countdownTimer.Tick += CountdownTimer_Tick;

            _displayTimer.Interval = TimeSpan.FromMilliseconds(DefaultDispalyTimerIntervalMs);
            _displayTimer.IsEnabled = true;
            _displayTimer.Tick += DisplayTimer_Tick;

            Label = (string)Settings.Default["TextToShow"];

            _logger.Info("init... DONE");
        }
        #endregion

        #region Private methods
        private void UpdateCountdownTimer(TimeSpan newInterval)
        {
            var remainingTime = _startTime.Add(_countdownTimer.Interval).Subtract(DateTime.UtcNow);

            if ((TimeSpan)_selectedTimeSpan.Value < remainingTime)
            {
                _startTime = DateTime.UtcNow;
                _countdownTimer.Interval = newInterval;
            }

            _logger.Debug(
                "START = " + _startTime.ToLongTimeString()
                + "\tNOW = " + DateTime.UtcNow.ToLongTimeString()
                + "\tTIMEOUT = " + _startTime.Add(_countdownTimer.Interval).ToLongTimeString()
                + "\tINTERVAL = " + _countdownTimer.Interval
                + "\tNEW INTERVAL = " + newInterval);
        }

        private void UpdateCustomTimeSpan()
        {
            var customTimeSpan = new TimeSpan((int)_selectedCustomDay.Value, (int)_selectedCustomHour.Value, (int)_selectedCustomMinute.Value, (int)_selectedCustomSecond.Value, (int)_selectedCustomMillisecond.Value);
            
            if (IsCountdownTimerEnabled)
            {
                UpdateCountdownTimer(customTimeSpan);

                NotifyPropertyChanged(() => SelectedTimeSpan);
            }

            _timeSpans.Last().Value = customTimeSpan;
            _timeSpans.Last().Label = "Custom... [" + customTimeSpan + "]";

            _logger.Info("updated custom timespan to [" + customTimeSpan + "]");
        }
        #endregion

        #region Timer_Tick
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _startTime = DateTime.UtcNow;

            if (_countdownTimer.Interval.TotalMilliseconds != ((TimeSpan)_selectedTimeSpan.Value).TotalMilliseconds)
                _countdownTimer.Interval = (TimeSpan)_selectedTimeSpan.Value;

            if (!isMuted)
                SystemSounds.Hand.Play();

            _notifier.ShowInformation(Label);
            _logger.Info("TIMEOUT! " + _selectedTimeSpan + " [" + Label + "]" );
        }

        private void DisplayTimer_Tick(object sender, EventArgs e)
        {
            NotifyPropertyChanged(() => CountdownLabel);
        }
        #endregion

        #region Commands
        #region StartStop
        private void StartStopCommand_Execute(object o)
        {
            _countdownTimer.IsEnabled = !_countdownTimer.IsEnabled;

            if (_countdownTimer.IsEnabled)
            {
                _startTime = DateTime.UtcNow;
                _displayTimer.IsEnabled = true;

                _logger.Info("START " + _selectedTimeSpan);
            }
            else
            {
                _displayTimer.IsEnabled = false;

                _logger.Info("STOP!");
            }

            NotifyPropertyChanged(() => IsCountdownTimerEnabled);
            NotifyPropertyChanged(() => StartStopCommandLabel);
        }
        #endregion
        #endregion

        #region IDisposable
        public void Dispose()
        {
            _notifier.Dispose();
        }
        #endregion
    }
}
