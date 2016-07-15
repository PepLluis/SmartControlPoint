/// :: IOT Core Labs:: AYNIT 22/04/2016
/// Smart Control Point
/// Hackster.io - WorkShop

// Imports
#region
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endregion

namespace UniversalSerialPort
{
    public sealed partial class MainPage : Page
    {
        SerialDevice            serialPort         = null;
        DataWriter              dataOutputStream   = null;
        DataReader              dataInputStream    = null;
        CancellationTokenSource readTaskCK;
        uint                    readFrameLenght     = 5;

        public MainPage()
        {
            this.InitializeComponent();
            readTaskCK = new CancellationTokenSource();
            serialPort_Open();
        }

        // 
        //       void         serialPort_Close()
        // async Task<string> serialPort_GetFirst()
        // async void         serialPort_Open()
        // async Task         serialPort_Read(CancellationToken cancellationToken)
        // async Task         serialPort_Settings(string deviceId)
        // async void         serialPort_StartReading()
        // async Task         serialPort_Write()
        //
        #region Serial Port Functions
        /// <summary>
        /// Close Serial Port
        /// </summary>
        void serialPort_Close()
        {
            try
            {
                status.Text = "";
                CancelReadTask();
                if (serialPort != null)
                {
                    serialPort.Dispose();
                }
                serialPort = null;
                sendTextButton.IsEnabled = false;
                rcvdText.Text = "";
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }
        /// <summary>
        /// Get First Serial Port
        /// </summary>
        /// <returns></returns>
        async Task<string> serialPort_GetFirst()
        {
            string devicesFound = SerialDevice.GetDeviceSelector();
            var serialDevices = await DeviceInformation.FindAllAsync(devicesFound);
            if (serialDevices.Count == 0) throw new Exception("No serial ports availables");
            // return first one
            return serialDevices[0].Id;
        }
        /// <summary>
        /// Open Serial Port
        /// </summary>
        async void serialPort_Open()
        {
            try
            {
                await serialPort_Settings(await serialPort_GetFirst());
                serialPort_StartReading();
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                sendTextButton.IsEnabled = false;
            }
        }
        /// <summary>
        /// Serial Port Read
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task serialPort_Read(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;
            cancellationToken.ThrowIfCancellationRequested();
            dataInputStream.InputStreamOptions = InputStreamOptions.Partial;
            loadAsyncTask = dataInputStream.LoadAsync(readFrameLenght).AsTask(cancellationToken);
            UInt32 bytesRead = await loadAsyncTask;
            if (bytesRead > 0) { rcvdText.Text = dataInputStream.ReadString(bytesRead); }
        }        
        /// <summary>
        /// Set Serial Port Settings
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        async Task serialPort_Settings(string deviceId)
        {
            serialPort = await SerialDevice.FromIdAsync(deviceId);
            // Configure serial settings
            serialPort.BaudRate = 9600;
            serialPort.Parity = SerialParity.None;
            serialPort.StopBits = SerialStopBitCount.One;
            serialPort.DataBits = 8;
            serialPort.WriteTimeout = TimeSpan.FromMilliseconds(500);
            serialPort.ReadTimeout = TimeSpan.FromMilliseconds(500);
            pageTitle.Text += serialPort.PortName.ToString();
        }
        /// <summary>
        /// Starting Continuous Reading
        /// </summary>
        async void serialPort_StartReading()
        {
            try
            {
                if (serialPort != null)
                {
                    dataInputStream = new DataReader(serialPort.InputStream);
                    while (true)
                    {
                        await serialPort_Read(readTaskCK.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                var a = ex.InnerException.Message;
                serialPort_Close();
                status.Text = ex.Message;
            }
            finally
            {
                if (dataInputStream != null)
                {
                    dataInputStream.DetachStream();
                    dataInputStream = null;
                }
            }
        }
        /// <summary>
        /// Send Data to Serial Port
        /// </summary>
        /// <returns></returns>
        async Task serialPort_Write()
        {
            Task<UInt32> sendStreamTask;
            if (sendText.Text.Length != 0)
            {
                dataOutputStream.WriteString(sendText.Text + "\n");
                sendStreamTask = dataOutputStream.StoreAsync().AsTask();
                UInt32 bytesWritten = await sendStreamTask;
                if (bytesWritten > 0)
                {
                    sendText.Text = "";
                }
            }
        }

        #endregion

        // async void sendButton_Click(object sender, RoutedEventArgs e)
        #region User Interface Controls
        /// <summary>
        /// When send button is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void sendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (serialPort != null)
                {
                    dataOutputStream = new DataWriter(serialPort.OutputStream);
                    await serialPort_Write();
                }
                else
                {
                    status.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                status.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                if (dataOutputStream != null)
                {
                    dataOutputStream.DetachStream();
                    dataOutputStream = null;
                }
            }
        }
        #endregion

        // void CancelReadTask()
        #region Miscellaneous 
        /// <summary>
        /// Set Cancellation token to Cancel Read Task
        /// </summary>
        void CancelReadTask()
        {
            if (readTaskCK != null)
            {
                if (!readTaskCK.IsCancellationRequested)
                {
                    readTaskCK.Cancel();
                }
            }
        }
        #endregion
    }
}
