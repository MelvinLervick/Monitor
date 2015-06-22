using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TimePeriod = 1000;
        private string ArduinoPort;
        private string AnalogData;
        private bool ScreenIO;
        private SerialPort mySerialPort;
        public delegate void AddDataDelegate(String myString);
        public AddDataDelegate MyDelegate;
        private int CountRecorded;
        private int TotalValue;
        private Stopwatch Timer;

        public MainWindow()
        {
            InitializeComponent();

            ArduinoPort = ConfigurationManager.AppSettings["ArduinoPort"];
            AnalogData = ConfigurationManager.AppSettings["AnalogDataFile"];
            ScreenIO = Convert.ToBoolean(ConfigurationManager.AppSettings["ScreenIO"]);

            try
            {
                mySerialPort = new SerialPort(ArduinoPort)
                {
                    BaudRate = 9600,
                    DtrEnable = true
                };

                mySerialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedEventHandler);
            }
            catch (Exception ex)
            {
                LogTextBlock.AppendText(string.Format("{0}\r\n", ex.ToString()));
            }

            this.MyDelegate = new AddDataDelegate(AddDataMethod);
            CountRecorded = 0;
            TotalValue = 0;
            Timer = new Stopwatch();
        }

        private void Menu_FileExitClick(object sender, RoutedEventArgs e)
        {
            mySerialPort.Close();
            var app = Application.Current;
            app.Shutdown();
        }

        private void Menu_OpenClick(object sender, RoutedEventArgs e)
        {
            LogTextBlock.AppendText("Start Recording\r\n");
            mySerialPort.Open();
            Thread.Sleep(3000);
        }

        private void Menu_StartClick(object sender, RoutedEventArgs e)
        {
            mySerialPort.Write("S");
        }

        public void AddDataMethod(string data)
        {
            var values = new List<int>();

            int dataValue;
            if (Int32.TryParse(data, out dataValue))
            {
                values.Add(dataValue);
            }
            else
            {
                LogTextBlock.AppendText(data);
            }

            if (values.Count > 0)
            {
                foreach (var value in values)
                {
                    if (SaveRecordedValue(value))
                    {
                        var outputValue = string.Format("{0}", (TotalValue/CountRecorded) + Environment.NewLine);

                        using (var w = new StreamWriter(AnalogData))
                        {
                            w.Write(outputValue);
                            w.Flush();
                        }

                        if(ScreenIO) LogTextBlock.AppendText(outputValue);
                        TotalValue = 0;
                        CountRecorded = 0;
                    }
                }
            }
        }

        public bool SaveRecordedValue(int data)
        {
            if (CountRecorded == 0)
            {
                Timer.Restart();
                CountRecorded++;
                TotalValue += data;
            }
            else
            {
                CountRecorded++;
                TotalValue += data;
                if (Timer.ElapsedMilliseconds > TimePeriod)
                {
                    return true;
                }
            }

            return false;
        }

        private void DataReceivedEventHandler(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var sp = (SerialPort)sender;

                var data = mySerialPort.ReadLine();

                this.Dispatcher.BeginInvoke(MyDelegate, data);
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(MyDelegate, ex);
            }
        }

        private void Menu_PauseClick(object sender, RoutedEventArgs e)
        {
            mySerialPort.Write("B");
        }

        private void Menu_CloseClick(object sender, RoutedEventArgs e)
        {
            LogTextBlock.AppendText("Stop Recording\r\n");
            mySerialPort.Close();
        }
    }
}
