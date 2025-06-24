using System;
using System.Text;
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
using Sharp7;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ClientDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        S7Client Client;
        byte[] Buffer = new byte[65536];

        public MainPage()
        {
            this.InitializeComponent();
            Client = new S7Client();
            BrowseMode();
        }

        private void BrowseMode()
        {
            ConnectBtn.IsEnabled = true;
            TxtIP.IsEnabled = true;
            TxtRack.IsEnabled = true;
            TxtSlot.IsEnabled = true;
            DisconnectBtn.IsEnabled = false;
            TxtDB.IsEnabled = false;
            TxtStart.IsEnabled = false;
            TxtSize.IsEnabled = false;
            ReadBtn.IsEnabled = false;
            TxtDump.IsEnabled = false;
        }

        private void RunMode()
        {
            ConnectBtn.IsEnabled = false;
            TxtIP.IsEnabled = false;
            TxtRack.IsEnabled = false;
            TxtSlot.IsEnabled = false;
            DisconnectBtn.IsEnabled = true;
            TxtDB.IsEnabled = true;
            TxtStart.IsEnabled = true;
            TxtSize.IsEnabled = true;
            ReadBtn.IsEnabled = true;
            TxtDump.IsEnabled = true;
        }

        private void HexDump(byte[] bytes, int Size)
        {
            if (bytes == null)
                return;
            int bytesLength = Size;
            int bytesPerLine = 16;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - 2) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            TxtDump.Text = result.ToString();
        }

        private void ShowResult(int Result)
        {
            // This function returns a textual explaination of the error code
            TxtResult.Text = Client.ErrorText(Result);
        }

        private void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            int Result = Client.ConnectTo(TxtIP.Text, Convert.ToInt32(TxtRack.Text), Convert.ToInt32(TxtSlot.Text));
            ShowResult(Result);
            if (Result == 0)
                RunMode();
        }

        private void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            int Result = Client.Disconnect();
            ShowResult(Result);
            BrowseMode();
        }

        private void ReadBtn_Click(object sender, RoutedEventArgs e)
        {
            int Size = Convert.ToInt32(TxtSize.Text);
            int Result = Client.DBRead(Convert.ToInt32(TxtDB.Text), Convert.ToInt32(TxtStart.Text), Size, Buffer);
            ShowResult(Result);
            if (Result == 0)
                HexDump(Buffer, Size);
        }

    }
}
