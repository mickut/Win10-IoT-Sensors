using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.I2c;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;
using System.Diagnostics;
using I2CSensors.Sensors;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SensorReaderSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer _timer;
        private Bh1750Fvi _luxMeter;
        private Hmc5883L _magnetometer;
        private Bmp180 _barometer;
        private Dsth01 _tempHum;


        public MainPage()
        {
            this.InitializeComponent();
        }
        
        #region illumination sensor event handler
        /// <summary>
        /// Simple illumination handler with autogain algorithm
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _AmbientLuxChanged(object sender, double e)
        {
            Debug.WriteLine("New measurement: " + e);
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => Lux.Text = $"{e:f2} lux");

            if (e < 3000 && _luxMeter.MeasurementTime < 250) // Increase accuracy
            {
                Debug.WriteLine("Very high resolution high-gain mode");
                _luxMeter.Mode = Bh1750Fvi.Resolution.VeryHigh;
                _luxMeter.MeasurementTime = 254;
            }
            else if ((e > 5000&& _luxMeter.MeasurementTime > 100) || (_luxMeter.MeasurementTime<40 && e<35000))
            {
                Debug.WriteLine("High resolution mid-gain mode");
                _luxMeter.Mode = Bh1750Fvi.Resolution.High;
                _luxMeter.MeasurementTime = 69;
            }
            else if (e > 50000 && _luxMeter.MeasurementTime > 40)
            {
                Debug.WriteLine("High resolution lo-gain mode");
                _luxMeter.Mode = Bh1750Fvi.Resolution.High;
                _luxMeter.MeasurementTime = 31;
            }
        }

        #endregion


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                _tempHum = new Dsth01(27); // GPIO27 connected to Dsth01 CS-pin
                _barometer = new Bmp180();
                _magnetometer = new Hmc5883L(22); // GPIO22 connected to Hmc5883L DRDY-pin
                _luxMeter = new Bh1750Fvi();
            }
            catch
            {
                Debug.WriteLine("Sensor initialization failed.");
                return;
            }

            await Task.WhenAll(_magnetometer.ConnectAsync(), _barometer.ConnectAsync(), _tempHum.ConnectAsync(), _luxMeter.ConnectAsync());

            // Set magnetometer gain an averaging
            if (_magnetometer.Connected)
            {
                await
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () => { Magnetometer.Visibility = Visibility.Visible; });
                _magnetometer.WriteRegister(Hmc5883L.Register.ConfA, (byte) Hmc5883L.ConfigA.Average8);
                _magnetometer.WriteRegister(Hmc5883L.Register.ConfB, (byte) Hmc5883L.ConfigB.Gain1370);
            }

            // The BH175FVI sensor supports a continuous measurement mode
            if (_luxMeter.Connected)
            {
                await
                    Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                        () => { Ambient.Visibility = Visibility.Visible; });
                _luxMeter.Mode = Bh1750Fvi.Resolution.VeryHigh;
                _luxMeter.ReadingChanged += _AmbientLuxChanged;
                _luxMeter.ContinuousPeriod = 2000; // every 2 seconds
                _luxMeter.ContinuousMeasurement = true;
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (_tempHum.Connected)
                    Humidity.Visibility = Visibility.Visible;

                if (_barometer.Connected)
                    Barometer.Visibility = Visibility.Visible;
            });

            // The rest of the sensors are polled periodically
            _timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(1000)};
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        /// <summary>
        /// Get measurements from sensors
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void _timer_Tick(object sender, object e)
        {
            
            try {
                if (_magnetometer.Connected)
                {
                    var m = await _magnetometer.GetReadingAsync();
                    Debug.WriteLine("Magnetometer: " + m);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        MagX.Text = $"{m.X:f2} uT";
                        MagY.Text = $"{m.Y:f2} uT";
                        MagZ.Text = $"{m.Z:f2} uT";
                    });
                }

                if (_barometer.Connected)
                {
                    // Use HW oversample for less noise
                    _barometer.OverSampling = 3;
                    var b = await _barometer.GetReadingAsync();
                    Debug.WriteLine("Barometer: " + b);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        BarPressure.Text = $"{b.Pressure:f2} hPa";
                        BarTemp.Text = $"{b.Temperature:f2}°C";
                    });
                }

                if (_tempHum.Connected)
                {
                    // Bmp180 humidity sensor has a heater we can turn on if humidity is very high.
                    // Heating reduces sensor wetting and stiction on high humidity conditions.
                    // It also causes a temp gradient within the sensor and affects our dewpoint calculations.
                    var th = await _tempHum.GetReadingAsync();
                    Debug.WriteLine("TempHum: " + th);
                    Debug.WriteLine("Dewpoint: " + th.DewPoint);
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        RhTemp.Text = $"{th.Temperature:f2}°C";
                        RhHum.Text = $"{th.Humidity:f0}%";
                        RhDew.Text = $"{th.DewPoint:f2}°C";
                    });

                    if (th.Humidity > 80)
                        _tempHum.Heater = true;
                    else if (th.Humidity < 70)
                        _tempHum.Heater = false;
                }
            }
            catch (Exception ex)
            {
                // TODO: deal with individual reading failures
                Debug.WriteLine("A measurement failed: "+ ex);
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            _timer.Stop();

            _tempHum?.Dispose();
            _tempHum = null;

            _barometer?.Dispose();
            _barometer = null;

            _luxMeter?.Dispose();
            _luxMeter = null;

            _magnetometer?.Dispose();
            _magnetometer = null;
        }
    }
}
