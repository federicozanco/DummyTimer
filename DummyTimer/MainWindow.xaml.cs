/*
 * DummyTimer - really dummy timer application... :D
 * Copyright (C) 2017  Federico Zanco [federico.zanco (at) gmail.com]
 * 
 * This file is part of DummyTimer.
 * 
 * DummyTimer is free software; you can redistribute it and/or modify
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

using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using log4net.Config;

namespace DummyTimer
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon =
                new Icon(
                    Application.GetResourceStream(
                        new Uri("pack://application:,,,/DummyTimer;component/timer.ico")).Stream),
            Visible = true
        };

        public MainWindow()
        {
            XmlConfigurator.Configure();

            InitializeComponent();

            var viewModel = (ViewModel)DataContext;

            Icon = Imaging.CreateBitmapSourceFromHIcon(
                new Icon(
                    Application.GetResourceStream(new Uri("pack://application:,,,/DummyTimer;component/timer.ico")).Stream)
                    .Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            _trayIcon.Click +=
                delegate (object sender, EventArgs args)
                {
                    if (IsVisible)
                        WindowState = WindowState.Minimized;
                    else
                    {
                        Show();
                        WindowState = WindowState.Normal;
                    }
                };

            _trayIcon.MouseMove += TrayIcon_MouseMove;

            ShowInTaskbar = false;
            Hide();
        }

        private void TrayIcon_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            _trayIcon.Text = ((ViewModel)DataContext).CountdownLabel;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                ShowInTaskbar = false;
                Hide();
            }
            else
                ShowInTaskbar = true;

            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _trayIcon.Dispose();
            ((ViewModel)DataContext).Dispose();

            base.OnClosing(e);
        }
    }
}
