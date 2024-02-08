using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace CycloneGames.Service
{
    public class AddressablesManager : MonoBehaviour
    {
        private const string DEBUG_FLAG = "[AddressablesManager]";

        public enum AssetHandleReleasePolicy
        {
            Keep,
            ReleaseOnComplete
        }

        // Loads an asset asynchronously and returns a UniTask.
        public UniTask<TResultObject> LoadAssetAsync<TResultObject>(string key, AssetHandleReleasePolicy releasePolicy,
            CancellationToken cancellationToken = default) where TResultObject : UnityEngine.Object
        {
            var completionSource = new UniTaskCompletionSource<TResultObject>();
            var operationHandle = Addressables.LoadAssetAsync<TResultObject>(key);

            operationHandle.Completed += operation =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled();
                    ReleaseAssetHandleIfNeeded(operationHandle, releasePolicy);
                    return;
                }

                try
                {
                    if (operation.Status == AsyncOperationStatus.Succeeded)
                    {
                        completionSource.TrySetResult(operation.Result);
                    }
                    else
                    {
                        var errorMessage = $"Failed to load the asset with key {key}. Status: {operation.Status}";
                        if (operation.OperationException != null)
                        {
                            errorMessage += $", Exception: {operation.OperationException.Message}";
                            throw operation.OperationException; // This preserves stack trace.
                        }

                        throw new Exception(errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{DEBUG_FLAG} Exception occurred: {ex.Message}");
                    completionSource.TrySetException(ex);
                }
                finally
                {
                    ReleaseAssetHandleIfNeeded(operationHandle, releasePolicy);
                }
            };

            return completionSource.Task;
        }

        // Releases an asset using its handle if the policy dictates.
        private void ReleaseAssetHandleIfNeeded(AsyncOperationHandle handle, AssetHandleReleasePolicy releasePolicy)
        {
            if (releasePolicy == AssetHandleReleasePolicy.ReleaseOnComplete && handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        // Cancels the asset load operation if the CancellationToken is invoked.
        private void RegisterForCancellation(AsyncOperationHandle handle, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                if (!handle.IsDone)
                {
                    Addressables.Release(handle);
                }
            });
        }
    }
}