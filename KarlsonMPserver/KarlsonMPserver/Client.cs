﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace KarlsonMPserver
{
    class Client
    {
        const int dataBufferSize = 4096;

        public int id;
        public TCP tcp;
        public Player player;

        public Client(int _id)
        {
            id = _id;
            tcp = new TCP(id);
        }

        public class TCP
        {
            public TcpClient socket;
            private readonly int id;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;
            public DateTime connectedTimeStamp;
            //public bool connected; not needed bc the player field is != null when client is connected
            public TCP(int _id)
            {
                id = _id;
            }
            public void Connect(TcpClient _client)
            {
                socket = _client;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;
                stream = socket.GetStream();
                receivedData = new Packet();
                receiveBuffer = new byte[dataBufferSize];
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                ServerSend.Welcome(id, Program.config.motd);
                connectedTimeStamp = DateTime.Now;
            }

            private void ReceiveCallback(IAsyncResult ar)
            {
                if(stream == null || socket == null || !socket.Connected)
                {
                    Server.clients[id].Disconnect();
                    return;
                }
                try
                {
                    int _byteLength = stream.EndRead(ar);
                    if(_byteLength <= 0)
                    {
                        Server.clients[id].Disconnect();
                        return;
                    }
                    byte[] _data = new byte[_byteLength];
                    Array.Copy(receiveBuffer, _data, _byteLength);
                    receivedData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch(Exception _ex)
                {
                    Program.Log($"Error while receiving data: {_ex}");
                    Server.clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLength = 0;
                receivedData.SetBytes(_data);
                if (receivedData.UnreadLength() >= 4)
                {
                    _packetLength = receivedData.ReadInt();
                    if (_packetLength <= 0)
                        return true;
                }
                while (_packetLength > 0 && _packetLength <= receivedData.UnreadLength())
                {
                    byte[] _packetBytes = receivedData.ReadBytes(_packetLength);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        Packet _packet = new(_packetBytes);
                        int _packetId = _packet.ReadInt();
                        if(Server.packetHandlers.ContainsKey(_packetId))
                            Server.packetHandlers[_packetId](id, _packet);
                    });

                    _packetLength = 0;
                    if (receivedData.UnreadLength() >= 4)
                    {
                        _packetLength = receivedData.ReadInt();
                        if (_packetLength <= 0)
                            return true;
                    }
                }
                if (_packetLength <= 1)
                    return true;
                return false;
            }

            public void SendData(byte[] _data)
            {
                try
                {
                    if (socket == null || !socket.Connected)
                        return;
                    stream.BeginWrite(_data, 0, _data.Length, null, null);
                }
                catch(Exception _ex)
                {
                    Program.Log($"Error while sending data: {_ex}");
                }
            }

            public void Disconnect()
            {
                if (socket == null)
                    return;
                socket.Close();
                stream = null;
                receivedData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public void Disconnect()
        {
            if(player != null && player.scene != "")
            {
                Program.Log($"ID {id} has disconnected");
                ServerSend.Chat("<color=red>[-] <b>" + player.username + "</b></color>");
                string old = player.scene;
                player.scene = "";
                foreach (Client client in from x in Server.clients
                                          where x.Value.tcp.socket != null && x.Value.player != null
                                             && x.Value.player.scene == old
                                          select x.Value)
                    ServerSend.LeaveScene(client.id, id);
            }
            player = null;

            tcp.Disconnect();
        }
    }
}
