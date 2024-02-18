using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Zenject;

namespace CycloneGames.HotUpdate
{
    public enum AssetHandleReleasePolicy
    {
        Keep,
        ReleaseOnComplete
    }

    public interface IYooAssetService
    {
        public void RunHotUpdateTasks(EPlayMode playMode);
        event Action RunHotUpdateDone;

        void RegisterHotUpdateDownloadCallback(Action<string> OnHotUpdateStartDownload,
            Action<string> OnHotUpdateFinishDownload, Action<float> OnHotUpdateProgressUpdate);

        void UnRegisterHotUpdateDownloadCallback(Action<string> OnHotUpdateStartDownload,
            Action<string> OnHotUpdateFinishDownload, Action<float> OnHotUpdateProgressUpdate);

        void TryUnloadUnusedAsset(string location);
        void TryUnloadUnusedRawFile(string location);
        void UnloadUnusedAssets();

        UniTask<Scene> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single,
            bool suspendLoad = false, uint priority = 100, CancellationToken cancellationToken = default);

        UniTask<TObject> LoadAssetAsync<TObject>(string location,
            AssetHandleReleasePolicy releasePolicy,
            uint priority = 0,
            CancellationToken cancellationToken = default) where TObject : UnityEngine.Object;

        public UniTask<T> LoadRawFileAsync<T>(string location, AssetHandleReleasePolicy releasePolicy,
            uint priority = 0,
            CancellationToken cancellationToken = default) where T : class;

        string GetResourcePackageVersion();
        string GetRawFilePackageVersion();
        bool IsServiceReady();
    }

    public class YooAssetService : IYooAssetService, IInitializable
    {
        private GameObject yooAssetGameObject;
        private YooAssetManager yooAssetManager;
        private const string DEBUG_FLAG = "[YooAssetService]";

        public void Initialize()
        {
            yooAssetGameObject = new GameObject("YooAssetService");
            yooAssetManager = yooAssetGameObject.AddComponent<YooAssetManager>();
            GameObject.DontDestroyOnLoad(yooAssetManager);
        }

        public void RunHotUpdateTasks(EPlayMode playMode)
        {
            yooAssetManager?.RunHotUpdateTasks(playMode, RunHotUpdateDone).Forget();
        }

        public event Action RunHotUpdateDone;

        public void UnloadUnusedAssets()
        {
            yooAssetManager?.UnloadUnusedAssets();
        }

        public UniTask<Scene> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false,
            uint priority = 100, CancellationToken cancellationToken = default)
        {
            if (yooAssetManager == null)
            {
                UnityEngine.Debug.LogError($"{DEBUG_FLAG} YooAssetManager is not initialized.");
                return default;
            }

            return yooAssetManager.LoadSceneAsync(location, sceneMode, suspendLoad, priority, cancellationToken);
        }

        public void TryUnloadUnusedAsset(string location)
        {
            yooAssetManager?.TryUnloadUnusedAsset(location);
        }

        public void TryUnloadUnusedRawFile(string location)
        {
            yooAssetManager?.TryUnloadUnusedRawFile(location);
        }

        public UniTask<TObject> LoadAssetAsync<TObject>(string location, AssetHandleReleasePolicy releasePolicy,
            uint priority = 0,
            CancellationToken cancellationToken = default) where TObject : UnityEngine.Object
        {
            if (yooAssetManager == null)
            {
                UnityEngine.Debug.LogError($"{DEBUG_FLAG} YooAssetManager is not initialized.");
                return default;
            }

            return yooAssetManager.LoadAssetAsync<TObject>(location, priority, releasePolicy, cancellationToken);
        }

        public UniTask<T> LoadRawFileAsync<T>(string location,
            AssetHandleReleasePolicy releasePolicy, uint priority = 0,
            CancellationToken cancellationToken = default) where T : class
        {
            if (yooAssetManager == null)
            {
                UnityEngine.Debug.LogError($"{DEBUG_FLAG} YooAssetManager is not initialized.");
                return default;
            }

            return yooAssetManager.LoadRawFileAsync<T>(location, priority, releasePolicy, cancellationToken);
        }

        public string GetResourcePackageVersion()
        {
            string result = yooAssetManager?.GetResourcePackageVersion();
            return string.IsNullOrEmpty(result) ? "Invalid YooAssetManager" : result;
        }

        public string GetRawFilePackageVersion()
        {
            string result = yooAssetManager?.GetRawFilePackageVersion();
            return string.IsNullOrEmpty(result) ? "Invalid YooAssetManager" : result;
        }

        public bool IsServiceReady()
        {
            return yooAssetManager != null;
        }

        public void RegisterHotUpdateDownloadCallback(Action<string> OnHotUpdateStartDownload,
            Action<string> OnHotUpdateFinishDownload,
            Action<float> OnHotUpdateProgressUpdate)
        {
            if (!yooAssetManager)
            {
                Debug.LogError($"{DEBUG_FLAG} YooAssetManager not prepared");
                return;
            }

            yooAssetManager.OnHotUpdateStartDownload -= OnHotUpdateStartDownload;
            yooAssetManager.OnHotUpdateStartDownload += OnHotUpdateStartDownload;

            yooAssetManager.OnHotUpdateFinishDownload -= OnHotUpdateFinishDownload;
            yooAssetManager.OnHotUpdateFinishDownload += OnHotUpdateFinishDownload;

            yooAssetManager.OnHotUpdateProgressUpdate -= OnHotUpdateProgressUpdate;
            yooAssetManager.OnHotUpdateProgressUpdate += OnHotUpdateProgressUpdate;
        }

        public void UnRegisterHotUpdateDownloadCallback(Action<string> OnHotUpdateStartDownload,
            Action<string> OnHotUpdateFinishDownload,
            Action<float> OnHotUpdateProgressUpdate)
        {
            yooAssetManager.OnHotUpdateStartDownload -= OnHotUpdateStartDownload;
            yooAssetManager.OnHotUpdateFinishDownload -= OnHotUpdateFinishDownload;
            yooAssetManager.OnHotUpdateProgressUpdate -= OnHotUpdateProgressUpdate;
        }
    }
}