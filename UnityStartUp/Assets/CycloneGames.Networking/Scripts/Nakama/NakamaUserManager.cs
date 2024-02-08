using System;
using Cysharp.Threading.Tasks;
using Nakama;
using Zenject;

namespace CycloneGames.Networking
{
    public interface INakamaUserData
    {
        IApiAccount Account { get; }
        IApiUser User { get; }
        string DisplayName { get; }
    }

    public interface INakamaUserManager
    {
        bool IsDataRefreshed { get; }
        UniTask RequestPlayerData(Action<IApiAccount> OnGetPlayerData = null);
    }

    public class NakamaUserManager : IInitializable, IDisposable, INakamaUserData, INakamaUserManager
    {
        private const string DEBUG_FLAG = "[NakamaUserManager]";
        
        [Inject] private INakamaNetworkService nakamaNetworkService;
        
        public IApiAccount Account => account;
        public IApiUser User => account?.User;
        public string DisplayName => account?.User?.DisplayName;
        public bool IsDataRefreshed => _dataRefreshed;
        
        public async UniTask RequestPlayerData(Action<IApiAccount> OnGetPlayerData = null)
        {
            _dataRefreshed = false;
            account = await nakamaNetworkService.Client.GetAccountAsync(nakamaNetworkService.Session);
            OnGetPlayerData?.Invoke(account);
            InternalOnGetPlayerData(account);
        }

        private IApiAccount account = null;
        private bool _dataRefreshed = false;
        
        public void Initialize()
        {
            nakamaNetworkService.OnLoginSuccess += AutoLoad;
        }
        
        public void Dispose()
        {
            nakamaNetworkService.OnLoginSuccess -= AutoLoad;
        }
        async void AutoLoad()
        {
            await RequestPlayerData();
            _dataRefreshed = true;
        }
        
        void InternalOnGetPlayerData(IApiAccount dataFromServer)
        {
            _dataRefreshed = true;
        }
    }
}