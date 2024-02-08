using System;
using System.Threading.Tasks;
using Nakama;
using Zenject;

namespace CycloneGames.Networking
{
    public interface INakamaNetworkService
    {
        void SetNakamaConnectionData(NakamaConnectionData newNakamaConnectionData);
        void LoginWithEmail(string emailStr, string passwdStr);
        void LoginAsync(Task<ISession> sessionTask);
        Task<IApiRpc> SendRPC(string rpc, string payload = "{}");
        void LogOut();
        IClient Client { get; }
        ISession Session { get; }
        ISocket Socket { get; }
        event Action OnConnecting;
        event Action OnConnected;
        event Action OnDisconnected;
        event Action OnLoginSuccess;
        event Action OnLoginFail;
    }
    public class NakamaNetworkService : INakamaNetworkService, IInitializable, IDisposable
    {
        public const string DEBUG_FLAG = "<color=cyan>[NakamaNetworkService]</color>";
        public IClient Client => client;
        public ISession Session => session;
        public ISocket Socket => socket;

        private IClient client = null;
        private ISession session = null;
        private ISocket socket = null;

        public event Action OnConnecting;
        public event Action OnConnected = null;
        public event Action OnDisconnected = null;
        public event Action OnLoginSuccess = null;
        public event Action OnLoginFail = null;
        
        private NakamaConnectionData savedNakamaConnectionData;
        

        public void Initialize()
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} Init Nakama");

            OnConnecting += LOG_OnConnecting;
            OnConnected += LOG_OnConnected;
            OnDisconnected += LOG_OnDisconnected;
            OnLoginSuccess += LOG_OnLoginSuccess;
            OnLoginFail += LOG_OnLoginFail;
        }
        
        public void Dispose()
        {
            OnConnecting -= LOG_OnConnecting;
            OnConnected -= LOG_OnConnected;
            OnDisconnected -= LOG_OnDisconnected;
            OnLoginSuccess -= LOG_OnLoginSuccess;
            OnLoginFail -= LOG_OnLoginFail;
            
            if (socket != null)
            {
                UnityEngine.Debug.Log($"{DEBUG_FLAG} close socket connection");
                socket.CloseAsync();
            }
        }

        public void SetNakamaConnectionData(NakamaConnectionData newNakamaConnectionData)
        {
            savedNakamaConnectionData = newNakamaConnectionData;
        }

        public async void LoginAsync(Task<ISession> sessionTask)
        {
            OnConnecting?.Invoke();

            try
            {
                session = await sessionTask;
                socket = client.NewSocket(true);
                socket.Connected += Connected;
                socket.Closed += Disconnected;
                await socket.ConnectAsync(session);
                OnLoginSuccess?.Invoke();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);
                OnLoginFail?.Invoke();
            }
        }

        public async void LogOut()
        {
            if (socket != null)
            {
                await socket.CloseAsync();
                socket = null;
            }
        }
        
        void Connected()
        {
            OnConnected?.Invoke();
        }

        void Disconnected()
        {
            OnDisconnected?.Invoke();
        }

        public async Task<IApiRpc> SendRPC(string rpc, string payload = "{}")
        {
            if (client == null || session == null) return null;
            return await client.RpcAsync(session, rpc, payload);
        }

        public void LoginWithEmail(string emailStr, string passwdStr)
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} Login with email, email: {emailStr}");
            client = new Client(savedNakamaConnectionData.ServerScheme, savedNakamaConnectionData.ServerHost, savedNakamaConnectionData.ServerPort, savedNakamaConnectionData.ServerKey, UnityWebRequestAdapter.Instance);
            LoginAsync(client.AuthenticateEmailAsync(emailStr, passwdStr, null, true));
        }

        private void LOG_OnConnecting()
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} OnConnecting");
        }
        private void LOG_OnConnected()
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} OnConnected");
        }
        private void LOG_OnDisconnected()
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} OnDisconnected");
        }
        private void LOG_OnLoginSuccess()
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} OnLoginSuccess");
        }
        private void LOG_OnLoginFail()
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} OnLoginFail");
        }
    }
}