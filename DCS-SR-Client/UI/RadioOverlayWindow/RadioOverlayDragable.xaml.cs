﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Network;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Settings;
using Ciribob.DCS.SimpleRadio.Standalone.Client.Singletons;
using Ciribob.DCS.SimpleRadio.Standalone.Client.UI.AwacsRadioOverlayWindow;
using Ciribob.DCS.SimpleRadio.Standalone.Common;
using Dragablz;
using NLog;

namespace Ciribob.DCS.SimpleRadio.Standalone.Overlay
{
    /// <summary>
    ///     Interaction logic for RadioOverlayWindow.xaml
    /// </summary>
    public partial class RadioOverlayWindowDragable : Window
    {
        private readonly double _aspectRatio;
        
        private readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private long _lastUnitId;

        private readonly RadioControlGroup[] radioControlGroup = new RadioControlGroup[10];

        private readonly DispatcherTimer _updateTimer;

        public static bool AwacsActive = false
            ; //when false and we're in spectator mode / not in an aircraft the other 7 radios will be disabled

        private readonly ClientStateSingleton _clientStateSingleton = ClientStateSingleton.Instance;

        private GlobalSettingsStore _globalSettings = GlobalSettingsStore.Instance;

        private Action<bool, int> _toggleOverlay;

        public RadioOverlayWindowDragable(Action<bool, int> ToggleOverlay)
        {
            //load opacity before the intialising as the slider changed
            //method fires after initialisation
            //     var opacity = AppConfiguration.Instance.RadioOpacity;
            AwacsActive = true;

            InitializeComponent();

            this.WindowStartupLocation = WindowStartupLocation.Manual;

            _aspectRatio = MinWidth / MinHeight;

            AllowsTransparency = true;
            Opacity = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalOpacity).DoubleValue;
            windowOpacitySlider.Value = Opacity;

            Left = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalX).DoubleValue;
            Top = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalY).DoubleValue;

            Width = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalWidth).DoubleValue;
            Height = _globalSettings.GetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalHeight).DoubleValue;
            
            radioControlGroup[0] = radio1;
            radioControlGroup[1] = radio2;
            radioControlGroup[2] = radio3;
            radioControlGroup[3] = radio4;
            radioControlGroup[4] = radio5;
            radioControlGroup[5] = radio6;
            radioControlGroup[6] = radio7;
            radioControlGroup[7] = radio8;
            radioControlGroup[8] = radio9;
            radioControlGroup[9] = radio10;


            //allows click and drag anywhere on the window
            containerPanel.MouseLeftButtonDown += WrapPanel_MouseLeftButtonDown;

            CalculateScale();

            LocationChanged += Location_Changed;

            RadioRefresh(null, null);

            //init radio refresh
            _updateTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(80)};
            _updateTimer.Tick += RadioRefresh;
            _updateTimer.Start();

            this._toggleOverlay = ToggleOverlay;
        }

        private void Location_Changed(object sender, EventArgs e)
        {
            //   AppConfiguration.Instance.RadioX = Top;
            //  AppConfiguration.Instance.RadioY = Left;
        }

        private void RadioRefresh(object sender, EventArgs eventArgs)
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.DcsPlayerRadioInfo;

            foreach (var radio in radioControlGroup)
            {
                radio.RepaintRadioStatus();
                radio.RepaintRadioReceive();
            }

            intercom.RepaintRadioStatus();

            if ((dcsPlayerRadioInfo != null) && dcsPlayerRadioInfo.IsCurrent())
            {
                //reset when we switch planes
                if (_lastUnitId != dcsPlayerRadioInfo.unitId)
                {
                    _lastUnitId = dcsPlayerRadioInfo.unitId;
                }

                var availableRadios = 0;

                for (var i = 0; i < dcsPlayerRadioInfo.radios.Length; i++)
                {
                    if (dcsPlayerRadioInfo.radios[i].modulation != RadioInformation.Modulation.DISABLED)
                    {
                        availableRadios++;

                    }
                }

                if (availableRadios > 1)
                {
                    if (dcsPlayerRadioInfo.control == DCSPlayerRadioInfo.RadioSwitchControls.HOTAS)
                    {
                        ControlText.Text = "10 Radio Panel";
                    }
                    else
                    {
                        ControlText.Text = "10 Radio Panel";
                    }
                }
                else
                {
                    ControlText.Text = "10 Radio Panel (Disconnected)";

                }
            }
            else
            {
                ControlText.Text = "10 Radio Panel (Disconnected)";
            }
        }

        private void WrapPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalX, Left);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalY, Top);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalWidth, Width);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalHeight, Height);
            _globalSettings.SetPositionSetting(GlobalSettingsKeys.RadioTenHorizontalOpacity, Opacity);

            base.OnClosing(e);

            AwacsActive = false;
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


        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Button_Swap_Orientation(object sender, RoutedEventArgs e)
        {
            Close();
            _toggleOverlay(true, 3); // index 3 is the vertical orientation
        }

        private void windowOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Opacity = e.NewValue;
            //AppConfiguration.Instance.RadioOpacity = Opacity;
        }

        private void containerPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //force aspect ratio
            CalculateScale();

            WindowState = WindowState.Normal;
        }

//
//
        private void CalculateScale()
        {
            var yScale = ActualHeight / RadioOverlayWin.MinWidth;
            var xScale = ActualWidth / RadioOverlayWin.MinWidth;
            var value = Math.Max(xScale, yScale);
            ScaleValue = (double) OnCoerceScaleValue(RadioOverlayWin, value);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged)
                Width = sizeInfo.NewSize.Height * _aspectRatio;
            else
                Height = sizeInfo.NewSize.Width / _aspectRatio;

            //  AppConfiguration.Instance.RadioWidth = Width;
            // AppConfiguration.Instance.RadioHeight = Height;
            // Console.WriteLine(this.Height +" width:"+ this.Width);
        }

        #region ScaleValue Depdency Property //StackOverflow: http://stackoverflow.com/questions/3193339/tips-on-developing-resolution-independent-application/5000120#5000120

        public static readonly DependencyProperty ScaleValueProperty = DependencyProperty.Register("ScaleValue",
            typeof(double), typeof(RadioOverlayWindowDragable),
            new UIPropertyMetadata(1.0, OnScaleValueChanged,
                OnCoerceScaleValue));


        private static object OnCoerceScaleValue(DependencyObject o, object value)
        {
            var mainWindow = o as RadioOverlayWindowDragable;
            if (mainWindow != null)
                return mainWindow.OnCoerceScaleValue((double) value);
            return value;
        }

        private static void OnScaleValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var mainWindow = o as RadioOverlayWindowDragable;
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

        private void ToggleGlobalSimultaneousTransmissionButton_Click(object sender, RoutedEventArgs e)
        {
            var dcsPlayerRadioInfo = _clientStateSingleton.DcsPlayerRadioInfo;
            if (dcsPlayerRadioInfo != null)
            {
                dcsPlayerRadioInfo.simultaneousTransmission = !dcsPlayerRadioInfo.simultaneousTransmission;

                if (!dcsPlayerRadioInfo.simultaneousTransmission)
                {
                    foreach (var radio in dcsPlayerRadioInfo.radios)
                    {
                        radio.simul = false;
                    }
                }

                foreach (var radio in radioControlGroup)
                {
                    radio.RepaintRadioStatus();
                }
            }
        }

        static T GetParentOfType<T>(Visual visual) where T : Visual
        {
            DependencyObject parent = visual;
            do
            {
                parent = VisualTreeHelper.GetParent(parent);
            } while (parent != null && !(parent is T));

            return parent as T;
        }

        private void TabControl_OnDragEnter(object sender, DragEventArgs e)
        {
            // Just a sanity check - we need a Visual here
            if (!(e.OriginalSource is Visual v))
            {
                return;
            }

            // DragablzItems will represent our TabItems, so we search for those
            var item = GetParentOfType<DragablzItem>(v);

            // DragablzItem.Content should contain our original TabItem
            if (item != null && item.Content is TabItem ti)
            {
                ti.IsSelected = true;
            }
        }
    }
}