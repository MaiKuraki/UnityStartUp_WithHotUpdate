using Nakama;
using Newtonsoft.Json;

namespace CycloneGames.Networking
{
    public class MultiplayerMessage
    {
        #region FIELDS

        private string jsonStr = null;
        private byte[] bytes = null;

        #endregion

        public long DataCode { get; private set; }
        public string SessionId { get; private set; }
        public string UserId { get; private set; }
        public string Username { get; private set; }
        public IUserPresence UserPresence { get; private set; }

        public MultiplayerMessage(IMatchState matchState)
        {
            DataCode = matchState.OpCode;
            if (matchState.UserPresence != null)
            {
                UserPresence = matchState.UserPresence;
                UserId = matchState.UserPresence.UserId;
                SessionId = matchState.UserPresence.SessionId;
                Username = matchState.UserPresence.Username;
            }

            var encoding = System.Text.Encoding.UTF8;
            // TODO: jsonString is not a valid JSON because of FlatBuffer, maybe you should not use json
            jsonStr = encoding.GetString(matchState.State);    
            bytes = matchState.State;
        }

        public T GetData<T>()
        {
            return JsonConvert.DeserializeObject<T>(jsonStr);
        }

        public byte[] GetBytes()
        {
            return bytes;
        }
    }
}