using UnityEngine;
using System.IO;

namespace UTJ.NetcodeGameObjectSample
{
    /// <summary>
    /// 接続先を保存するための設定
    /// </summary>
    [System.Serializable]
    public class ConnectInfo
    {
        //接続先IPアドレス
        [SerializeField]
        public string ipAddr;
        // ポート番号
        [SerializeField]
        public int port;
        //リレーサーバーの有無
        [SerializeField]
        public bool useRelay;
        // Relayの参加コード(毎回変わるのでシリアライズ対象外
        [System.NonSerialized]
        public string relayCode;

        // プレイヤー名
        [SerializeField]
        public string playerName;

        /// <summary>
        /// GetDefault
        /// </summary>
        /// <returns></returns>

        public static ConnectInfo GetDefault()
        {
            var info = new ConnectInfo() {
                useRelay = false,
                ipAddr = "127.0.0.1",
                port = 7777,
                playerName = string.Format("大鳥こはく{0}", System.DateTime.Now.Millisecond)
            };
            return info;
        }

        /// <summary>
        /// ConfigFile
        /// </summary>
        private static string ConfigFile
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                return "connectInfo.json";
#else
                return Path.Combine(Application.persistentDataPath, "connectInfo.json");
#endif
            }
        }

        /// <summary>
        /// LoadFromFile
        /// </summary>
        /// <returns></returns>
        public static ConnectInfo LoadFromFile()
        {
            var configFilePath = ConfigFile;
            if (!File.Exists(configFilePath)) {
                return GetDefault();
            }
            var jsonStr = File.ReadAllText(configFilePath);
            var connectInfo = JsonUtility.FromJson<ConnectInfo>(jsonStr);
            return connectInfo;
        }

        /// <summary>
        /// SaveToFile
        /// </summary>
        public void SaveToFile()
        {
            File.WriteAllText(ConfigFile, JsonUtility.ToJson(this));
        }
    }
}