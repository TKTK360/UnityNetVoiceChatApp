﻿using UnityEngine;
using UnityEngine.UI;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// クライアント接続した際に、MLAPIからのコールバックを管理して切断時等の処理をします
    /// </summary>
    public class ClientManager : MonoBehaviour
    {
        public GmeVoiceChatScript voiceLoginScript;
        public Button stopButton;
        public GameObject configureObject;
        private bool previewConnected;

        /// <summary>
        /// Start
        /// </summary>
        private void Start()
        {
            
        }

        /// <summary>
        /// Setup
        /// </summary>
        public void Setup()
        {
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnect;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }

        /// <summary>
        /// ReoveCallbacks
        /// </summary>
        private void ReoveCallbacks()
        {
            Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnect;
            Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
        }

        /// <summary>
        /// Disconnect
        /// </summary>
        private void Disconnect()
        {
#if ENABLE_AUTO_CLIENT
            // クライアント接続時に切断したらアプリ終了させます
            if (NetworkUtility.IsBatchModeRun)
            {
                Application.Quit();
            }
#endif
            // UIを戻します
            configureObject.SetActive(true);
            stopButton.gameObject.SetActive(false);
            stopButton.onClick.RemoveAllListeners();
            // コールバックも削除します
            ReoveCallbacks();
        }

        private void OnClickStopButton()
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
            Disconnect();

            StopVoiceServer();
        }

        private void OnClientConnect(ulong clientId)
        {
            Debug.Log("Connect Client:" + clientId + "::" + Unity.Netcode.NetworkManager.Singleton.LocalClientId);
        }
        private void OnClientDisconnect(ulong clientId)
        {
            Debug.Log("Disconnect Client: " + clientId);
        }

        // 自信が接続した時に呼び出されます
        private void OnConnectSelf()
        {
            configureObject.SetActive(false);

            stopButton.GetComponentInChildren<Text>().text = "Disconnect";
            stopButton.onClick.AddListener(this.OnClickStopButton);
            stopButton.gameObject.SetActive(true);
        }

        private void Update()
        {
            var netMgr = Unity.Netcode.NetworkManager.Singleton;
            var currentConnected = netMgr.IsConnectedClient;
            // 3人以上接続時に切断が呼び出されないので対策
            if (currentConnected != previewConnected)
            {
                if (!currentConnected)
                {
                    Disconnect();
                }
                else
                {
                    OnConnectSelf();
                }
            }

            previewConnected = netMgr.IsConnectedClient;
        }

        /// <summary>
        /// StopVoiceServer
        /// </summary>
        protected void StopVoiceServer()
        {
            Debug.Log("Client StopVoiceServer");
            voiceLoginScript?.GmeClose();
        }

    }
}