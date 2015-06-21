using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Monitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int TimePeriod = 1000;
        private SerialPort mySerialPort;
        public delegate void AddDataDelegate(String myString);
        public AddDataDelegate MyDelegate;
        private int CountRecorded;
        private int TotalValue;
        private Stopwatch Timer;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                mySerialPort = new SerialPort("COM3")
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
            //LogTextBlock.AppendText(data);
            //var records = data.Split('|');
            var values = new List<int>();
            //foreach (var record in records)
            //{
                int dataValue;
                if (Int32.TryParse(data, out dataValue))
                {
                    values.Add(dataValue);
                }
                else
                {
                    LogTextBlock.AppendText(data);
                }
            //}

            if (values.Count > 0)
            {
                foreach (var value in values)
                {
                    if (SaveRecordedValue(value))
                    {
                        LogTextBlock.AppendText((TotalValue/CountRecorded) + Environment.NewLine);
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
