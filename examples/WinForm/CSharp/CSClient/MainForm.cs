using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Sharp7;

namespace CSClient
{

    public partial class MainForm : Form
    {
        private S7Client Client;
        private byte[] Buffer = new byte[65536];
        private byte[] DB_A = new byte[1024];
        private byte[] DB_B = new byte[1024];
        private byte[] DB_C = new byte[1024];

        private void ShowResult(int Result)
        {
            // This function returns a textual explaination of the error code
            TextError.Text = Client.ErrorText(Result);
            if (Result == 0)
                TextError.Text = TextError.Text + " (" + Client.ExecutionTime.ToString() + " ms)";
        }

        public MainForm()
        {
            InitializeComponent();
            Client = new S7Client();
            if (IntPtr.Size == 4)
                this.Text = this.Text + " - Running 32 bit Code";
            else
                this.Text = this.Text + " - Running 64 bit Code";

            CBType.SelectedIndex  = 0;
            CBArea.SelectedIndex  = 3;
            CBWLen.SelectedIndex  = 1;
            CBBlock.SelectedIndex = 1;
            tabControl.Enabled = false;
        }

        private void ConnectBtn_Click(object sender, EventArgs e)
        {
            int Result;
            int Rack = System.Convert.ToInt32(TxtRack.Text);
            int Slot = System.Convert.ToInt32(TxtSlot.Text);
            Result = Client.ConnectTo(TxtIP.Text, Rack, Slot);
            ShowResult(Result);
            if (Result == 0)
            {
                TextError.Text = TextError.Text + " PDU Negotiated : " + Client.PduSizeNegotiated.ToString();
                TxtIP.Enabled = false;
                TxtRack.Enabled = false;
                TxtSlot.Enabled = false;
                ConnectBtn.Enabled = false;
                DisconnectBtn.Enabled = true;
                tabControl.Enabled = true;
            }
        }

        private void DisconnectBtn_Click(object sender, EventArgs e)
        {
            Client.Disconnect();
            TextError.Text = "Disconnected";
            TxtIP.Enabled = true;
            TxtRack.Enabled = true;
            TxtSlot.Enabled = true;
            ConnectBtn.Enabled = true;
            DisconnectBtn.Enabled = false;
            tabControl.Enabled = false;
        }

        private void HexDump(TextBox DumpBox, byte[] bytes, int Size)
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
            DumpBox.Text=result.ToString();
        }

        private void ReadArea()
        {
            // Declaration separated from the code for readability
            int DBNumber;
            int Amount;
            int SizeRead = 0;
            int Result;
            int[] Area = 
            { 
                 S7Consts.S7AreaPE,
                 S7Consts.S7AreaPA,
                 S7Consts.S7AreaMK,
                 S7Consts.S7AreaDB,
                 S7Consts.S7AreaCT,
                 S7Consts.S7AreaTM
            };
            int[] WordLen =
            {
                 S7Consts.S7WLBit,
                 S7Consts.S7WLByte,
                 S7Consts.S7WLChar,
                 S7Consts.S7WLWord,
                 S7Consts.S7WLInt,
                 S7Consts.S7WLDWord,
                 S7Consts.S7WLDInt,
                 S7Consts.S7WLReal,
                 S7Consts.S7WLCounter,
                 S7Consts.S7WLTimer
            };

            label18.Text = "0";
            TxtDump.Text = "";

            DBNumber = System.Convert.ToInt32(TxtDB.Text);
            Amount = System.Convert.ToInt32(TxtSize.Text);
            Result = Client.ReadArea(Area[CBArea.SelectedIndex], DBNumber, 0, Amount, WordLen[CBWLen.SelectedIndex], Buffer, ref SizeRead);
        
            ShowResult(Result);
            label18.Text = SizeRead.ToString();
            if (Result == 0)
                HexDump(TxtDump, Buffer, SizeRead);            
        }

        public void DBMultiRead()
        {
            // Multi Reader Instance
            S7MultiVar Reader = new S7MultiVar(Client);

            TxtRes_A.Text = "";
            TxtRes_B.Text = "";
            TxtRes_C.Text = "";

            int DBNumber_A = System.Convert.ToInt32(TxtDB_A.Text);
            int DBNumber_B = System.Convert.ToInt32(TxtDB_B.Text);
            int DBNumber_C = System.Convert.ToInt32(TxtDB_C.Text);

            // Add Items def.
            Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_A, 0, 16, ref DB_A);
            Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_B, 0, 16, ref DB_B);
            Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_C, 0, 16, ref DB_C);
            // Performs the Read
            int Result = Reader.Read();

            // Dumps the data and shows the results
            ShowResult(Result);


            TxtRes_A.Text = Client.ErrorText(Reader.Results[0]);
            if (Reader.Results[0] == 0)
                HexDump(TxtDump_A, DB_A, 16);
            else
                TxtDump_A.Text = "< No Data Available >";

            TxtRes_B.Text = Client.ErrorText(Reader.Results[1]);
            if (Reader.Results[1] == 0)
                HexDump(TxtDump_B, DB_B, 16);
            else
                TxtDump_B.Text = "< No Data Available >";

            TxtRes_C.Text = Client.ErrorText(Reader.Results[2]);
            if (Reader.Results[2] == 0)
                HexDump(TxtDump_C, DB_C, 16);
            else
                TxtDump_C.Text = "< No Data Available >";

        }
        
        void DBMultiWrite()
        {
            for (int c = 0; c < 16; c++)
            {
                DB_A[c] = 0x01;
            }
            for (int c = 0; c < 16; c++)
            {
                DB_B[c] = 0x02;
            }

            // Multi Writer Instance
            S7MultiVar Writer = new S7MultiVar(Client);

            TxtRes_A.Text = "";
            TxtRes_B.Text = "";
            TxtRes_C.Text = "";
            TxtDump_A.Text = "";
            TxtDump_B.Text = "";
            TxtDump_C.Text = "";

            int DBNumber_A = Convert.ToInt32(TxtDB_A.Text);
            int DBNumber_B = Convert.ToInt32(TxtDB_B.Text);
            int DBNumber_C = Convert.ToInt32(TxtDB_C.Text);

            // Add Items def.
            
            Writer.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_A, 0, 16, ref DB_A);
            Writer.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_B, 0, 16, ref DB_B);
            Writer.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_C, 0, 16, ref DB_C);
            
            
            // Performs the Write
            int Result = Writer.Write();
            // Shows the results
            ShowResult(Result);

            TxtRes_A.Text = Client.ErrorText(Writer.Results[0]);
            TxtRes_B.Text = Client.ErrorText(Writer.Results[1]);
            TxtRes_C.Text = Client.ErrorText(Writer.Results[2]);
        }

        void ReadSZL()
        {
            S7Client.S7SZL SZL = new S7Client.S7SZL();
            int Size = 4096; // Declare our Buffer Size
            SZL.Data = new byte[Size];

            label29.Text = "0";
            TxtSZL.Text = "";

            int Result = Client.ReadSZL(Convert.ToInt32(TxtSZLID.Text), Convert.ToInt32(TxtSZLIndex.Text), ref SZL, ref Size);
            ShowResult(Result);
            if (Result == 0)
            {
                label29.Text = Size.ToString();
                HexDump(TxtSZL, SZL.Data, Size);
            }
        }

        private void PlcDBWrite()
        {
            // Declaration separated from the code for readability
            int DBNumber;
            int Size;
            int Result;

            DBNumber = System.Convert.ToInt32(TxtDB.Text);
            Size = System.Convert.ToInt32(TxtSize.Text);
            Result = Client.DBWrite(DBNumber, 0, Size, Buffer);
            ShowResult(Result);
        }

        void ReadCPUInfo()
        {
            S7Client.S7CpuInfo Info = new S7Client.S7CpuInfo();
            txtModuleTypeName.Text = "";
            txtSerialNumber.Text = "";
            txtCopyright.Text = "";
            txtAsName.Text = "";
            txtModuleName.Text = "";
            int Result = Client.GetCpuInfo(ref Info);
            ShowResult(Result);
            if (Result==0)
            {
                txtModuleTypeName.Text = Info.ModuleTypeName;
                txtSerialNumber.Text = Info.SerialNumber;
                txtCopyright.Text = Info.Copyright;
                txtAsName.Text = Info.ASName;
                txtModuleName.Text = Info.ModuleName;
            }
        }

        void ReadOrderCode()
        {
            S7Client.S7OrderCode Info = new S7Client.S7OrderCode();
            txtOrderCode.Text = "";
            txtVersion.Text = "";
            int Result = Client.GetOrderCode(ref Info);
            ShowResult(Result);
            if (Result == 0)
            {
                txtOrderCode.Text = Info.Code;
                txtVersion.Text = Info.V1.ToString() + "." + Info.V2.ToString() + "." + Info.V3.ToString();
            }
        }


        private void ReadBtn_Click(object sender, EventArgs e)
        {
            ReadArea();
        }

        private void FieldBtn_Click(object sender, EventArgs e)
        {
/*
          0 Byte    8 Bit Word                     (All)
          1 Word   16 Bit Word                     (All)
          2 DWord  32 Bit Word                     (All)
          3 LWord  64 Bit Word                     (S71500)
          4 USint   8 Bit Unsigned Integer         (S71200/1500)
          5 UInt   16 Bit Unsigned Integer         (S71200/1500)
          6 UDInt  32 Bit Unsigned Integer         (S71200/1500)
          7 ULint  64 Bit Unsigned Integer         (S71500)
          8 Sint    8 Bit Signed Integer           (S71200/1500)
          9 Int    16 Bit Signed Integer           (All)
         10 DInt   32 Bit Signed Integer           (S71200/1500)
         11 LInt   64 Bit Signed Integer           (S71500)
         12 Real   32 Bit Floating point           (All)
         13 LReal  64 Bit Floating point           (S71200/1500)
         14 Time   32 Bit Time elapsed ms          (All)
         15 LTime  64 Bit Time Elapsed ns          (S71500)
         16 Date   16 Bit days from 1990/1/1       (All)
         17 TOD    32 Bit ms elapsed from midnight (All)
         18 DT      8 Byte Date and Time           (All)
         19 LTOD   64 Bit time of day (ns)         (S71500)
         20 DTL    12 Byte Date and Time Long      (S71200/1500)
         21 LDT    64 Bit ns elapsed from 1970/1/1 (S71500)
*/
            int Pos = System.Convert.ToInt32(TxtOffset.Text);
            switch(CBType.SelectedIndex)
            {
                case 0:
                    {
                        TxtValue.Text = "16#" + System.Convert.ToString(Buffer[Pos], 16).ToUpper();
                        break;
                    }
                case 1:
                    {
                        UInt16 Word = S7.GetWordAt(Buffer, Pos);
                        TxtValue.Text = "16#" + System.Convert.ToString(Word, 16).ToUpper();
                        break;
                    }
                case 2:
                    {
                        UInt32 DWord = S7.GetDWordAt(Buffer, Pos);
                        TxtValue.Text = "16#" + System.Convert.ToString(DWord, 16).ToUpper();
                        break;
                    }
                case 3:
                    {
                        UInt64 LWord = S7.GetLWordAt(Buffer, Pos);
                        TxtValue.Text = "16#" + System.Convert.ToString((Int64)LWord, 16).ToUpper(); // <-- Convert.ToString does not handle UInt64
                        break;
                    }
                case 4:
                    {
                        UInt16 USInt = S7.GetUSIntAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(USInt);
                        break;
                    }
                case 5:
                    {
                        UInt16 UInt = S7.GetUIntAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(UInt);
                        break;
                    }
                case 6:
                    {
                        UInt32 UDInt = S7.GetDWordAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(UDInt);
                        break;
                    }
                case 7:
                    {
                        UInt64 ULInt = S7.GetLWordAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(ULInt);
                        break;
                    }
                case 8:
                    {
                        int SInt = S7.GetSIntAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(SInt);
                        break;
                    }
                case 9:
                    {
                        int S7Int = S7.GetIntAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(S7Int);
                        break;
                    }
                case 10:
                    {
                        int DInt = S7.GetDIntAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(DInt);
                        break;
                    }
                case 11:
                    {
                        Int64 LInt = S7.GetLIntAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(LInt);
                        break;
                    }
                case 12:
                    {
                        Single S7Real = S7.GetRealAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(S7Real);
                        break;
                    }
                case 13:
                    {
                        Double S7LReal = S7.GetLRealAt(Buffer, Pos);
                        TxtValue.Text = System.Convert.ToString(S7LReal);
                        break;
                    }
                case 14:
                    {
                        Int32 TimeElapsed = S7.GetDIntAt(Buffer, Pos);
                        // TIME type is a 32 signed number of ms elapsed
                        // Can be added to a DateTime or used as Value.
                        TxtValue.Text = "T#" + System.Convert.ToString(TimeElapsed) + "MS";
                        break;
                    }
                case 15:
                    {
                        Int64 TimeElapsed = S7.GetLIntAt(Buffer, Pos);
                        // LTIME type is a 64 signed number of ns elapsed
                        // Can be added (after a conversion) to a DateTime or used as Value.
                        TxtValue.Text = "LT#" + System.Convert.ToString(TimeElapsed) + "NS";
                        break;
                    }
                case 16:
                    {
                        DateTime DATE = S7.GetDateAt(Buffer, Pos);
                        TxtValue.Text = DATE.ToString("D#yyyy-MM-dd");
                        break;
                    }
                case 17:
                    {
                        DateTime TOD = S7.GetTODAt(Buffer, Pos);
                        TxtValue.Text = TOD.ToString("TOD#HH:mm:ss.fff");
                        break;
                    }
                case 18:
                    {
                        DateTime DT = S7.GetDateTimeAt(Buffer, Pos);
                        TxtValue.Text = DT.ToString("DT#yyyy-MM-dd-HH:mm:ss.fff");
                        break;
                    }
                case 19:
                    {
                        DateTime LTOD = S7.GetLTODAt(Buffer, Pos);
                        TxtValue.Text = LTOD.ToString("LTOD#HH:mm:ss.fffffff");
                        break;
                    }
                case 20:
                    {
                        DateTime DTL = S7.GetDTLAt(Buffer, Pos);
                        TxtValue.Text = DTL.ToString("DTL#yyyy-MM-dd-HH:mm:ss.fffffff");
                        break;
                    }
                case 21:
                    {
                        DateTime LDT = S7.GetLDTAt(Buffer, Pos);
                        TxtValue.Text = LDT.ToString("LDT#yyyy-MM-dd-HH:mm:ss.fffffff");
                        break;
                    }

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // These are tests done on my DB
            
            DateTime DT = DateTime.Now;
            S7.SetSIntAt(Buffer, 40, -125);
            S7.SetIntAt(Buffer, 42, 32501);
            S7.SetDIntAt(Buffer, 44, -332501);
            S7.SetLIntAt(Buffer, 48, -99832501);
            S7.SetRealAt(Buffer, 56, (float)98.778);
            S7.SetLRealAt(Buffer, 60, 123000000000.778);
            S7.SetUSIntAt(Buffer, 24, 125);
            S7.SetUIntAt(Buffer, 26, 32501);
            S7.SetUDIntAt(Buffer, 28, 332501);
            S7.SetULintAt(Buffer, 32, 99832501);
            S7.SetDateAt(Buffer, 80, DT);
            S7.SetTODAt(Buffer, 82, DT);
            S7.SetDTLAt(Buffer, 112, DT);
            S7.SetLTODAt(Buffer, 86, DT);
            S7.SetLDTAt(Buffer, 94, DT);
            PlcDBWrite();
        }

        private void btnMultiRead_Click(object sender, EventArgs e)
        {
            DBMultiRead();
        }

        private void btnMultiWrite_Click(object sender, EventArgs e)
        {
            DBMultiWrite();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            int Result = Client.WriteArea(S7Consts.S7AreaCT, 0, 1, 3, S7Consts.S7WLCounter, Buffer);
            ShowResult(Result);
        }

        private void ReadSZLBtn_Click(object sender, EventArgs e)
        {
            ReadSZL();
        }

        private void ReadCPUInfoBtn_Click(object sender, EventArgs e)
        {
            ReadCPUInfo();
        }

        private void ReadOrderCodeBtn_Click(object sender, EventArgs e)
        {
            ReadOrderCode();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DateTime DT = new DateTime();
            if (Client.GetPlcDateTime(ref DT) == 0)
            {
                txtDateTime.Text = DT.ToLongDateString() + " - " + DT.ToLongTimeString();
            }
        }

        private void SetDateTimeBtn_Click(object sender, EventArgs e)
        {
            ShowResult(Client.SetPlcSystemDateTime());
        }

        private void SetPwdBtn_Click(object sender, EventArgs e)
        {
            ShowResult(Client.SetSessionPassword(txtPwd.Text));
        }

        private void ClrPwdBtn_Click(object sender, EventArgs e)
        {
            ShowResult(Client.ClearSessionPassword());
        }

        void ShowPlcStatus()
        {
            int Status = 0;
            int Result = Client.PlcGetStatus(ref Status);
            ShowResult(Result);
            if (Result == 0)
            {
                switch (Status)
                {
                    case S7Consts.S7CpuStatusRun:
                        {
                            lblStatus.Text = "RUN";
                            lblStatus.ForeColor = System.Drawing.Color.LimeGreen;
                            break;
                        }
                    case S7Consts.S7CpuStatusStop:
                        {
                            lblStatus.Text = "STOP";
                            lblStatus.ForeColor = System.Drawing.Color.Red;
                            break;
                        }
                    default:
                        {
                            lblStatus.Text = "Unknown";
                            lblStatus.ForeColor = System.Drawing.Color.Black;
                            break;
                        }
                }
            }
            else
            {
                lblStatus.Text = "Unknown";
                lblStatus.ForeColor = System.Drawing.Color.Black;
            }
        }

        string HexByte(byte B)
        {
            string Result = Convert.ToString(B, 16);
            if (Result.Length < 2)
                Result = "0" + Result;
            return "0x" + Result;
        }

        string HexWord(ushort W)
        {
            string Result = Convert.ToString(W, 16);
            while(Result.Length<4)
                Result = "0" + Result;
            return "0x" + Result;
        }

        void GetBlockInfo()
        {
            S7Client.S7BlockInfo BI = new S7Client.S7BlockInfo();
            int[] BlockType =
            {
                S7Client.Block_OB, 
                S7Client.Block_DB, 
                S7Client.Block_SDB,
                S7Client.Block_FC, 
                S7Client.Block_SFC,
                S7Client.Block_FB, 
                S7Client.Block_SFB
            };
            txtBI.Text = "";
            int Result = Client.GetAgBlockInfo(BlockType[CBBlock.SelectedIndex], System.Convert.ToInt32(txtBlockNum.Text), ref BI);
            ShowResult(Result);
            if (Result==0)
            {
                // Here a more descriptive Block Type, Block lang and so on, are needed, 
                // but I'm too lazy, do it yourself.
                txtBI.Text = txtBI.Text + "Block Type    : " + HexByte((byte)BI.BlkType) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Block Number  : " + Convert.ToString(BI.BlkNumber) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Block Lang    : " + HexByte((byte)BI.BlkLang) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Block Flags   : " + HexByte((byte)BI.BlkFlags) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "MC7 Size      : " + Convert.ToString(BI.MC7Size) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Load Size     : " + Convert.ToString(BI.LoadSize) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Local Data    : " + Convert.ToString(BI.LocalData) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "SBB Length    : " + Convert.ToString(BI.SBBLength) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Checksum      : " + HexWord((ushort)BI.CheckSum) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Version       : 0." + Convert.ToString(BI.Version) + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Code Date     : " + BI.CodeDate + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Intf.Date     : " + BI.IntfDate + (char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Author        : " + BI.Author +(char)13 + (char)10;               
                txtBI.Text = txtBI.Text + "Family        : " + BI.Family +(char)13 + (char)10;
                txtBI.Text = txtBI.Text + "Header        : " + BI.Header;
            }
        }

        void GetSelectedDB()
        {
            int Size = 65536; // 64 K (the maximum for a S7400)
            byte[] Buffer = new byte[Size];
            txtDBGet.Text = "";
            int Result = Client.DBGet(System.Convert.ToInt32(txtBlockNum.Text), Buffer, ref Size);
            ShowResult(Result);
            if (Result==0)
            {
                HexDump(txtDBGet, Buffer, Size);
            }
        }

        private void PlcStatusBtn_Click(object sender, EventArgs e)
        {
            ShowPlcStatus();
        }

        private void PlcStopBtn_Click(object sender, EventArgs e)
        {
            ShowResult(Client.PlcStop());
            ShowPlcStatus();
        }

        private void PlcHotBtn_Click(object sender, EventArgs e)
        {
            ShowResult(Client.PlcHotStart());
            ShowPlcStatus();
        }

        private void PlcColdBtn_Click(object sender, EventArgs e)
        {
            ShowResult(Client.PlcColdStart());
            ShowPlcStatus();
        }

        private void BlockInfoBtn_Click(object sender, EventArgs e)
        {
            GetBlockInfo();
        }

        private void DBGetBtn_Click(object sender, EventArgs e)
        {
            GetSelectedDB();
        }
    }
}
