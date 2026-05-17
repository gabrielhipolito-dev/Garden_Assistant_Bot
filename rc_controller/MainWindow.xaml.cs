using System.Collections.Concurrent;
using System.Globalization;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace rc_controller
{
    public partial class MainWindow : Window
    {
        private const char FrameStart = '<';
        private const char FrameEnd = '>';
        private const int MaxBufferSize = 4096;
        private const int SensorReadTimeoutMs = 1500;
        private const int SensorReadRetries = 2;
        private const int MaxBadFrameThreshold = 3;

        private readonly object _serialLock = new();
        private readonly object _bufferLock = new();
        private readonly StringBuilder _incomingBuffer = new();
        private readonly ConcurrentDictionary<SensorType, TaskCompletionSource<SensorFrame>> _pendingSensorRequests = new();
        private readonly Dictionary<SensorType, double> _lastValidValues = new();

        private SerialPort? _serialPort;
        private int _corruptedFrameCount;


        private enum SensorType
        {
            Voltage,
            Moisture,
            Humidity,
            Temperature
        }

        private readonly record struct SensorFrame(SensorType Type, double Value, int Sequence);

        public MainWindow()
        {
            InitializeComponent();
            // focus on window to allow keypress
            this.Focusable = true;
            this.KeyDown += Window_KeyDown;
            this.KeyUp += Window_KeyUp;
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Environment.GetEnvironmentVariable("GAB_SIMULATE_SERIAL") == "1")
            {
                SimulateCorruptedFrames();
            }
        }


        /* ------------- SERIAL CONNECTION ------------- */
        bool isConnected = false;
        bool disconnect = false;
        private void InitializeSerialPort(string comport)
        {

            if (comport != "--Select COM port--")
            {
                try
                {
                    if (comport == "COM4"){
                        MessageBox.Show($"Operation failed: {comport}");
                        return; }
                    if (!isConnected)
                    {
                        _serialPort = new SerialPort
                        {
                            PortName = comport,
                            BaudRate = 9600,
                            Parity = Parity.None,
                            DataBits = 8,
                            StopBits = StopBits.One,
                            NewLine = "\n",
                            Encoding = Encoding.ASCII,
                            ReadTimeout = 500,
                            WriteTimeout = 500
                        };

                        _serialPort.DataReceived += SerialPortOnDataReceived;
                        _serialPort.Open();

                        MessageBox.Show("Serial port opened successfully!");
                        isConnected = true;
                        click_button.Background = new SolidColorBrush(Colors.Red);
                        //baguhin mo lance yun link sa image 
                        disconnect_image.Source = new BitmapImage(new Uri("../img/minus.png", UriKind.RelativeOrAbsolute));
                        UpdateSensorStatus("Sensor status: OK", Brushes.LimeGreen);
                    }
                    else
                    {
                        string pop_up = "Do you want to disconnect";
                        MessageBoxResult result = MessageBox.Show(
                        pop_up,                     
                        "Confirm Disconnection",      
                        MessageBoxButton.YesNo,       
                        MessageBoxImage.Question
                        );

                        if (result == MessageBoxResult.Yes)
                        {
                            DisconnectSerialPort();
                            MessageBox.Show("Serial port disconnected successfully!");
                            click_button.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00979C"));
                            disconnect_image.Source = new BitmapImage(new Uri("../img/connect.png", UriKind.RelativeOrAbsolute));
                            isConnected = false;
                        }
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Operation failed: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Serial port not yet selected!");
            }
        }

        private void DisconnectSerialPort()
        {
            if (_serialPort == null)
            {
                return;
            }

            try
            {
                _serialPort.DataReceived -= SerialPortOnDataReceived;
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }

            CancelPendingRequests();
            lock (_bufferLock)
            {
                _incomingBuffer.Clear();
            }
            _corruptedFrameCount = 0;
            UpdateSensorStatus("Sensor status: disconnected", Brushes.Gray);
        }


        /* ------------- SERIAL READ/WRITE COMMANDS ------------- */
        private bool IsSerialReady => _serialPort != null && _serialPort.IsOpen;

        private bool TrySendCommand(string data)
        {
            if (!IsSerialReady)
            {
                MessageBox.Show("Serial port not yet connected!");
                return false;
            }

            try
            {
                lock (_serialLock)
                {
                    _serialPort!.WriteLine(data);
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send command to serial port: {ex.Message}");
                return false;
            }
        }

        private async Task RequestSensorAsync(SensorType type, string command)
        {
            if (!IsSerialReady)
            {
                MessageBox.Show("Serial port not yet connected!");
                return;
            }

            for (int attempt = 0; attempt <= SensorReadRetries; attempt++)
            {
                var tcs = new TaskCompletionSource<SensorFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingSensorRequests.AddOrUpdate(type, tcs, (_, _) => tcs);

                if (!TrySendCommand(command))
                {
                    _pendingSensorRequests.TryRemove(type, out _);
                    return;
                }

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(SensorReadTimeoutMs));
                if (completed == tcs.Task)
                {
                    return;
                }

                _pendingSensorRequests.TryRemove(type, out _);
            }

            RegisterTimeout(type);
        }

        private void SerialPortOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!IsSerialReady)
            {
                return;
            }

            string data;
            lock (_serialLock)
            {
                data = _serialPort!.ReadExisting();
            }

            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            AppendIncomingData(data);
        }

        private void AppendIncomingData(string data)
        {
            var frames = new List<SensorFrame>();
            string? debugLine = null;
            int corruptedFrames;

            lock (_bufferLock)
            {
                _incomingBuffer.Append(data);
                if (_incomingBuffer.Length > MaxBufferSize)
                {
                    _incomingBuffer.Remove(0, _incomingBuffer.Length - MaxBufferSize);
                }

                ExtractFramesAndLines(_incomingBuffer, frames, ref debugLine, out corruptedFrames);
            }

            if (!string.IsNullOrWhiteSpace(debugLine))
            {
                UpdateDebugText(debugLine);
            }

            foreach (var frame in frames)
            {
                HandleSensorFrame(frame);
            }

            if (corruptedFrames > 0)
            {
                RegisterCorruptedFrames(corruptedFrames);
            }
        }

        private static void ExtractFramesAndLines(StringBuilder buffer, List<SensorFrame> frames, ref string? debugLine, out int corruptedFrames)
        {
            corruptedFrames = 0;
            while (true)
            {
                var current = buffer.ToString();
                var startIndex = current.IndexOf(FrameStart);

                if (startIndex < 0)
                {
                    debugLine = GetLastLine(current) ?? debugLine;
                    buffer.Clear();
                    return;
                }

                if (startIndex > 0)
                {
                    debugLine = GetLastLine(current.Substring(0, startIndex)) ?? debugLine;
                    buffer.Remove(0, startIndex);
                    current = buffer.ToString();
                }

                var endIndex = current.IndexOf(FrameEnd, 1);
                if (endIndex < 0)
                {
                    return;
                }

                var frameContent = current.Substring(1, endIndex - 1);
                if (TryParseFrame(frameContent, out var frame))
                {
                    frames.Add(frame);
                }
                else
                {
                    corruptedFrames++;
                }

                buffer.Remove(0, endIndex + 1);
            }
        }

        private static string? GetLastLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return null;
            }

            var lastLine = lines[^1].Trim();
            return string.IsNullOrWhiteSpace(lastLine) ? null : lastLine;
        }

        private static bool TryParseFrame(string frame, out SensorFrame sensorFrame)
        {
            sensorFrame = default;
            var segments = frame.Split('|');
            if (segments.Length != 4)
            {
                return false;
            }

            if (!string.Equals(segments[0], "S", StringComparison.Ordinal))
            {
                return false;
            }

            if (!int.TryParse(segments[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sequence))
            {
                return false;
            }

            var keyValue = segments[2].Split('=', 2);
            if (keyValue.Length != 2)
            {
                return false;
            }

            if (!TryGetSensorType(keyValue[0], out var type))
            {
                return false;
            }

            if (!double.TryParse(keyValue[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return false;
            }

            if (!IsValueInRange(type, value))
            {
                return false;
            }

            if (!byte.TryParse(segments[3], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var expectedChecksum))
            {
                return false;
            }

            var payload = $"{segments[0]}|{segments[1]}|{segments[2]}";
            var actualChecksum = ComputeChecksum(payload);
            if (expectedChecksum != actualChecksum)
            {
                return false;
            }

            sensorFrame = new SensorFrame(type, value, sequence);
            return true;
        }

        private static bool TryGetSensorType(string key, out SensorType type)
        {
            switch (key)
            {
                case "V":
                    type = SensorType.Voltage;
                    return true;
                case "M":
                    type = SensorType.Moisture;
                    return true;
                case "H":
                    type = SensorType.Humidity;
                    return true;
                case "T":
                    type = SensorType.Temperature;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        private static bool IsValueInRange(SensorType type, double value)
        {
            return type switch
            {
                SensorType.Voltage => value >= 0 && value <= 20,
                SensorType.Moisture => value >= 0 && value <= 100,
                SensorType.Humidity => value >= 0 && value <= 100,
                SensorType.Temperature => value >= -40 && value <= 85,
                _ => false
            };
        }

        private static byte ComputeChecksum(string payload)
        {
            byte checksum = 0;
            foreach (var ch in payload)
            {
                checksum ^= (byte)ch;
            }
            return checksum;
        }

        private void HandleSensorFrame(SensorFrame frame)
        {
            _lastValidValues[frame.Type] = frame.Value;
            _corruptedFrameCount = 0;
            UpdateSensorStatus("Sensor status: OK", Brushes.LimeGreen);
            UpdateSensorValue(frame.Type, frame.Value);

            if (_pendingSensorRequests.TryRemove(frame.Type, out var tcs))
            {
                tcs.TrySetResult(frame);
            }
        }

        private void RegisterCorruptedFrames(int count)
        {
            _corruptedFrameCount += count;
            if (_corruptedFrameCount >= MaxBadFrameThreshold)
            {
                UpdateSensorStatus("Sensor data corrupted", Brushes.Red);
            }
            else
            {
                UpdateSensorStatus("Sensor data unstable", Brushes.Orange);
            }
        }

        private void RegisterTimeout(SensorType type)
        {
            _corruptedFrameCount++;
            UpdateSensorStatus($"Sensor timeout: {type}", Brushes.OrangeRed);
            if (_lastValidValues.TryGetValue(type, out var lastValue))
            {
                UpdateSensorValue(type, lastValue);
            }
            if (_corruptedFrameCount >= MaxBadFrameThreshold)
            {
                UpdateSensorStatus("Sensor data corrupted", Brushes.Red);
            }
        }

        private void CancelPendingRequests()
        {
            foreach (var pending in _pendingSensorRequests)
            {
                pending.Value.TrySetCanceled();
            }
            _pendingSensorRequests.Clear();
        }

        private void UpdateDebugText(string text)
        {
            Dispatcher.BeginInvoke(() => DebugTextBox.Text = text);
        }

        private void UpdateSensorStatus(string message, Brush color)
        {
            Dispatcher.BeginInvoke(() =>
            {
                SensorStatusTextBox.Text = message;
                SensorStatusTextBox.Foreground = color;
            });
        }

        private void UpdateSensorValue(SensorType type, double value)
        {
            var formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
            Dispatcher.BeginInvoke(() =>
            {
                switch (type)
                {
                    case SensorType.Voltage:
                        VoltageTextBox.Text = $"{formatted} V";
                        VoltageColor(value);
                        break;
                    case SensorType.Moisture:
                        MoistureTextBox.Text = $"{formatted}%";
                        SoilColor(value);
                        break;
                    case SensorType.Humidity:
                        HumidityTextBox.Text = $"{formatted}%";
                        HumColor(value);
                        break;
                    case SensorType.Temperature:
                        TemperatureTextBox.Text = $"{formatted} C";
                        TempColor(value);
                        break;
                }
            });
        }

        private void SimulateCorruptedFrames()
        {
            var valid = BuildSensorFrame(SensorType.Moisture, 45.5, 1);
            var invalid = "<S|999|M=BAD|00>";
            AppendIncomingData($"noise\n{invalid}\n{valid}\n");
        }

        private static string BuildSensorFrame(SensorType type, double value, int sequence)
        {
            var key = type switch
            {
                SensorType.Voltage => "V",
                SensorType.Moisture => "M",
                SensorType.Humidity => "H",
                SensorType.Temperature => "T",
                _ => "U"
            };

            var payload = $"S|{sequence}|{key}={value.ToString("0.##", CultureInfo.InvariantCulture)}";
            var checksum = ComputeChecksum(payload);
            return $"<{payload}|{checksum:X2}>";
        }

        /* ------------- TEXTBOX COLOR CHANGER ------------- */
        private void VoltageColor(double v)
        {
            if (v < 7.0)
            {
                VoltageTextBox.Foreground = Brushes.Red;
            }
            else if (v < 7.4)
            {
                VoltageTextBox.Foreground = Brushes.Yellow;
            }
            else
            {
                VoltageTextBox.Foreground = Brushes.LimeGreen;
            }
        }
        private void SoilColor(double s)
        {
            if (s < 40)
            {
                MoistureTextBox.Foreground = Brushes.Red;
            }
            else
            {
                MoistureTextBox.Foreground = Brushes.RoyalBlue;
            }
        }

        private void HumColor(double h)
        {
            if (h < 30)
            {
                HumidityTextBox.Foreground = Brushes.Red;
            }
            else
            {
                HumidityTextBox.Foreground = Brushes.RoyalBlue;
            }
        }

        private void TempColor(double t)
        {
            if (t < 30)
            {
                TemperatureTextBox.Foreground = Brushes.RoyalBlue;
            }
            else if (t < 35)
            {
                TemperatureTextBox.Foreground = Brushes.Orange;
            }
            else
            {
                TemperatureTextBox.Foreground = Brushes.Red;
            }
        }


        /* ------------- BUTTON PRESS/RELEASE EVENTS ------------- */
        // Establish Serial Connection
        private void CONNECT_BUTTON(object sender, RoutedEventArgs e) => InitializeSerialPort(ComPortComboBox.Text);

        // Directional Movements
        private void ButtonForward_Pressed(object sender, RoutedEventArgs e) => TrySendCommand("w");
        private void ButtonForward_Released(object sender, RoutedEventArgs e) => TrySendCommand("x");
        private void ButtonBackward_Pressed(object sender, RoutedEventArgs e) => TrySendCommand("s");
        private void ButtonBackward_Released(object sender, RoutedEventArgs e) => TrySendCommand("x");
        private void ButtonLeft_Pressed(object sender, RoutedEventArgs e) => TrySendCommand("a");
        private void ButtonLeft_Released(object sender, RoutedEventArgs e) => TrySendCommand("x");
        private void ButtonRight_Pressed(object sender, RoutedEventArgs e) => TrySendCommand("d");
        private void ButtonRight_Released(object sender, RoutedEventArgs e) => TrySendCommand("x");

        // Motor Speed
        private void MotorMinus(object sender, RoutedEventArgs e) => TrySendCommand("z");
        private void MotorPlus(object sender, RoutedEventArgs e) => TrySendCommand("c");

        // Servo 1 Control
        private void S1minus_press(object sender, RoutedEventArgs e) => TrySendCommand("t");
        private void S1minus_Released(object sender, RoutedEventArgs e) => TrySendCommand("p");
        private void S1plus_press(object sender, RoutedEventArgs e) => TrySendCommand("g");
        private void S1plus_Released(object sender, RoutedEventArgs e) => TrySendCommand("p");

        // Servo 2 Control
        private void S2minus_press(object sender, RoutedEventArgs e) => TrySendCommand("y");
        private void S2minus_Released(object sender, RoutedEventArgs e) => TrySendCommand("p");
        private void S2plus_press(object sender, RoutedEventArgs e) => TrySendCommand("h");
        private void S2plus_Released(object sender, RoutedEventArgs e) => TrySendCommand("p");

        // Servo 3 Control
        private void S3minus_press(object sender, RoutedEventArgs e) => TrySendCommand("u");
        private void S3minus_Released(object sender, RoutedEventArgs e) => TrySendCommand("p");
        private void S3plus_press(object sender, RoutedEventArgs e) => TrySendCommand("i");
        private void S3plus_Released(object sender, RoutedEventArgs e) => TrySendCommand("p");

        // Sensor Readings
        private async void VOLTAGE_BUTTON(object sender, RoutedEventArgs e) => await RequestSensorAsync(SensorType.Voltage, "v");
        private async void HUMIDITY_BUTTON(object sender, RoutedEventArgs e) => await RequestSensorAsync(SensorType.Humidity, "n");
        private async void MOISTURE_BUTTON(object sender, RoutedEventArgs e) => await RequestSensorAsync(SensorType.Moisture, "b");
        private async void TEMPERATURE_BUTTON(object sender, RoutedEventArgs e) => await RequestSensorAsync(SensorType.Temperature, "m");


        /* ------------- KEYBOARD HANDLING ------------- */
        // Button Press
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W: ButtonForward_Pressed(sender, new RoutedEventArgs()); break;
                case Key.S: ButtonBackward_Pressed(sender, new RoutedEventArgs()); break;
                case Key.A: ButtonLeft_Pressed(sender, new RoutedEventArgs()); break;
                case Key.D: ButtonRight_Pressed(sender, new RoutedEventArgs()); break;
                case Key.Z: MotorMinus(sender, new RoutedEventArgs()); break;
                case Key.C: MotorPlus(sender, new RoutedEventArgs()); break;
                case Key.V: VOLTAGE_BUTTON(sender, new RoutedEventArgs()); break;
                case Key.B: MOISTURE_BUTTON(sender, new RoutedEventArgs()); break;
                case Key.N: HUMIDITY_BUTTON(sender, new RoutedEventArgs()); break;
                case Key.M: TEMPERATURE_BUTTON(sender, new RoutedEventArgs()); break;
                case Key.T: S1minus_press(sender, new RoutedEventArgs()); break;
                case Key.G: S1plus_press(sender, new RoutedEventArgs()); break;
                case Key.Y: S2minus_press(sender, new RoutedEventArgs()); break;
                case Key.H: S2plus_press(sender, new RoutedEventArgs()); break;
                case Key.U: S3minus_press(sender, new RoutedEventArgs()); break;
                case Key.I: S3plus_press(sender, new RoutedEventArgs()); break;
            }
        }

        // Button Release
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.W: ButtonForward_Released(sender, new RoutedEventArgs()); break;
                case Key.S: ButtonBackward_Released(sender, new RoutedEventArgs()); break;
                case Key.A: ButtonLeft_Released(sender, new RoutedEventArgs()); break;
                case Key.D: ButtonRight_Released(sender, new RoutedEventArgs()); break;
                case Key.T: S1minus_Released(sender, new RoutedEventArgs()); break;
                case Key.G: S1plus_Released(sender, new RoutedEventArgs()); break;
                case Key.Y: S2minus_Released(sender, new RoutedEventArgs()); break;
                case Key.H: S2plus_Released(sender, new RoutedEventArgs()); break;
                case Key.U: S3minus_Released(sender, new RoutedEventArgs()); break;
                case Key.I: S3plus_Released(sender, new RoutedEventArgs()); break;
            }
        }

        private void HumidityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        /* ------------- END OF CODE ------------- */
    }
}
