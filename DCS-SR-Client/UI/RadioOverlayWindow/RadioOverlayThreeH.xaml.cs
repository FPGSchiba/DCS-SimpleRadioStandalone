﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Ciribob.DCS.SimpleRadio.Standalone.Client;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.RadioOverlayWindow;
using NLog;
using Ciribob.DCS.SimpleRadio.Standalone.Common;
using System.Windows.Forms;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.AwacsRadioOverlayWindow;
using MessageBox = System.Windows.Forms.MessageBox;

namespace Ciribob.DCS.SimpleRadio.Standalone.Overlay
{
    /// <summary>
    ///     Interaction logic for RadioOverlayWindow.xaml
    /// </summary>
    public partial class RadioOverlayWindowThreeH : Window
    {
        private  double _aspectRatio;
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Client.UI.AwacsRadioOverlayWindow.RadioControlGroup[] radioControlGroup =
            new Client.UI.AwacsRadioOverlayWindow.RadioControlGroup[3];

        private readonly DispatcherTimer _updateTimer;

        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        private readonly GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;

        private readonly double _originalMinHeight;

        private readonly double _radioHeight;

        private long _lastUnitId;


        public RadioOverlayWindowThreeH()
        {
            //load opacity before the intialising as the slider changed
            //method fires after initialisation
            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;

            _aspectRatio = MinWidth / MinHeight;

            _originalMinHeight = MinHeight;
            _radioHeight = Radio1.Height;
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(MainWindow.GetWindow(this));
            Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            MaxHeight = screen.Bounds.Height;

            AllowsTransparency = true;
            Opacity = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioThreeHOpacity).DoubleValue;
            WindowOpacitySlider.Value = Opacity;

            radioControlGroup[0] = Radio1;
            radioControlGroup[1] = Radio2;
            radioControlGroup[2] = Radio3;

            //allows click and drag anywhere on the window
            ContainerPanel.MouseLeftButtonDown += WrapPanel_MouseLeftButtonDown;

            Left = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioThreeHX).DoubleValue;
            Top = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioThreeHY).DoubleValue;

            Width = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioThreeHWidth).DoubleValue;
            Height = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioThreeHHeight).DoubleValue;

            //  Window_Loaded(null, null);
            CalculateScale();

            LocationChanged += Location_Changed;

            RadioRefresh(null, null);

            //init radio refresh
            _updateTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(80)};
            _updateTimer.Tick += RadioRefresh;
            _updateTimer.Start();
        }

        private void Location_Changed(object sender, EventArgs e)
        {
        }

        private void RadioRefresh(object sender, EventArgs eventArgs)
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.DcsPlayerRadioInfo;

            foreach (var radio in radioControlGroup)
            {
                radio.RepaintRadioStatus();
                radio.RepaintRadioReceive();
            }

            Intercom.RepaintRadioStatus();

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent())
            {
                //reset when we switch planes
                if (_lastUnitId != dcsPlayerRadioInfo.unitId)
                {
                    _lastUnitId = dcsPlayerRadioInfo.unitId;
                    ResetHeight();
                }

                var availableRadios = 0;

                for (var i = 0; i < dcsPlayerRadioInfo.radios.Length; i++)
                {
                    if (dcsPlayerRadioInfo.radios[i].modulation != RadioInformation.Modulation.DISABLED)
                    {
                        availableRadios++;

                    }
                }

                if (availableRadios == 3
                         || dcsPlayerRadioInfo.radios.Length >= 3
                         && dcsPlayerRadioInfo.radios[1].modulation != RadioInformation.Modulation.DISABLED)
                {
                    if (MinHeight != _originalMinHeight)
                    {
                        MinHeight = _originalMinHeight;
                        Recalculate();
                    }
                }
                else
                {
                    ResetHeight();
                }


                if (availableRadios > 1)
                {
                    if (dcsPlayerRadioInfo.control == DCSPlayerRadioInfo.RadioSwitchControls.HOTAS)
                    {
                        ControlText.Text = "Three Radio Panel Horizontal";
                    }
                    else
                    {
                        ControlText.Text = "Three Radio Panel Horizontal";
                    }
                }
                else
                {
                    ControlText.Text = "Three Radio Panel Horizontal (Disconnected)";
                    
                }
            }
            else
            {
                ResetHeight();
                ControlText.Text = "Three Radio Panel Horizontal (Disconnected)";
            }

            FocusDCS();
        }

        private void ResetHeight()
        {

            if (MinHeight != _originalMinHeight)
            {
                MinHeight = _originalMinHeight;
                Recalculate();
            }
        }

        private void Recalculate()
        {
            _aspectRatio = MinWidth / MinHeight;
            containerPanel_SizeChanged(null, null);
            Height = Height+1;
        }

        private long _lastFocus;
        private RadioCapabilities _radioCapabilitiesWindow;

        private void FocusDCS()
        {
            if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RefocusDCS))
            {
                var overlayWindow = new WindowInteropHelper(this).Handle;

                //focus DCS if needed
                var foreGround = WindowHelper.GetForegroundWindow();

                Process[] localByName = Process.GetProcessesByName("dcs");

                if (localByName != null && localByName.Length > 0)
                {
                    //either DCS is in focus OR Overlay window is not in focus
                    if (foreGround == localByName[0].MainWindowHandle || overlayWindow != foreGround ||
                        this.IsMouseOver)
                    {
                        _lastFocus = DateTime.Now.Ticks;
                    }
                    else if (DateTime.Now.Ticks > _lastFocus + 20000000 && overlayWindow == foreGround)
                    {
                        WindowHelper.BringProcessToFront(localByName[0]);
                    }
                }
            }
        }

        private void WrapPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioThreeHWidth, Width);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioThreeHHeight,Height);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioThreeHOpacity,Opacity);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioThreeHX,Left);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioThreeHY, Top);
            base.OnClosing(e);

            _updateTimer.Stop();
        }

        private void Button_Minimise(object sender, RoutedEventArgs e)
        {
            // Minimising a window without a taskbar icon leads to the window's menu bar still showing up in the bottom of screen
            // Since controls are unusable, but a very small portion of the always-on-top window still showing, we're closing it instead, similar to toggling the overlay
            if (_globalSettings.GetClientSettingBool(GlobalSettingsKeys.RadioOverlayTaskbarHide))
            {
                Close();
            }
            else
            {
                WindowState = WindowState.Minimized;
            }
        }

        private void Button_About(object sender, RoutedEventArgs e)
        {
            //Show Radio Capabilities
            if ((_radioCapabilitiesWindow == null) || !_radioCapabilitiesWindow.IsVisible ||
                (_radioCapabilitiesWindow.WindowState == WindowState.Minimized))
            {
                _radioCapabilitiesWindow?.Close();

                _radioCapabilitiesWindow = new RadioCapabilities();
                _radioCapabilitiesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                _radioCapabilitiesWindow.Owner = this;
                _radioCapabilitiesWindow.ShowDialog();
            }
            else
            {
                _radioCapabilitiesWindow?.Close();
                _radioCapabilitiesWindow = null;
            }

        }


        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void windowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Opacity = e.NewValue;
        }

        private void containerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //force aspect ratio
            CalculateScale();

            WindowState = WindowState.Normal;
        }


        private void CalculateScale()
        {
            var yScale = ActualHeight / RadioOverlayWin.MinWidth;
            var xScale = ActualWidth / RadioOverlayWin.MinWidth;
            var value = Math.Min(xScale, yScale);
            ScaleValue = (double) OnCoerceScaleValue(RadioOverlayWin, value);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
                Width = sizeInfo.NewSize.Height * _aspectRatio;
            else
                Height = sizeInfo.NewSize.Width / _aspectRatio;


            // Console.WriteLine(this.Height +" width:"+ this.Width);
        }

        #region ScaleValue Depdency Property //StackOverflow: http://stackoverflow.com/questions/3193339/tips-on-developing-resolution-independent-application/5000120#5000120

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue",
            typeof(double), typeof(RadioOverlayWindowThreeH),
            new UIPropertyMetadata(1.0, OnScaleValueChanged,
                OnCoerceScaleValue));

        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            var mainWindow = o as RadioOverlayWindowThreeH;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double) value);
            return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var mainWindow = o as RadioOverlayWindowThreeH;
            if (mainWindow != null)
                mainWindow.OnScaleValueChanged((double) e.OldValue, (double) e.NewValue);
        }

        protected virtual double OnCoerceScaleValue(double value)
        {
            if (double.IsNaN(value))
                return 1.0f;

            value = Math.Max(0.1, value);
            return value;
        }

        protected virtual void OnScaleValueChanged(double oldValue, double newValue)
        {
        }

        public double ScaleValue
        {
            get { return (double) GetValue(ScaleValueProperty); }
            set { SetValue(ScaleValueProperty, value); }
        }

        #endregion

        private void RadioOverlayWindow_OnLocationChanged(object sender, EventArgs e)
        {
            //reset last focus so we dont switch back to dcs while dragging
            _lastFocus = DateTime.Now.Ticks;
        }
    }
}