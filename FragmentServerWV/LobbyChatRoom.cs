﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FragmentServerWV
{
    public class LobbyChatRoom
    {
        public string name;
        public ushort ID;
        public ushort type;
        public List<int> Users;
        public LobbyChatRoom(string desc, ushort id, ushort t)
        {
            Users = new List<int>();
            name = desc;
            ID = id;
            type = t;
        }

        public int FindRoomIndexById(int id)
        {
            int result = -1;
            for (int i = 0; i < Users.Count; i++)
                if (Users[i] == id)
                    return i + 1;
            return result;
        }

        public void DispatchAllStatus(int towho)
        {
            GameClient client = null;
            foreach(GameClient c in Server.clients)
                if(c.index == towho)
                {
                    client = c;
                    break;
                }
            if(client == null)
                return;
            foreach (GameClient c in Server.clients)
                if (c.index != towho && c.room_index == ID)
                    client.SendPacket30(0x7009, c.last_status);
        }

        public void DispatchStatus(byte[] data, int who)
        {
            MemoryStream m = new MemoryStream();
            m.Write(BitConverter.GetBytes(GameClient.swap16((ushort)FindRoomIndexById(who))), 0, 2);
            m.Write(BitConverter.GetBytes(GameClient.swap16((ushort)(data.Length))), 0, 2);
            m.Write(data, 0, data.Length);
            byte[] buff = m.ToArray();
            foreach (GameClient client in Server.clients)
                if (!client.isAreaServer && client.index != who && !client._exited && client.room_index == ID)
                    client.SendPacket30(0x7009, buff);
                else if (client.index == who)
                    client.last_status = buff;
        }

        public void DispatchPublicBroadcast(byte[] data, int who)
        {
            int id = FindRoomIndexById(who);
            byte[] temp = new byte[data.Length];
            data.CopyTo(temp, 0);
            foreach (GameClient client in Server.clients)
                if (!client.isAreaServer && !client._exited && client.room_index == ID && client.index != who)
                {
                    temp[0] = (byte)(id >> 8);
                    temp[1] = (byte)(id & 0xff);
                    client.SendPacket30(0x7862, temp);
                }
                else if(client.index == who)
                {
                    temp[0] = 0xff;
                    temp[1] = 0xff;
                    client.SendPacket30(0x7862, temp);
                }
        }
        
        public void DispatchPrivateBroadcast(byte[] data, int who, int destid)
        {
            int srcid = FindRoomIndexById(who);
            int towho = Users[destid - 1];
            foreach (GameClient client in Server.clients)
                if (client.index == towho)
                {
                    byte[] temp = new byte[data.Length];
                    data.CopyTo(temp, 0);
                    temp[2] = (byte)(srcid >> 8);
                    temp[3] = (byte)(srcid & 0xff);
                    client.SendPacket30(0x788c, temp);
                }
                else if (client.index == who)
                {
                    byte[] temp = new byte[data.Length];
                    data.CopyTo(temp, 0);
                    temp[2] = 0xff;
                    temp[3] = 0xff;
                    client.SendPacket30(0x788c, temp);
                }
        }
    }
}
