/*=============================================================================|
|  PROJECT Sharp7                                                        1.0.0 |
|==============================================================================|
|  Copyright (C) 2013, Davide Nardella                                         |
|  All rights reserved.                                                        |
|==============================================================================|
|  SNAP7 is free software: you can redistribute it and/or modify               |
|  it under the terms of the Lesser GNU General Public License as published by |
|  the Free Software Foundation, either version 3 of the License, or           |
|  (at your option) any later version.                                         |
|                                                                              |
|  It means that you can distribute your commercial software linked with       |
|  SNAP7 without the requirement to distribute the source code of your         |
|  application and without the requirement that your application be itself     |
|  distributed under LGPL.                                                     |
|                                                                              |
|  SNAP7 is distributed in the hope that it will be useful,                    |
|  but WITHOUT ANY WARRANTY; without even the implied warranty of              |
|  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the               |
|  Lesser GNU General Public License for more details.                         |
|                                                                              |
|  You should have received a copy of the GNU General Public License and a     |
|  copy of Lesser GNU General Public License along with Snap7.                 |
|  If not, see  http://www.gnu.org/licenses/                                   |
|==============================================================================|
|                                                                              |
|  Client Example                                                              |
|                                                                              |
|=============================================================================*/
using System;
using System.Runtime.InteropServices;
using System.Text;
using Sharp7;

class ClientDemo
{
    static S7Client Client;
    static int ok = 0, ko = 0;
    
    static byte[] DB_A = new byte[1024];
    static byte[] DB_B = new byte[1024];
    static byte[] DB_C = new byte[1024];

    static int DBNumber_A = 1; // DB 1 used for DBRead and first item of MultiRead
    static int DBNumber_B = 2; // DB 2 used for 2nd item of MultiRead
    static int DBNumber_C = 3; // DB 3 used for 3th item of MultiRead
    
    #region [Utility]

    //------------------------------------------------------------------------------
    // Usage syntax 
    //------------------------------------------------------------------------------
    static void Usage()
    {
        Console.WriteLine("Usage");
        Console.WriteLine("  client <IP> [Rack=0 Slot=2]");
        Console.WriteLine("Example");
        Console.WriteLine("  client 192.168.1.101 0 2");
        Console.WriteLine("or");
        Console.WriteLine("  client 192.168.1.101");
        Console.ReadKey();
    }
    //-------------------------------------------------------------------------  
    // Summary
    //-------------------------------------------------------------------------  
    static void Summary()
    {
        Console.WriteLine();
        Console.WriteLine("+-----------------------------------------------------");
        Console.WriteLine("| Test Summary");
        Console.WriteLine("+-----------------------------------------------------");
        Console.WriteLine("| Performed : " + (ok + ko).ToString());
        Console.WriteLine("| Passed    : " + ok.ToString());
        Console.WriteLine("| Failed    : " + ko.ToString());
        Console.WriteLine("+----------------------------------------[press a key]");
    }
    //------------------------------------------------------------------------------
    // HexDump, a very nice function, it's not mine.
    // I found it on the net somewhere some time ago... thanks to the author ;-)
    //------------------------------------------------------------------------------
    static void HexDump(byte[] bytes, int Size)
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
#if __MonoCS__
            result.Append('\n');
#endif
        }
        Console.WriteLine(result.ToString());
    }
    //------------------------------------------------------------------------------
    // Check error (simply writes an header)
    //------------------------------------------------------------------------------
    static bool Check(int Result, string FunctionPerformed)
    {
        Console.WriteLine();
        Console.WriteLine("+-----------------------------------------------------");
        Console.WriteLine("| " + FunctionPerformed);
        Console.WriteLine("+-----------------------------------------------------");
        if (Result == 0)
        {
            int ExecTime = Client.ExecTime();
            Console.WriteLine("| Result         : OK");
            Console.WriteLine("| Execution time : " + ExecTime.ToString() + " ms"); //+ Client.getex->ExecTime());
            Console.WriteLine("+-----------------------------------------------------");
            ok++; // one more test passed
        }
        else
        {
            Console.WriteLine("| ERROR !!! \n");
            if (Result < 0)
                Console.WriteLine("| Library Error (-1)\n");
            else
                Console.WriteLine("| " + Client.ErrorText(Result));
            Console.WriteLine("+-----------------------------------------------------\n");
            ko++;
        }
        return Result == 0;
    }
    #endregion

    #region [Info]
    //------------------------------------------------------------------------------
    // CPU Info : unit info
    //------------------------------------------------------------------------------
    static void CpuInfo()
    {
        S7Client.S7CpuInfo Info = new S7Client.S7CpuInfo();
        int res = Client.GetCpuInfo(ref Info);
        if (Check(res, "Unit Info"))
        {
            Console.WriteLine("  Module Type Name : " + Info.ModuleTypeName);
            Console.WriteLine("  Serial Number    : " + Info.SerialNumber);
            Console.WriteLine("  AS Name          : " + Info.ASName);
            Console.WriteLine("  Module Name      : " + Info.ModuleName);
        };
    }
    //-------------------------------------------------------------------------  
    // Read a SZL block : ID 0x0011 IDX 0x0000
    //-------------------------------------------------------------------------  
    static void ReadSZL_0011_0000()
    {
        S7Client.S7SZL SZL = new S7Client.S7SZL();
        int Size = 0x8000;
        SZL.Data = new byte[Size];
        int res = Client.ReadSZL(0x0011, 0x000, ref SZL, ref Size);
        if (Check(res, "Read SZL - ID : 0x0011, IDX 0x0000"))
        {
            Console.WriteLine("  LENTHDR : " + SZL.Header.LENTHDR.ToString());
            Console.WriteLine("  N_DR    : " + SZL.Header.N_DR.ToString());
            Console.WriteLine("Dump : " + Size.ToString() + " bytes");
            HexDump(SZL.Data, Size);
        }
    }
    #endregion

    //------------------------------------------------------------------------------
    // DB Dump using DB Read                            
    //------------------------------------------------------------------------------
    static void DBReadAndDump()
    {
        int Size = 32;
        byte[] Buffer = new byte[32]; 
        int res = Client.DBRead(DBNumber_A, 0, Size, Buffer);
        if (Check(res, "DB Read (DB = " + DBNumber_A.ToString() + ", Start = 0, Size = " + Size.ToString() + ", Buffer)"))
        {
            Console.WriteLine("Dump : " + Size.ToString() + " bytes");
            HexDump(Buffer, Size);
        }
    }
    //-------------------------------------------------------------------------  
    // PLC connection
    //-------------------------------------------------------------------------  
    static bool PlcConnect(string Address, int Rack, int Slot)
    {
        int res = Client.ConnectTo(Address, Rack, Slot);
        if (Check(res, "UNIT Connection"))
        {
            int Requested = Client.RequestedPduLength();
            int Negotiated = Client.NegotiatedPduLength();
            Console.WriteLine("  Connected to   : " + Address + " (Rack=" + Rack.ToString() + ", Slot=" + Slot.ToString() + ")");
            Console.WriteLine("  PDU Requested  : " + Requested.ToString());
            Console.WriteLine("  PDU Negotiated : " + Negotiated.ToString());
        }
        return res == 0;
    }
    //-------------------------------------------------------------------------  
    // PLC Stop/Run
    //-------------------------------------------------------------------------  
    static void StopRun()
    {
        int res = Client.PlcStop();
        Check(res, "PLC Stop");
        if (res==0)
        {
            Console.WriteLine();
            Console.WriteLine(" Waiting 2000 ms ....");
            System.Threading.Thread.Sleep(2000);
            Check(Client.PlcColdStart(), "PLC Cold Start");
        }
    }
    //-------------------------------------------------------------------------  
    // PLC Date and Time
    //-------------------------------------------------------------------------  
    static void DateAndTime()
    {
        DateTime DT = new DateTime();
        int res = Client.GetPlcDateTime(ref DT);
        if (Check(res,"PLC Date and Time"))
        {
            Console.WriteLine(DT.ToLongDateString() + " - " + DT.ToLongTimeString());
        }
    }

    //-------------------------------------------------------------------------  
    // MultiRead
    //-------------------------------------------------------------------------  
    static void MultiRead()
    {
        int Size = 16;
        // Multi Reader Instance
        S7MultiVar Reader = new S7MultiVar(Client);
        // Add Items def.
        Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_A, 0, Size, ref DB_A);
        Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_B, 0, Size, ref DB_B);
        Reader.Add(S7Consts.S7AreaDB, S7Consts.S7WLByte, DBNumber_C, 0, Size, ref DB_C);
        // Performs the Read
        int res = Reader.Read();
        if (Check(res, "Multi Read in a single telegram"))
        {

            if (Reader.Results[0]==0)
            {
                Console.WriteLine("Dump : " + Size.ToString() + " bytes from DB " + DBNumber_A.ToString());
                HexDump(DB_A, Size);
            }
            else
                Console.WriteLine("DB " + DBNumber_A.ToString() + " " + Client.ErrorText(Reader.Results[0]));

            if (Reader.Results[1] == 0)
            {
                Console.WriteLine("Dump : " + Size.ToString() + " bytes from DB " + DBNumber_B.ToString());
                HexDump(DB_B, Size);
            }
            else
                Console.WriteLine("DB " + DBNumber_B.ToString() + " " + Client.ErrorText(Reader.Results[0]));

            if (Reader.Results[2] == 0)
            {
                Console.WriteLine("Dump : " + Size.ToString() + " bytes from DB " + DBNumber_C.ToString());
                HexDump(DB_C, Size);
            }
            else
                Console.WriteLine("DB " + DBNumber_C.ToString() + " " + Client.ErrorText(Reader.Results[0]));
        }
    }
    //-------------------------------------------------------------------------  
    // Perform some safe (readonly) tests
    //-------------------------------------------------------------------------  
    static void PerformTests()
    {
        CpuInfo();
        DateAndTime();
        DBReadAndDump();
        ReadSZL_0011_0000();
        MultiRead();
        StopRun();
    }
    //-------------------------------------------------------------------------  
    // Main                                  
    //-------------------------------------------------------------------------  
    public static void Main(string[] args)
    {
        int Rack = 0, Slot = 2; // default for S7300

        // Uncomment next line if you are not able to see
        // the entire test text. (Note : Doesn't work in Mono 2.10)

        // Console.SetBufferSize(80, Int16.MaxValue-1);

        // Get Progran args
        if ((args.Length != 1) && (args.Length != 3))
        {
            Usage();
            return;
        }
        if (args.Length == 3) // only address without rack and slot
        {
            Rack = Convert.ToInt32(args[1]);
            Slot = Convert.ToInt32(args[2]);
        }
        // Client creation
        Client = new S7Client();
        // Try Connection
        if (PlcConnect(args[0], Rack, Slot))
        {
            PerformTests();
            Client.Disconnect();
        }
        // Prints a short summary
        Summary();
        Console.ReadKey();
    }
}
