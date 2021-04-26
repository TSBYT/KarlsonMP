﻿using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KarlsonMP
{
    public class Main : MelonMod
    {
        public override void OnApplicationLateStart()
        {
            if (Client.instance == null)
            {
                Client.instance = new Client();
                Client.instance.Start();
            }
            usernameField = DataSave.Load(); // returns username, and loads ip history
            if (DataSave.IpHistory.Count > 0)
                ipField = DataSave.IpHistory.Last(); // load last connected to ip
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static string oldScene = "";
        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if(scene.name == "MainMenu" && !PrefabInstancer.Initialized)
            {
                SceneManager.LoadScene("0Tutorial");
                return;
            }
            if (scene.name == "0Tutorial" && !PrefabInstancer.Initialized)
            {
                PrefabInstancer.LoadPrefabs();
                return;
            }
            if (Client.instance.isConnected)
                Client.instance.players.Clear();
            // we only need to send to the server entering/leaving levels
            if (oldScene != "" && oldScene != "Initialize" && oldScene != "MainMenu" && Client.instance.isConnected)
                ClientSend.LeaveScene(oldScene);
            if (scene.name != "Initialize" && scene.name != "MainMenu" && Client.instance.isConnected)
                ClientSend.EnterScene(scene.name);
            oldScene = scene.name;
        }

        private static string usernameField = "";
        private static string ipField = "";
        private static bool historyShown = false;
        private static Vector2 ipHistoryScroll = Vector2.zero;
        public override void OnGUI()
        {
            if (SceneManager.GetActiveScene().name == "MainMenu" || UIManger.Instance.deadUI.activeSelf || UIManger.Instance.winUI.activeSelf)
			{
				GUI.Box(new Rect(Screen.width / 2 - 150f, Screen.height - 40f, 300f, 40f), "");
				GUI.Label(new Rect(Screen.width / 2 - 150f, Screen.height - 40f, 300f, 40f), "Username");
				usernameField = GUI.TextField(new Rect(Screen.width / 2 - 85f, Screen.height - 40f, 155f, 20f), usernameField);
				GUI.Label(new Rect(Screen.width / 2 - 150f, Screen.height - 20f, 300f, 40f), "IP");
				ipField = GUI.TextField(new Rect(Screen.width / 2 - 130f, Screen.height - 20f, 180f, 20f), ipField);
				if (GUI.Button(new Rect(Screen.width / 2 + 50f, Screen.height - 20f, 20f, 20f), "^"))
				{
					historyShown = !historyShown;
				}
				string connButtonStr = "Connect"; // button doesn't work for some reason 
				if (Client.instance.isConnected)
					connButtonStr = "Leave";
				if (Client.instance.isConnecting)
					connButtonStr = "Cancel";
				if (GUI.Button(new Rect(Screen.width / 2 + 70f, Screen.height - 40f, 80f, 40f), connButtonStr))
				{
					if(Client.instance.isConnected)
                    {
						Client.instance.tcp.Disconnect();
						return;
                    }
					if(Client.instance.isConnecting)
                    {
						// TODO: cancel the timeout timer
						Client.instance.Disconnect();
						return;
                    }
					// parse ip
					if (ipField.Split(':').Length != 2)
						return;
                    if (!int.TryParse(ipField.Split(':')[1], out int port))
                        return;
                    Client.instance.ip = Dns.GetHostAddresses(ipField.Split(':')[0])[0].MapToIPv4().ToString();
					Client.instance.port = port;
					Client.instance.username = usernameField;
					Client.instance.ConnectToServer();
                    DataSave.AddToList(ipField);
                    DataSave.Save(usernameField);
				}
				if(historyShown)
                {
                    GUI.Box(new Rect(Screen.width / 2 - 130f, Screen.height - 340f, 200f, 300f), "");
                    GUI.BeginScrollView(new Rect(Screen.width / 2 - 130f, Screen.height - 340f, 200f, 300f), ipHistoryScroll, new Rect(0f, 0f, 200f, DataSave.IpHistory.Count * 50f));
                    int i = 0;
                    GUIStyle ipButton = new GUIStyle();
                    ipButton.normal.background = Texture2D.blackTexture;
                    ipButton.alignment = TextAnchor.MiddleLeft;
                    ipButton.fontSize = 20;
                    foreach(string _ip in DataSave.IpHistory.ToArray().Reverse())
                    {
                        string color = "<color=white>";
                        if (ipField == _ip)
                            color = "<color=green>";
                        if(GUI.Button(new Rect(0f, i * 50f, 140f, 40f), color + _ip + "</color>"))
                            ipField = _ip;
                        if (GUI.Button(new Rect(140f, i * 50f, 60f, 20f), "<color=red>Remove</color>"))
                            DataSave.Remove(DataSave.IpHistory.Count - 1 - i); // we fliped the list when showing it, we need to shout count - 1 - i | count - 1 => last index, - i => flips it, last being first, first being last
                        if (GUI.Button(new Rect(140f, i * 50f + 20f, 30f, 20f), "▲"))
                            DataSave.MoveUp(DataSave.IpHistory.Count - 1 - i); // see comment above
                        if (GUI.Button(new Rect(170f, i * 50f + 20f, 30f, 20f), "▼"))
                            DataSave.MoveDown(DataSave.IpHistory.Count - 1 - i); // see comment above
                        i++;
                    }
                    GUI.EndScrollView(true);
                }
			}
			if(Client.instance.isConnected && SceneManager.GetActiveScene().buildIndex >= 2)
            {
                foreach(OnlinePlayer player in Client.instance.players.Values)
                {
                    string text = "(" + player.id + ") " + player.username;
                    
                    Vector3 pos = Camera.main.WorldToScreenPoint(player.pos + new Vector3(0f, 2.0f, 0f));
                    if (Vector3.Distance(player.pos, PlayerMovement.Instance.transform.position) >= 150f)
                        continue; // player is too far
                    if (pos.z < 0)
                        continue; // point is behind our camera
                    Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(text));
                    textSize.x += 10f;
                    GUI.Box(new Rect(pos.x - textSize.x / 2, (Screen.height - pos.y) - textSize.y / 2, textSize.x, textSize.y), text);
                }
            }
        }

        public override void OnUpdate()
        {
            ThreadManager.UpdateMain();
            PosSender.Update();
        }
        public override void OnApplicationQuit()
        {
            Client.instance.Disconnect();
        }
    }
}
