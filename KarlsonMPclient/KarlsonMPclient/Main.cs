﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using HarmonyLib;

namespace KarlsonMPclient
{
    
    public class Main
    {
		public static string test = "test";

		public const string GUID = "me.devilexe.karlsonmp";
		public static Harmony Harmony;
        public static void Start()
        {
			SceneManager.sceneLoaded += OnSceneLoaded;
			if(Client.instance == null)
            {
				Client.instance = new Client();
				Client.instance.Start();
            }
			usernameField = DataSave.Load(); // returns username, and loads ip history
			if (DataSave.IpHistory.Count > 0)
				ipField = DataSave.IpHistory.Last(); // load last connected to ip
			//SceneManager.LoadScene("0Tutorial");
			Harmony = new Harmony(GUID);
			Harmony.Patch(typeof(Enemy).GetMethod("LateUpdate"), prefix: new HarmonyMethod(typeof(HarmonyHooks).GetMethod("Enemy_LateUpdate")));
		}

		private static string oldScene = "";

		static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			// we only need to send to the server entering/leaving levels
			if (scene.name == "0Tutorial" && !PrefabInstancer.initialized)
			{
				PrefabInstancer.LoadPrefabs();
				return;
			}
			if (Client.instance.isConnected)
				Client.instance.players.Clear();
			if (oldScene != "" && oldScene != "Initialize" && oldScene != "MainMenu" && Client.instance.isConnected)
				ClientSend.LeaveScene(oldScene);
			if (scene.name != "Initialize" && scene.name != "MainMenu" && Client.instance.isConnected)
				ClientSend.EnterScene(scene.name);
			oldScene = scene.name;
		}

		public static void OnGUI()
        {
			GUI.Label(new Rect(0f, 0f, 1000f, 1000f), "<size=30><b>WORK FFS</b></size>");
			/*if (SceneManager.GetActiveScene().name == "MainMenu" || UIManger.Instance.deadUI.activeSelf || UIManger.Instance.winUI.activeSelf)
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
				string connButtonStr = "Connect";
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
				}
				if(historyShown)
                {
					// TODO: add history list (I already have something like this, just need to port it)
                }
			}
			if(Client.instance.isConnected && SceneManager.GetActiveScene().buildIndex >= 2)
            {

            }*/
		}

		private static string usernameField = "";
		private static string ipField = "";
		private static bool historyShown = false;

		public static void Update(float deltaTime) {
			ThreadManager.UpdateMain();
			PosSender.Update();
		}
        public static void FixedUpdate(float fixedDeltaTime) { }
        public static void OnApplicationQuit()
		{
			Client.instance.Disconnect();
		}
    }
}
