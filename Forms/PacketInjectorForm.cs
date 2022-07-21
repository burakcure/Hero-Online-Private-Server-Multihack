using Be.Windows.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace OSPE.Forms
{
    public partial class PacketInjectorForm : Form
    {
        private ushort _socketId = 0;
        private int _editingIndex;
        private int _repeats;

        public PacketInjectorForm(int edit = -1)
        {
            _editingIndex = edit;
            InitializeComponent();
            hexBox1.ByteProvider = new DynamicByteProvider(new byte[0]);
            if (_editingIndex != -1)
            {
                SendInfo si = SendManager.GetSendInfoAtListIndex(_editingIndex);
                LoadFormData(si.Name, si.Active, si.Packet);
            }

        }
        public PacketInjectorForm(string name, bool active, Packet p)
        {
            InitializeComponent();
            LoadFormData(name, active, p);
        }

        private void LoadFormData(string name, bool active, Packet p)
        {
            txtItemName.Text = name;
            chkEnabled.Checked = active;
            txtData.Text = p.GetBufferAsText();
            numPacketSize.Value = p.Size;
            numOpenedSocketId.Value = p.SocketId;
            txtNewSocketIp.Text = p.RemoteIp.ToString();
            numNewSocketPort.Value = p.RemotePort;
            hexBox1.ByteProvider = new DynamicByteProvider(p.Data);
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            timerSender.Interval = (int) numDelay.Value;
            _repeats = 0;
            timerSender.Enabled = true;

            if (radNewSocket.Checked)
            {
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //_socketId = s.; // TODO
            }
            else
                _socketId = (ushort)numOpenedSocketId.Value;

            btnStart.Enabled = false;
            btnStop.Enabled = true;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            timerSender.Enabled = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            _repeats = 0;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Packet packet = new Packet(Functions.CODE_NOP, 
                (ushort) numOpenedSocketId.Value, 0, 0, 
                BitConverter.ToUInt16(System.Net.IPAddress.Parse(txtNewSocketIp.Text).GetAddressBytes(), 0), 
                (ushort) numNewSocketPort.Value, 
                Encoding.ASCII.GetBytes(txtData.Text), Packet.Directions.None);
            SendInfo si = new SendInfo(txtItemName.Text, chkEnabled.Enabled, packet);
            if (_editingIndex != -1)
                SendManager.ReplaceFromList(_editingIndex, si);
            else
                SendManager.AddToList(si);
            Program.mainForm.LoadSendListItems();
            Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
        int byteToIntStack = 156;
        int byteToIntStackSecond = 64;
        int byteToIntStackThird = 0;
        int byteToIntStackLoot = 0;
        int byteToIntStackSecondLoot = 0;
        int mobSizeDamageHack = mobSize.Text;//Convert.ToInt32(mobSize.Text);
        int lootSizeLootHack = 100;
        int slotSizeDeleteHack = 3000;
        private void timerSender_Tick(object sender, EventArgs e)
        {
            if (!radContinously.Checked && _repeats >= (int)numSendTimes.Value)
            {
                btnStop_Click(null, null);
                return;
            }

            _repeats++;
            var bp = hexBox1.ByteProvider;
          
            if (damageHack.Checked)
            {
                byte[] bytes = new byte[((bp.Length + 8) * mobSizeDamageHack)];

                for (int i = 0; i < bp.Length; i++)
                {
                    bytes[i] = bp.ReadByte(i);
                }
                bytes[14] = Convert.ToByte(170);
                bytes[15] = Convert.ToByte(85);
                bytes[16] = Convert.ToByte(2);
                bytes[17] = Convert.ToByte(0);
                bytes[18] = Convert.ToByte(254);
                bytes[19] = Convert.ToByte(13);
                bytes[20] = Convert.ToByte(85);
                bytes[21] = Convert.ToByte(170);
                for (int i = 0; i < (bp.Length + 8) * mobSizeDamageHack; i++)
                {
                    bytes[i] = bytes[i % 22];
                }

                for (int i = 0; i < mobSizeDamageHack; i++)
                {
                    bytes[8 + (i * (bp.Length + 8))] = Convert.ToByte(byteToIntStack);
                    bytes[7 + (i * (bp.Length + 8))] = Convert.ToByte(byteToIntStackSecond);

                    byteToIntStackSecond++;

                    if (byteToIntStackSecond == 255)
                    {
                        byteToIntStack++;
                        byteToIntStackSecond = 0;
                    }
                    if (byteToIntStack == 196)
                    {
                        byteToIntStack = 156;
                        byteToIntStackSecond = 64;
                        byteToIntStackThird = 0;
                    }
                }
                try
                {
                    DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, bytes, (UInt16)bytes.Length, _socketId);
                }
                catch { }
            } else if (deleteHack.Checked) {
                int inventorySlot = 0;
                int secondByte = 0;

                byte[] bytes = new byte[16 * slotSizeDeleteHack];


                bytes[0] = Convert.ToByte(170);
                bytes[1] = Convert.ToByte(85);
                bytes[2] = Convert.ToByte(10);
                bytes[3] = Convert.ToByte(0);
                bytes[4] = Convert.ToByte(89);
                bytes[5] = Convert.ToByte(48);
                bytes[6] = Convert.ToByte(209);
                bytes[7] = Convert.ToByte(202);
                bytes[8] = Convert.ToByte(155);
                bytes[9] = Convert.ToByte(00);
                bytes[10] = Convert.ToByte(00);
                bytes[11] = Convert.ToByte(00);
                bytes[12] = Convert.ToByte(01);
                bytes[13] = Convert.ToByte(00);
                bytes[14] = Convert.ToByte(85);
                bytes[15] = Convert.ToByte(170);
                for (int i = 0; i < 16  * slotSizeDeleteHack; i++)
                {
                    bytes[i] = bytes[i % 16];
                   
                }
                for (int i = 0; i < slotSizeDeleteHack; i++)
                {
                    bytes[10 + 16 * i] = Convert.ToByte(inventorySlot);
                    bytes[11 + 16 * i] = Convert.ToByte(secondByte);
                    inventorySlot++;
                    if (inventorySlot == 255)
                    {
                        inventorySlot = 0;
                        secondByte++;
                    }
                }
                try
                {
                    DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, bytes, (ushort)bytes.Length, _socketId);
                }
                catch { }

            }
            else if (lootHack.Checked)
            {
                byte[] bytes = new byte[((bp.Length ) * lootSizeLootHack)];

                for (int i = 0; i < bp.Length; i++)
                {
                    bytes[i] = bp.ReadByte(i);
                }

                for (int i = 0; i < (bp.Length) * lootSizeLootHack; i++)
                {
                    bytes[i] = bytes[i % 13];
                }

                for (int i = 0; i < lootSizeLootHack; i++)
                {
                    bytes[7 + (i * (bp.Length ))] = Convert.ToByte(byteToIntStackLoot);
                    bytes[8 + (i * (bp.Length ))] = Convert.ToByte(byteToIntStackSecondLoot);

                    byteToIntStackSecondLoot++;

                    if (byteToIntStackSecondLoot == 255)
                    {
                        byteToIntStackLoot++;
                        byteToIntStackSecondLoot = 0;
                    }
                    if (byteToIntStackLoot == 255)
                    {
                        byteToIntStackLoot = 0;
                        byteToIntStackSecondLoot = 0;
                        byteToIntStackThird = 0;
                    }
                }
                try
                {
                    DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, bytes, (UInt16)bytes.Length, _socketId);
                }
                catch { }
            }else if (skillHack.Checked)
            {
             
                    byte[] bytes = new byte[((bp.Length + 8) * mobSizeDamageHack)];

                    for (int i = 0; i < bp.Length; i++)
                    {
                        bytes[i] = bp.ReadByte(i);
                    }
                    bytes[26] = Convert.ToByte(170);
                    bytes[27] = Convert.ToByte(85);
                    bytes[28] = Convert.ToByte(2);
                    bytes[29] = Convert.ToByte(0);
                    bytes[30] = Convert.ToByte(73);
                    bytes[31] = Convert.ToByte(17);
                    bytes[32] = Convert.ToByte(85);
                    bytes[33] = Convert.ToByte(170);
                    for (int i = 0; i < (bp.Length + 8) * mobSizeDamageHack; i++)
                    {
                        bytes[i] = bytes[i % 34];
                    }

                    for (int i = 0; i < mobSizeDamageHack; i++)
                    {
                        bytes[23 + (i * (bp.Length + 8))] = Convert.ToByte(byteToIntStack);
                        bytes[22 + (i * (bp.Length + 8))] = Convert.ToByte(byteToIntStackSecond);

                        byteToIntStackSecond++;

                        if (byteToIntStackSecond == 255)
                        {
                            byteToIntStack++;
                            byteToIntStackSecond = 0;
                        }
                        if (byteToIntStack == 196)
                        {
                            byteToIntStack = 156;
                            byteToIntStackSecond = 64;
                            byteToIntStackThird = 0;
                        }
                    }
                    try
                    {
                        DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, bytes, (UInt16)bytes.Length, _socketId);
                    }
                    catch { }


                }
            else
            {
                byte[] bytes = new byte[bp.Length];

                for (int i = 0; i < bp.Length; i++)
                {
                    bytes[i] = bp.ReadByte(i);
                }

                try
                {
                    DllCommunication.WriteCommandToCmdMMF(ServerCodes.SCODE_INJECTPACKET, bytes, (ushort)bytes.Length, _socketId);
                }
                catch { }

            }







            
            
        }

        private void rad_CheckedChanged(object sender, EventArgs e)
        {
            numOpenedSocketId.Enabled = !radNewSocket.Checked;
            txtNewSocketIp.Enabled = radNewSocket.Checked;
            numNewSocketPort.Enabled = radNewSocket.Checked;
        }

        private void txtData_TextChanged(object sender, EventArgs e)
        {
            hexBox1.ByteProvider = new DynamicByteProvider(Encoding.ASCII.GetBytes(txtData.Text));
            numPacketSize.Value = txtData.Text.Length;
        }

        private void hexBox1_Paint(object sender, PaintEventArgs e)
        {
            var bp = hexBox1.ByteProvider;
            StringBuilder strBuilder = new System.Text.StringBuilder((int)bp.Length);
            for (int i = 0; i < bp.Length; i++)
            {
                strBuilder.Insert(i, (char)bp.ReadByte(i));
            }
            txtData.TextChanged -= txtData_TextChanged;
            txtData.Text = strBuilder.ToString();
            txtData.TextChanged += txtData_TextChanged;
        }

        private void noHack_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
