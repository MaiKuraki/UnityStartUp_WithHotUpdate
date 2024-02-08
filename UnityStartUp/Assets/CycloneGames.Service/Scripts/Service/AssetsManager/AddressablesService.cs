using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

namespace CycloneGames.Service
{
    public interface IAddressablesService
    {
        UniTask<TResultObject> LoadAssetAsync<TResultObject>(string key, AddressablesManager.AssetHandleReleasePolicy releasePolicy,
            CancellationToken cancellationToken = default) where TResultObject : UnityEngine.Object;

        void ReleaseAssetHandle(AsyncOperationHandle handle);

        bool IsServiceReady();
    }

    public class AddressablesService : IInitializable, IAddressablesService
    {
        private const string DEBUG_FLAG = "[AddressablesService]";
        [Inject] private IServiceDisplay serviceDisplay;
        private GameObject addressablesManagerGO;
        private AddressablesManager addressablesManager;

        public void Initialize()
        {
            addressablesManagerGO = new GameObject("AddressablesManager");
            addressablesManagerGO.transform.SetParent(serviceDisplay.ServiceDisplayTransform);
            addressablesManager = addressablesManagerGO.AddComponent<AddressablesManager>();
        }
        
        public UniTask<TResultObject> LoadAssetAsync<TResultObject>(string key, AddressablesManager.AssetHandleReleasePolicy releasePolicy,
            CancellationToken cancellationToken = default) where TResultObject : Object
        {
            if (addressablesManager == null)
            {
                throw new System.InvalidOperationException($"{DEBUG_FLAG} AddressablesManager is not initialized.");
            }
            return addressablesManager.LoadAssetAsync<TResultObject>(key, releasePolicy, cancellationToken);
        }

        public void ReleaseAssetHandle(AsyncOperationHandle handle)
        {
            if (!handle.IsValid())
            {
                Debug.LogWarning($"{DEBUG_FLAG} Attempting to release an invalid handle.");
                return;
            }
            Addressables.Release(handle);
        }

        public bool IsServiceReady()
        {
            return addressablesManager != null;
        }
    }
}