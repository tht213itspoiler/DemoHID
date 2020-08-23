﻿using System;
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
using HidLibrary;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, HidDevice> CurrentHIDs;

        public MainWindow()
        {
            InitializeComponent();

            CurrentHIDs = new Dictionary<string, HidDevice>();

            foreach (var item in ScanDevicesToList())
            {
                cboDevices.Items.Add(item);
            }
        }

        #region Events Handler
        private void button_Click(object sender, RoutedEventArgs e)
        {
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
                    }
                }
                else
                {
                    if (DisconnectHidDevie(cboDevices.Text))
                    {
                        btnConnect.Content = "Connect";
                        cboDevices.IsHitTestVisible = true;
                        cboDevices.Focusable = true;
                    }
                }
            }
        }

        private void btnSendData_Click(object sender, RoutedEventArgs e)
        {
            var chosenDev = CurrentHIDs[cboDevices.Text];

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
                    sendString = FromHexStringToASCII(sendString);
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
            }

            var SendBytes = new List<byte>();

            SendBytes.AddRange(Encoding.ASCII.GetBytes(sendString.Trim()).ToList());

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
            var reading = chosenDev.Read(10);

            if (reading.Status == HidDeviceData.ReadStatus.Success)
            {
                txtConsole.Text += Environment.NewLine + "Read successful";

                var output = new List<byte>();

                foreach (var bt in reading.Data)
                {
                    if (bt != 0)
                    {
                        output.Add(bt);
                    }
                }

                txtReadData.Text += Environment.NewLine + Encoding.ASCII.GetString(output.ToArray(), 0, output.Count);
            }
        }

        private void CheckBoxChecked(object sender, RoutedEventArgs e)
        {
            txtSendData.Text = ConvertASCIItoHEX(txtSendData.Text);
        }

        private void CheckBoxUnchecked(object sender, RoutedEventArgs e)
        {
            txtSendData.Text = FromHexStringToASCII(txtSendData.Text);
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

        string FromHexStringToASCII(string hexString)
        {
            // initialize the ASCII code string as empty. 
            var ascii = "";

            hexString = hexString.Replace("-", "");

            for (int i = 0; i < hexString.Length; i += 2)
            {
                // extract two characters from hex string 
                var part = hexString.Substring(i, 2);

                // change it into base 16 and  
                // typecast as the character 
                char ch = (char)Convert.ToInt32(part, 16); ;

                // add this char to final ASCII string 
                ascii = ascii + ch;
            }

            return ascii;
        }

        bool IsHexStringFormat(string hexstr)
        {
            return Regex.IsMatch(hexstr, "^[A-z0-9]{2}(-[A-z0-9]{2}){0,31}$");
        }

        #endregion

        
    }


}