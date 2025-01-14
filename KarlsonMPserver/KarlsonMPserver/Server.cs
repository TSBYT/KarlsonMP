﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace KarlsonMPserver
{
    class Server
    {
        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static string Branch = "main"; // TODO: add it to the server config
        public static Dictionary<int, Client> clients = new();
        private static TcpListener listener;
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        public static int OnlinePlayers()
        {
            int online = 0;
            for (int i = 1; i <= MaxPlayers; i++)
                if (clients[i].tcp.socket != null)
                    online++;
            return online;
        }

        public static void Start(int _port, int _maxPlayers)
        {
            Port = _port;
            MaxPlayers = _maxPlayers;
            InitializeServerData();
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            listener.BeginAcceptTcpClient(TCPConnectCallback, null);
            Program.Log($"Server listening for connections on *:{Port}");
        }
        private static void TCPConnectCallback(IAsyncResult ar)
        {
            TcpClient _client = listener.EndAcceptTcpClient(ar);
            listener.BeginAcceptTcpClient(TCPConnectCallback, null);
            int onthisip = 0;
            for (int i = 1; i <= MaxPlayers; i++)
                if (clients[i].tcp.socket != null)
                    if (clients[i].tcp.socket.Client.RemoteEndPoint.ToString().Split(':')[0] == _client.Client.RemoteEndPoint.ToString().Split(':')[0])
                        onthisip++;
            if(onthisip >= Program.config.iplimit)
            {
                Program.Log($"{_client.Client.RemoteEndPoint} connected from the same ip more than {Program.config.iplimit} times, dropped.");
                _client.Close();
                return;
            }
            Program.Log($"Incoming connection from {_client.Client.RemoteEndPoint}");
            for(int i = 1; i <= MaxPlayers; i++)
                if(clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            _client.Close();
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= MaxPlayers; i++)
                clients.Add(i, new Client(i));
            packetHandlers = new Dictionary<int, PacketHandler>
            {
                { (int)PacketID.welcome,        ServerHandle.WelcomeReceived },
                { (int)PacketID.enterScene,     ServerHandle.EnterScene },
                { (int)PacketID.leaveScene,     ServerHandle.LeaveScene },
                { (int)PacketID.clientInfo,     ServerHandle.GetClientInfo },
                { (int)PacketID.clientMove,     ServerHandle.ClientMove },
                { (int)PacketID.chat,           ServerHandle.Chat },
                { (int)PacketID.finishLevel,    ServerHandle.FinishLevel },
                { (int)PacketID.ping,           ServerHandle.Ping },
                { (int)PacketID.rcon,           ServerHandle.Rcon },
                { (int)PacketID.changeGun,      ServerHandle.ChangeGun },
                { (int)PacketID.changeGrapple,  ServerHandle.ChangeGrapple },
            };
        }
    }
}
