using UnityEngine;

namespace CycloneGames.Networking
{
    [CreateAssetMenu(menuName = "Nakama/ConnectionData")]
    public class NakamaConnectionData : ScriptableObject
    {
        private Data.NakamaConnectionData data = null;

        public string AppVersion
        {
            get => data?.appVersion;
        }
        
        public string ServerScheme
        {
            get => data?.serverScheme;
        }

        public string ServerHost
        {
            get => data?.serverHost;
        }

        public int ServerPort
        {
            get => data?.serverPort ?? default(int);
        }

        public string ServerKey
        {
            get => data?.serverKey;
        }

        public string ResHost
        {
            get => data?.resHost;
        }

        public void SetData(Data.NakamaConnectionData newData)
        {
            data = newData;
        }
    }
}