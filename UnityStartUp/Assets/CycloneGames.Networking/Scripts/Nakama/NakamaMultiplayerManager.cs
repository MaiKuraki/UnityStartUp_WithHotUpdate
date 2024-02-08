using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nakama;
using Zenject;

namespace CycloneGames.Networking
{
    public interface INakamaMultiplayerManager
    {
        event System.Action OnMatchJoin;
        event System.Action OnMatchLeave;
        event System.Action OnLocalTick;
        event System.Action<IMatch> OnReceivedMatchmakerMatchedEvt;
        event System.Action<IMatchPresenceEvent> OnReceivedMatchPresenceEvt;
        IUserPresence Self { get; }
        bool IsOnMatch { get; }
        UniTask JoinMatchAsync();
        UniTask LeaveMatchAsync();
        void EnableLog(bool bNewEnable);
        void Subscribe(long code, System.Action<MultiplayerMessage> action);
        void UnSubscribe(long code, System.Action<MultiplayerMessage> action);
        UniTask Send(long code, object data);
        UniTask Send(long code, byte[] byteData);
        UniTask AutoMatchAsync(int minPlayers = 2);
        UniTask CancelMatchmaking();
    }
    public class NakamaMultiplayerManager : INakamaMultiplayerManager, IInitializable, IDisposable
    {
        private const string DEBUG_FLAG = "<color=cyan>[MultiplayerManager]</color>";
        
        [Inject] private INakamaNetworkService nakamaNetworkService;

        private const int TickRate = 5;
        private const int SendRate = (int)(1000.0 / TickRate);
        private const string JoinOrCreateMatchRpc = "JoinOrCreateMatchRpc";
        
        //  Log
        private const string LogFormat = "{0} with code {1}:\n{2}";
        private const string SendingDataLog = "Sending data";
        private const string ReceivedDataLog = "Received data";

        public event System.Action OnMatchJoin;
        public event System.Action OnMatchLeave;
        public event System.Action OnLocalTick;
        public event System.Action<IMatch> OnReceivedMatchmakerMatchedEvt;
        public event System.Action<IMatchPresenceEvent> OnReceivedMatchPresenceEvt;

        public IUserPresence Self => localUser;
        public bool IsOnMatch { get; }
        
        private Dictionary<long, System.Action<MultiplayerMessage>> OnReceiveData = new Dictionary<long, System.Action<MultiplayerMessage>>();
        private IMatch match = null;
        private bool bEnableLog = false;
        private CancellationTokenSource Cancel_LocalTick;

        private string savedMatchmakingTicket;
        private IUserPresence localUser;

        private bool isDisposed = false;
        public void Initialize()
        {
            if (Cancel_LocalTick != null)
            {
                if(Cancel_LocalTick.Token.CanBeCanceled) Cancel_LocalTick.Cancel();
                Cancel_LocalTick.Dispose();
            }

            Cancel_LocalTick = new CancellationTokenSource();
            LocalTickAsync(Cancel_LocalTick.Token).Forget();
        }

        async UniTask LocalTickAsync(CancellationToken cancelToken)
        {
            while (!isDisposed)
            {
                await UniTask.Delay(SendRate, DelayType.Realtime, PlayerLoopTiming.Update, cancelToken);
                LocalTickPassed();
            }
        }
        
        public async UniTask JoinMatchAsync()
        {
            nakamaNetworkService.Socket.ReceivedMatchState -= OnReceivedMatchState;
            nakamaNetworkService.Socket.ReceivedMatchState += OnReceivedMatchState;
            nakamaNetworkService.OnDisconnected += OnDisconnected;
            IApiRpc rpcResult = await nakamaNetworkService.SendRPC(JoinOrCreateMatchRpc);
            string matchId = rpcResult.Payload;
            match = await nakamaNetworkService.Socket.JoinMatchAsync(matchId);
            OnMatchJoin?.Invoke();
        }

        public async UniTask LeaveMatchAsync()
        {
            nakamaNetworkService.OnDisconnected -= OnDisconnected;
            nakamaNetworkService.Socket.ReceivedMatchState -= OnReceivedMatchState;
            await nakamaNetworkService.Socket.LeaveMatchAsync(match);
            match = null;
            OnMatchLeave?.Invoke();
        }

        public void EnableLog(bool bNewEnable)
        {
            bEnableLog = bNewEnable;
        }

        public void Subscribe(long code, System.Action<MultiplayerMessage> action)
        {
            if (!OnReceiveData.ContainsKey(code))
            {
                OnReceiveData.Add(code, null);
            }

            OnReceiveData[code] += action;
        }

        public void UnSubscribe(long code, System.Action<MultiplayerMessage> action)
        {
            if (OnReceiveData.ContainsKey(code))
            {
                OnReceiveData[code] -= action;
            }
        }

        public async UniTask Send(long code, object data)
        {
            if (match == null) return;

            string json = data != null ? data.ToString() : string.Empty;
            if (bEnableLog)
            {
                LogData(SendingDataLog, code, json);
            }
            await nakamaNetworkService.Socket.SendMatchStateAsync(match.Id, (long)code, json);
        }

        public async UniTask Send(long code, byte[] byteData)
        {
            if (match == null) return;
            
            if (byteData != null)
            {
                await nakamaNetworkService.Socket.SendMatchStateAsync(match.Id, (long)code, byteData);
                if (bEnableLog)
                {
                    LogDataCode(SendingDataLog, code);
                }
            }
            else
            {
                UnityEngine.Debug.Log($"{DEBUG_FLAG} Invalid nakama net data");
            }
        }

        public async UniTask AutoMatchAsync(int minPlayers = 2)
        {
            nakamaNetworkService.Socket.ReceivedMatchState -= OnReceivedMatchState;
            nakamaNetworkService.Socket.ReceivedMatchState += OnReceivedMatchState;
            nakamaNetworkService.OnDisconnected += OnDisconnected;
            
            nakamaNetworkService.Socket.ReceivedMatchmakerMatched -= OnReceivedMatchmakerMatched;
            nakamaNetworkService.Socket.ReceivedMatchmakerMatched += OnReceivedMatchmakerMatched;
            nakamaNetworkService.Socket.ReceivedMatchPresence -= OnReceivedMatchPresence;
            nakamaNetworkService.Socket.ReceivedMatchPresence += OnReceivedMatchPresence;

            var matchmakingPorperties = new Dictionary<string, string>
            {
                { "engine", "unity" }
            };
            
            var matchmakingTicket = await nakamaNetworkService.Socket.AddMatchmakerAsync(query: "+properties.engine:unity", minCount: minPlayers, maxCount: minPlayers, stringProperties: matchmakingPorperties);
            savedMatchmakingTicket = matchmakingTicket.Ticket;
            UnityEngine.Debug.Log($"{DEBUG_FLAG} MatchTicket: {savedMatchmakingTicket}");
        }

        public async UniTask CancelMatchmaking()
        {
            await nakamaNetworkService.Socket.RemoveMatchmakerAsync(savedMatchmakingTicket);
        }

        private void OnReceivedMatchState(IMatchState newState)
        {
            if (bEnableLog)
            {
                LogDataCode(ReceivedDataLog, newState.OpCode);
            }

            MultiplayerMessage msg = new MultiplayerMessage(newState);
            if (OnReceiveData.ContainsKey(msg.DataCode))
            {
                OnReceiveData[msg.DataCode]?.Invoke(msg);
            }
        }
        
        private void OnDisconnected()
        {
            nakamaNetworkService.OnDisconnected -= OnDisconnected;
            nakamaNetworkService.Socket.ReceivedMatchState -= OnReceivedMatchState;
            match = null;
            OnMatchLeave?.Invoke();
        }

        private void LocalTickPassed()
        {
            OnLocalTick?.Invoke();
        }
        
        private void LogData(string description, long dataCode, string json)
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} {string.Format(LogFormat, description, dataCode, json)}");
        }

        private void LogDataCode(string description, long dataCode)
        {
            UnityEngine.Debug.Log($"{DEBUG_FLAG} {string.Format(LogFormat, description, dataCode)}");
        }

        private async void OnReceivedMatchmakerMatched(IMatchmakerMatched matchmakerMatched)
        {
            localUser = matchmakerMatched.Self.Presence;
            match = await nakamaNetworkService.Socket.JoinMatchAsync(matchmakerMatched);
            OnReceivedMatchmakerMatchedEvt?.Invoke(match);
            UnityEngine.Debug.Log($"{DEBUG_FLAG} ReceiveMatchmakerMatched, matchID: {match.Id}");
        }

        private void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
        {
            OnReceivedMatchPresenceEvt?.Invoke(matchPresenceEvent);
            IUserPresence joinData = matchPresenceEvent.Joins.FirstOrDefault();
            IUserPresence leavesData = matchPresenceEvent.Leaves.FirstOrDefault();
            UnityEngine.Debug.Log($"{DEBUG_FLAG} ReceivedMatchPresence, matchID: {matchPresenceEvent.MatchId}, joins:{joinData?.Status} {joinData?.UserId}, leaves:{leavesData?.Status} {leavesData?.UserId}");
        }

        public void Dispose()
        {
            if(Cancel_LocalTick is { Token: { CanBeCanceled: true } }) Cancel_LocalTick.Cancel();
            Cancel_LocalTick.Dispose();
            isDisposed = true;
        }
    }    
}

