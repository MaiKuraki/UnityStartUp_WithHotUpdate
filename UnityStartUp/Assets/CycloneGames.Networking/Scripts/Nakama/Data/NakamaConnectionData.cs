using System.Runtime.Serialization;

namespace CycloneGames.Networking.Data
{
    [System.Serializable]
    public class NakamaConnectionData
    {
        [DataMember(Name = "AppVersion")] public string appVersion = null;
        [DataMember(Name = "ServerScheme")] public string serverScheme = null;
        [DataMember(Name = "ServerHost")] public string serverHost = null;
        [DataMember(Name = "ServerPort")] public int serverPort = default(int);
        [DataMember(Name = "ServerKey")] public string serverKey = null;
        [DataMember(Name = "ResHost")] public string resHost = null;
    }

    public enum ServerType
    {
        INNER = 0,
        DEV = 1,
    }
}