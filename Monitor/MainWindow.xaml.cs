using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

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
        }

        private void Menu_StartClick(object sender, RoutedEventArgs e)
        {
            mySerialPort.Write("S");
        }

        public void AddDataMethod(string data)
        {
            if (SaveRecordedValue(data))
            {
                LogTextBlock.AppendText((TotalValue/CountRecorded).ToString());
                TotalValue = 0;
                CountRecorded = 0;
            }
        }

        private bool SaveRecordedValue(string data)
        {
            int dataToSave = Convert.ToInt32(data);
            if (CountRecorded == 0)
            {
                Timer.Restart();
                CountRecorded++;
                TotalValue += dataToSave;
            }
            else
            {
                CountRecorded++;
                TotalValue += dataToSave;
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

                var data = mySerialPort.ReadExisting();

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
