/*
* OSPE - Open Source Packet Editor
* Copyright(C) 2018-2019 Javier Pereda <https://github.com/elecyb>
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using Timer = System.Timers.Timer;

namespace OSPE
{

    static class PacketManager
    {

        public static List<Packet> PacketList = new List<Packet>();
        public static List<int> Received = new List<int>();
        public static List<int> Sent = new List<int>();
        public static List<int> Both = new List<int>();
        public static List<int> Watch = new List<int>();

        public static int TotalSize { get; private set; }
        public static int TotalSizeReceived { get; private set; }
        public static int TotalSizeSent { get; private set; }

        public static int PacketsCount { get { return PacketList.Count; } }
        public static bool IsModifiedList { get; private set; }
        public static FilterCaptureFuncs LogFunctions = (FilterCaptureFuncs)Settings.LogFunctions;

        public static bool IsFilteringActived;
        public static bool IsCaptureEnabled;
        public static bool damageHack = false;
        public static bool skillDamageHack = false;
        private static int _lastLabelRefresh;
        private static int _lastTableRefresh;
        private static int _lastPacketCount;
        private static Timer timer = new Timer{Interval = 1000, Enabled = true, AutoReset = false};
        private static readonly object LockingVar = new object();
        public static byte[] atack;
        public static int socket;
        
        static PacketManager()
        {
            timer.Elapsed += On_TimerElapsed;
        }

        public static void StartCapture()
        {
            IsCaptureEnabled = true;
        }
        public static void StopCapture()
        {
            IsCaptureEnabled = false;
        }
        public static void StartFiltering()
        {
            IsFilteringActived = true;
        }
        public static void StopFiltering()
        {
            IsFilteringActived = false;
        }
        public static DateTime measurement = DateTime.Now;
        public static byte pm1 = 0x00;
        public static byte pm2 = 0x00;
        public static int counter = 0;
        public static void PacketReceived(PacketInfo packetInfo, ref byte[] data, int echo = 0/*, ref ServerCommand serverResponse*/)
        {
            Packet.Directions direction;
            int ID;
            TotalSize += packetInfo.Size;

            switch (packetInfo.FunctionID)
            {
                case Functions.CODE_RECV:
                case Functions.CODE_RECVFROM:
                case Functions.CODE_WS2RECV:
                case Functions.CODE_WS2RECVFROM:
                case Functions.CODE_WSARECV:
                case Functions.CODE_WSARECVFROM:
                case Functions.CODE_PR_RECV:
                case Functions.CODE_PR_READ:
                case Functions.CODE_SSLDECRYPTPACKET:
                case Functions.CODE_DECRYPTMESSAGE:
                case Functions.CODE_SSL_READ:
                    direction = Packet.Directions.In;
                    TotalSizeReceived += packetInfo.Size;
                    break;
                case Functions.CODE_SEND:
                case Functions.CODE_SENDTO:
                case Functions.CODE_WS2SEND:
                case Functions.CODE_WS2SENDTO:
                case Functions.CODE_WSASEND:
                case Functions.CODE_WSASENDTO:
                case Functions.CODE_PR_SEND:
                case Functions.CODE_PR_WRITE:
                case Functions.CODE_SSLENCRYPTPACKET:
                case Functions.CODE_ENCRYPTMESSAGE:
                case Functions.CODE_SSL_WRITE:
                    direction = Packet.Directions.Out;
                    TotalSizeSent += packetInfo.Size;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }

            var functionFlag = FilterManager.GetFilterActionFlagForFunction(packetInfo.FunctionID);

            // If a matching breakpoint type filter is active, we open a new window to edit the data
            if (IsFilteringActived && FilterManager.CheckPacketBreak(data, packetInfo.Size, functionFlag))
            {
                var pckt = new Packet(packetInfo.FunctionID, packetInfo.SocketId, packetInfo.LocalIp, packetInfo.LocalPort, packetInfo.RemoteIp, packetInfo.RemotePort, data, direction);
                var showPacketForm = new ShowPacketForm(0, pckt, true);
                showPacketForm.ShowDialog();
                if (showPacketForm.DialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    data = showPacketForm.NewPacketData;
                    packetInfo.Size = showPacketForm.NewPacketSize;
                }
                // Send the new data to the DLL
                DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_SETPACKET, data, packetInfo.Size);
            }

            if (!IsCaptureEnabled) // Capture disabled, return
                return;

            // Don't log Functions, Ips, or Ports that are not activated in the program's settings menu
            if (!LogFunctions.HasFlag(FilterManager.GetFilterActionFlagForFunction(packetInfo.FunctionID)))
                return;
            if ((Settings.LocalIpChecked && packetInfo.LocalIp != (uint)Settings.LocalIp) ||
                    (Settings.LocalPortChecked && packetInfo.LocalPort != Settings.LocalPort) ||
                    (Settings.RemoteIpChecked && packetInfo.RemoteIp != (uint)Settings.RemoteIp) ||
                    (Settings.RemotePortChecked && packetInfo.RemotePort != Settings.RemotePort))
                return;


            IsModifiedList = true;
            var packet = new Packet(packetInfo.FunctionID, packetInfo.SocketId, packetInfo.LocalIp, packetInfo.LocalPort, packetInfo.RemoteIp, packetInfo.RemotePort, data, direction);
            
            lock(LockingVar)
            {
                ID = PacketList.Count;
                PacketList.Add(packet);
                Program.mainForm.AddPacket(packet);
            }  

            if (IsFilteringActived && FilterManager.CheckPacketWatch(data, packetInfo.Size, functionFlag))
                Watch.Add(ID);
            // Don't log ignored packets
            if (IsFilteringActived && FilterManager.CheckPacketIgnore(data, packetInfo.Size, functionFlag))
                return;

            Both.Add(ID);

            switch (direction)
            {
                case Packet.Directions.In:
                    if (skillDamageHack == true&&packet.Data.Length==39)
                    {
                        
                            byte mobid1 = packet.Data[5];
                        byte mobid2 = packet.Data[6];
                        byte[] packetHack = new byte[] { 0xAA, 0X55, 0X14, 0X00, 0X49, 0X10, 0XEB, 0X67, 0X00, 0X00, 0XF1, 0X7E, 0X18, 0X43, 0X88, 0X10, 0X2B, 0X43, 0X38, 0XCD, 0XFC, 0X3D, mobid1, mobid2, 0X55, 0XAA, 0XAA, 0X55, 0X02, 0X00, 0XFE, 0X1B, 0X55, 0XAA };
                        
                        if (counter<10&&mobid1!=pm1)
                        {
                            counter++;
                            pm1 = mobid1;
                            DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, packetHack, (ushort)packetHack.Length, (UInt16)packetInfo.SocketId);

                        }
                        if((measurement - DateTime.Now).TotalSeconds < -1)
                        {
                            measurement = DateTime.Now;
                            counter = 0;
                        }

                    }
                    Received.Add(ID);
                    break;
                case Packet.Directions.Out:
                    if ((packet.Data[2]) == 8&&packet.Data.Length==14&&damageHack==true)
                    {
                        atack = packet.Data;
                        socket = packet.SocketId;
                        byte[] yedix = new byte[PacketManager.atack.Length + 8];
                        for (int i = 0; i < PacketManager.atack.Length; i++)
                        {
                            yedix[i] = PacketManager.atack[i];


                        }
                        yedix[PacketManager.atack.Length] = Convert.ToByte(170);
                        yedix[PacketManager.atack.Length + 1] = Convert.ToByte(85);
                        yedix[PacketManager.atack.Length + 2] = Convert.ToByte(2);
                        yedix[PacketManager.atack.Length + 3] = Convert.ToByte(0);
                        yedix[PacketManager.atack.Length + 4] = Convert.ToByte(254);
                        yedix[PacketManager.atack.Length + 5] = Convert.ToByte(13);
                        yedix[PacketManager.atack.Length + 6] = Convert.ToByte(85);
                        yedix[PacketManager.atack.Length + 7] = Convert.ToByte(170);
                        byte[] yedixmultiplier = new byte[yedix.Length * 500];
                        for (int i = 0; i < yedixmultiplier.Length; i++)
                        {
                            yedixmultiplier[i] = yedix[i % yedix.Length];

                        }
                        DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, yedixmultiplier, (UInt16)yedixmultiplier.Length, (UInt16)PacketManager.socket);

                        Sent.Add(ID);
                    }
                    else
                    Sent.Add(ID);
                    break;
            }
            
            SmartRefresh();
        }

        private static void SmartRefresh(bool forced = false)
        {
            if ((Environment.TickCount - _lastLabelRefresh) > 50 || forced)
            {
                Program.mainForm.UpdateLabels(PacketList.Count, TotalSizeReceived, TotalSizeSent);
                _lastLabelRefresh = Environment.TickCount;
            }

            if ((Environment.TickCount - _lastTableRefresh) > 500 || forced)
            {
                Program.mainForm.RefreshTables();
                _lastTableRefresh = Environment.TickCount;
                _lastPacketCount = PacketList.Count;
                timer.Start();
            }
        }

        private static void On_TimerElapsed(object sender, EventArgs e)
        {
            // If new packets arrived and more than 500 ms from the last Refresh have passed then we do a Refresh.
            if (PacketList.Count > _lastPacketCount && (Environment.TickCount - _lastTableRefresh) > 500)
            {
                Program.mainForm.UpdateLabels(PacketList.Count, TotalSizeReceived, TotalSizeSent);
                Program.mainForm.RefreshTables();

                _lastPacketCount = PacketList.Count;
            }
        }

        public static void ClearStorage()
        {
            lock (LockingVar)
            {
                if (PacketList.Count == 0) // Already clear
                    return; 

                PacketList = new List<Packet>();
            }
            Both = new List<int>();
            Sent = new List<int>();
            Received = new List<int>();
            Watch = new List<int>();
            IsModifiedList = false;

            // Refresh view
            SmartRefresh(true);
        }

        public static void LoadCapture(CaptureRecord cr)
        {
            ClearStorage();
            // load capture data
            lock (LockingVar)
            {
                PacketList = cr.GetPacketList();
            }
            Both = cr.GetBothList();
            Sent = cr.GetSentList();
            Received = cr.GetReceivedList();
            Watch = cr.GetWatchList();

            // Refresh view
            SmartRefresh(true);
        }
    }





}
