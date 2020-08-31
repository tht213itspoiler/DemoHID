using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
//using HidSharp;
using WpfApp1.HidLib;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, HidDevice> CurrentHIDs;
        HidDeviceData.ReadStatus LastReadStatus;

        public MainWindow()
        {
            InitializeComponent();

            CurrentHIDs = new Dictionary<string, HidDevice>();

            foreach (var item in ScanDevicesToList())
            {
                cboDevices.Items.Add(item);
            }

            btnSendData.IsEnabled = false;
            btnReadData.IsEnabled = false;         
        }

        #region Events Handler
        private void button_Click(object sender, RoutedEventArgs e)
        {
            LastReadStatus = HidDeviceData.ReadStatus.Success;
            if (string.IsNullOrEmpty(cboDevices.Text))
            {
                txtConsole.Text += Environment.NewLine + "Please choose device";
            }
            else
            {
                if (btnConnect.Content.ToString() == "Connect")
                {
                    if (ConnectHidDevie(cboDevices.Text))
                    {
                        btnConnect.Content = "Disconnect";
                        cboDevices.IsHitTestVisible = false;
                        cboDevices.Focusable = false;

                        btnSendData.IsEnabled = true;
                        btnReadData.IsEnabled = true;
                        btnRescan.IsEnabled = false;
                    }
                }
                else
                {
                    if (DisconnectHidDevie(cboDevices.Text))
                    {
                        btnConnect.Content = "Connect";
                        cboDevices.IsHitTestVisible = true;
                        cboDevices.Focusable = true;

                        btnSendData.IsEnabled = false;
                        btnReadData.IsEnabled = false;
                        btnRescan.IsEnabled = true;
                    }
                }
            }
        }

        private void btnSendData_Click(object sender, RoutedEventArgs e)
        {
            var chosenDev = CurrentHIDs[cboDevices.Text];
            var SendBytes = new List<byte>();

            if (string.IsNullOrEmpty(txtSendData.Text))
            {
                txtConsole.Text += Environment.NewLine + "Please insert data to send";
                return;
            }

            var sendString = "";

            if (chk.IsChecked == true)
            {
                if (IsHexStringFormat(txtSendData.Text.Trim()))
                {
                    sendString = "00-" + txtSendData.Text.Trim();

                   
                        SendBytes.AddRange(FromHexStringToDecList(sendString));
                    
                }
                else
                {
                    txtConsole.Text += Environment.NewLine + "Hex string is incorrect format";
                    return;
                }
            }    
            else
            {
                sendString = "0" + txtSendData.Text.Trim();
                SendBytes.AddRange(Encoding.ASCII.GetBytes(sendString.Trim()).ToList());
            }

            if (LastReadStatus != HidDeviceData.ReadStatus.Success)
            {
                chosenDev.CloseDevice();
            }

            if (chosenDev.Write(SendBytes.ToArray(), 500))
            {
                txtConsole.Text += Environment.NewLine + "Write successful";
            }
            else
            {
                txtConsole.Text += Environment.NewLine + "Write fail";
            }
        }

        private void btnReadData_Click(object sender, RoutedEventArgs e)
        {
            var chosenDev = CurrentHIDs[cboDevices.Text];
            var reading = chosenDev.Read(100);
            LastReadStatus = reading.Status;

            if (reading.Status == HidDeviceData.ReadStatus.Success)
            {
                txtConsole.Text += Environment.NewLine + "Read successful";

                var dataList = reading.Data.ToList();
                dataList.RemoveAt(0);

                if (chk.IsChecked == true)
                {
                    txtReadData.Text = BitConverter.ToString(dataList.ToArray());
                }
                else
                {
                    txtReadData.Text = Encoding.ASCII.GetString(dataList.ToArray(), 0, dataList.Count);
                }
            }
            else if (reading.Status == HidDeviceData.ReadStatus.NoDataRead)
            {
                txtConsole.Text += Environment.NewLine + "No data to read";
            }
        }

        private void btnRescan_Click(object sender, RoutedEventArgs e)
        {
            cboDevices.Items.Clear();

            foreach (var item in ScanDevicesToList())
            {
                cboDevices.Items.Add(item);
            }
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            txtSendData.Text = ConvertASCIItoHEX(txtSendData.Text);
            txtReadData.Text = ConvertASCIItoHEX(txtReadData.Text);
        }

        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            txtSendData.Text = "";
            txtReadData.Text = "";
        } 

        #endregion

        string GetHidInfoDisplay(HidDevice dev)
        {
            var aaa = dev.ReadProduct(out var data);
            var output = new List<byte>();

            foreach (var bt in data)
            {
                if (bt != 0)
                {
                    output.Add(bt);
                }
            }

            return $"{Encoding.ASCII.GetString(output.ToArray(), 0, output.Count)} - VID:{dev.Attributes.VendorId}, PID:{dev.Attributes.ProductId}, UsagePage:{dev.Capabilities.UsagePage}, Usage:{dev.Capabilities.Usage}";
        }

        #region HID Device communications
        List<string> ScanDevicesToList()
        {
            var output = new List<string>();
            CurrentHIDs.Clear();

            foreach (var item in HidDevices.Enumerate())
            {
                try
                {
                    var str = GetHidInfoDisplay(item);
                    CurrentHIDs.Add(str, item);
                    output.Add(str);
                }
                catch { }
            }

            return output;
        }

        bool ConnectHidDevie(string devPath)
        {
            var lyly = CurrentHIDs[devPath];
            lyly.OpenDevice();

            if (lyly.IsOpen)
            {
                txtConsole.Text += Environment.NewLine + $"Connect to {devPath} succesful";
                return true;
            }
            else
            {
                txtConsole.Text += Environment.NewLine + $"Connect to {devPath} fail";
                return false;
            }
        }

        bool DisconnectHidDevie(string devPath)
        {
            var lyly = CurrentHIDs[devPath];
            lyly.CloseDevice();

            if (!lyly.IsOpen)
            {
                txtConsole.Text += Environment.NewLine + $"Disconnect to {devPath} succesful";
                return true;
            }
            else
            {
                txtConsole.Text += Environment.NewLine + $"Disconnect to {devPath} fail";
                return false;
            }
        }

        #endregion

        #region extension

        string ConvertASCIItoHEX(string input)
        {
            var hexBytes = Encoding.Default.GetBytes(input);

            var hexStr = BitConverter.ToString(hexBytes);
            
            return hexStr;
        }

        List<byte> FromHexStringToDecList(string hexString)
        {
            var decVals = new List<byte>();

            foreach (var hex in hexString.Split('-'))
            {
                var decVal = Convert.ToByte(hex, 16);
                decVals.Add(decVal);
            }

            return decVals;
        }

        bool IsHexStringFormat(string hexstr)
        {
            return Regex.IsMatch(hexstr, "^[A-z0-9]{2}(-[A-z0-9]{2}){0,31}$");
        }

        #endregion

        private void txtConsole_IsVisibleChanged(object sender, TextChangedEventArgs e)
        {
            txtConsole.SelectionStart = txtConsole.Text.Length;
            txtConsole.ScrollToEnd();
        }

       
    }


}
