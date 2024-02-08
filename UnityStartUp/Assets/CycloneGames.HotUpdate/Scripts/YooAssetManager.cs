using System;
using System.Threading;
using CycloneGames.Utility;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using YooAsset;

namespace CycloneGames.HotUpdate
{
    public class YooAssetManager : MonoBehaviour
    {
        enum EPackageType
        {
            Resource,
            RawFile
        }

        private const string DEBUG_FLAG = "<color=#66ccff>[YooAssetManager]</color>";
        private ResourcePackage resourcePackage;
        private ResourcePackage rawFilePackage;

        private string YooAssetConfigPath =
            "Assets/CycloneGames.HotUpdate/ScriptableObject/ShouldBeModified/YooAssetData.asset";

        private YooAssetData YooAssetConfig;

        private string HotUpdateDLLBasePath = "InvalidPath";

        public event Action<string> OnHotUpdateStartDownload;
        public event Action<string> OnHotUpdateFinishDownload;
        public event Action<float> OnHotUpdateProgressUpdate;

        private void Awake()
        {
            HotUpdateDLLBasePath = $"{Application.persistentDataPath.TrimEnd('/')}/HotUpdateDLL";
        }

        void Initialize()
        {
            // TODO: YooAsset 2.0 can only load RawFile from RawFilePackage Build Pipeline
            // https://github.com/tuyoogame/YooAsset/issues/221

            YooAssets.Initialize();
            Debug.Log($"{DEBUG_FLAG} Initialize YooAsset, Platform: {Application.platform.ToString()}");
            resourcePackage = YooAssets.CreatePackage(YooAssetConfig.GetResourcePackageName(Application.platform));
            rawFilePackage = YooAssets.CreatePackage(YooAssetConfig.GetRawFilePackageName(Application.platform));
        }

        public async UniTask RunHotUpdateTasks(EPlayMode playMode, Action OnCompleted)
        {
            Debug.Log($"{DEBUG_FLAG} Start HotUpdate Pipeline, PlayMode: {playMode}");

            await InitYooAssetConfig();
            Initialize();

            await InitializeYooAsset(rawFilePackage, EPackageType.RawFile, EDefaultBuildPipeline.RawFileBuildPipeline,
                playMode);
            var rawFilePackageVersion = await UpdatePackageVersion(EPackageType.RawFile);
            await UpdatePackageManifest(EPackageType.RawFile, rawFilePackageVersion);
            await Download(EPackageType.RawFile);

            await InitializeYooAsset(resourcePackage, EPackageType.Resource, EDefaultBuildPipeline.BuiltinBuildPipeline,
                playMode);
            var resourcePackageVersion = await UpdatePackageVersion(EPackageType.Resource);
            await UpdatePackageManifest(EPackageType.Resource, resourcePackageVersion);
            await Download(EPackageType.Resource);

            await LoadHotUpdateDll();

            OnCompleted?.Invoke();
        }

        private async UniTask InitYooAssetConfig()
        {
            var request = Addressables.LoadAssetAsync<YooAssetData>(YooAssetConfigPath);
            request.Completed += handle =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"{DEBUG_FLAG} YooAsset Config Load Failed, Path: {YooAssetConfigPath}");
                }
                else
                {
                    YooAssetConfig = handle.Result;
                    Debug.Log(
                        $"{DEBUG_FLAG} YooAsset Config Load Success, ResPath: {handle.Result.GetResFullPath(Application.platform)}");
                }
            };
            await request;

            Addressables.Release(request);
        }

        private async UniTask InitializeYooAsset(ResourcePackage packageRef, EPackageType packageType,
            EDefaultBuildPipeline buildPipeline, EPlayMode playMode)
        {
            //  Initialize Asset System
            InitializeParameters initParameters = null;
            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
                    initParameters = new EditorSimulateModeParameters();
                    string packageNameStr = GetPackageName(packageType);
                    //  TODO: Using enum as first param will cause an error in Editor Build.
                    //var simulateManifastFilePath = EditorSimulateModeHelper.SimulateBuild(BuildPipeline, packageNameStr);
                    var simulateManifastFilePath =
                        EditorSimulateModeHelper.SimulateBuild(buildPipeline.ToString(), packageNameStr);
                    ((EditorSimulateModeParameters)initParameters).SimulateManifestFilePath = simulateManifastFilePath;
                    await packageRef.InitializeAsync(initParameters).Task;
                    break;
                case EPlayMode.OfflinePlayMode:
                    initParameters = new OfflinePlayModeParameters();
                    await packageRef.InitializeAsync(initParameters).Task;
                    break;
                case EPlayMode.HostPlayMode:
                    string defaultHostServer = $"{YooAssetConfig.GetResFullPath(Application.platform)}";
                    string fallbackHostServer = $"{YooAssetConfig.GetResFullPath(Application.platform)}";
                    initParameters = new HostPlayModeParameters();
                    ((HostPlayModeParameters)initParameters).BuildinQueryServices = new YooAssetBuildinQueryServices();
                    ((HostPlayModeParameters)initParameters).RemoteServices =
                        new YooAssetRemoteServices(defaultHostServer, fallbackHostServer);
                    // ((HostPlayModeParameters)initParameters).DecryptionServices = new GameQueryServices();
                    initParameters.CacheFileAppendExtension = true;
                    var initOperation = packageRef.InitializeAsync(initParameters);
                    await initOperation.Task;
                    if (initOperation.Status != EOperationStatus.Succeed)
                    {
                        Debug.Log($"{DEBUG_FLAG} Init asset package Failure");
                        //  TODO: maybe retry
                        return;
                    }

                    Debug.Log($"{DEBUG_FLAG} Init asset package Success");
                    break;
                case EPlayMode.WebPlayMode:
                    break;
            }
        }

        private async UniTask<string> UpdatePackageVersion(EPackageType packageType)
        {
            string packageName = GetPackageName(packageType);

            var pkg = YooAssets.GetPackage(packageName);
            var operation = pkg.UpdatePackageVersionAsync();
            await operation.Task;
            if (operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                string packageVersion = operation.PackageVersion;
                Debug.Log(
                    $"{DEBUG_FLAG} Updated package Version Success, packageName: [{packageName}], version: {packageVersion}");
                return packageVersion;
            }
            else
            {
                //更新失败
                Debug.LogError(operation.Error);
                return "";
            }
        }

        private async UniTask UpdatePackageManifest(EPackageType packageType, string packageVersion)
        {
            string packageName = GetPackageName(packageType);

            // 更新成功后自动保存版本号，作为下次初始化的版本。
            // 也可以通过operation.SavePackageVersion()方法保存。
            bool savePackageVersion = false;
            var pkg = YooAssets.GetPackage(packageName);
            var operation = pkg.UpdatePackageManifestAsync(packageVersion, savePackageVersion);
            await operation.Task;
            if (operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                Debug.Log($"{DEBUG_FLAG} Updated package Manifest Success, packageName: [{packageName}]");
            }
            else
            {
                //更新失败
                Debug.LogError(operation.Error);
            }
        }

        private async UniTask Download(EPackageType packageType)
        {
            string packageName = GetPackageName(packageType);

            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var pkg = YooAssets.GetPackage(packageName);
            var downloader = pkg.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            //没有需要下载的资源
            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log($"{DEBUG_FLAG} No Asset need update, packageName: [{packageName}]");
                return;
            }

            OnHotUpdateStartDownload?.Invoke(packageName);

            //需要下载的文件总数和总大小
            int totalDownloadCount = downloader.TotalDownloadCount;
            long totalDownloadBytes = downloader.TotalDownloadBytes;

            downloader.OnDownloadErrorCallback = OnDownloadError;
            downloader.OnDownloadProgressCallback = OnDownloadProgress;
            downloader.OnDownloadOverCallback = OnDownloadOver;
            downloader.OnStartDownloadFileCallback = OnStartDownloadFile;

            //开启下载
            downloader.BeginDownload();
            await downloader.Task;

            //检测下载结果
            if (downloader.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"{DEBUG_FLAG} Download Success");

                await CopyHotUpdateDll(packageType);
            }
            else
            {
                Debug.Log($"{DEBUG_FLAG} Download Failed");
            }

            OnHotUpdateFinishDownload?.Invoke(packageName);
        }

        private async UniTask CopyHotUpdateDll(EPackageType packageType)
        {
            if (packageType != EPackageType.RawFile)
            {
                return;
            }

            string packageName = GetPackageName(packageType);
            var pkg = YooAssets.GetPackage(packageName);
            Debug.Log($"{DEBUG_FLAG} Start Copy DLL, packageName: {packageName}");
            foreach (string dll in YooAssetConfig.HotUpdateDLLs)
            {
                Debug.Log($"{DEBUG_FLAG} Copy DLL: {dll}");
                RawFileHandle handle = pkg.LoadRawFileAsync(dll);
                await handle.Task;
                string filePath = handle.GetRawFilePath();

                if (!System.IO.File.Exists(filePath))
                {
                    Debug.Log($"{DEBUG_FLAG} No HotUpdate dll, path: {filePath}");
                    return;
                }

                FileUtility.CopyFileWithComparison(filePath, $"{HotUpdateDLLBasePath}/{dll}");
            }

            Debug.Log($"{DEBUG_FLAG} Finish Copy DLL");
        }

        private async UniTask LoadHotUpdateDll()
        {
            // Editor 环境下，dll 已经被自动加载，不需要加载，重复加载反而会出问题。
#if UNITY_EDITOR
            await UniTask.DelayFrame(1);
            Debug.Log("编辑器模式无需加载热更Dll ");
#else
            foreach (string dll in YooAssetConfig.HotUpdateDLLs)
            {
                string hotupdatePath = $"{HotUpdateDLLBasePath}/{dll}";
            
                if (!System.IO.File.Exists(hotupdatePath))
                {
                    Debug.Log($"{DEBUG_FLAG} No Hot Update file need to load");
                    return;
                }
            
                byte[] assemblyData = await System.IO.File.ReadAllBytesAsync(hotupdatePath);
                System.Reflection.Assembly.Load(assemblyData);
            }

            Debug.Log($"{DEBUG_FLAG} Load HotUpdate DLL Success");
#endif
        }

        public UniTask<Scene> LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool suspendLoad = false, uint priority = 100, CancellationToken cancellationToken = default)
        {
            var completionSource = new UniTaskCompletionSource<Scene>();

            // 异步加载场景
            SceneHandle handle = resourcePackage?.LoadSceneAsync(location, sceneMode, suspendLoad, priority);
    
            if (handle == null)
            {
                completionSource.TrySetException(new Exception($"{DEBUG_FLAG} Scene loading handle is null."));
                return completionSource.Task;
            }

            handle.Completed += op =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled();
                    if (!handle.IsDone)
                    {
                        // TODO: to be implemented
                        // handle.Release();
                    }
                    return;
                }

                if (op.Status == EOperationStatus.Succeed)
                {
                    completionSource.TrySetResult(op.SceneObject);
                }
                else
                {
                    var errorMessage = $"{DEBUG_FLAG} Failed to load the scene at location {location}. Status: {op.Status}";
                    completionSource.TrySetException(new Exception(errorMessage));
                    
                    // TODO: to be implemented
                    // handle.Release(); // Make sure to release the handle in case of failure too
                }
            };

            // 注册取消操作
            cancellationToken.Register(() =>
            {
                if (!handle.IsDone)
                {
                    // TODO: to be implemented
                    // handle.Release();
                }
            });

            return completionSource.Task;
        }


        private void OnDownloadError(string fileName, string error)
        {
            Debug.Log($"{DEBUG_FLAG} Download Error, info: {error}");
        }

        private void OnDownloadProgress(int totalDownloadCount, int currentDownloadCount, long totalDownloadBytes,
            long currentDownloadBytes)
        {
            float currentProgress = (float)currentDownloadBytes / totalDownloadBytes;
            string currentProgressStr = (currentProgress * 100).ToString("f2");

            Debug.Log(
                $"{DEBUG_FLAG} Download Progress, totalBytes: {totalDownloadBytes}, currentBytes: {currentDownloadBytes}, progress: {currentProgressStr}");

            OnHotUpdateProgressUpdate?.Invoke(currentProgress);
        }

        private string GetPackageName(EPackageType packageType)
        {
            string packageName = "INVALID";
            switch (packageType)
            {
                case EPackageType.Resource:
                    packageName = YooAssetConfig.GetResourcePackageName(Application.platform);
                    break;
                case EPackageType.RawFile:
                    packageName = YooAssetConfig.GetRawFilePackageName(Application.platform);
                    break;
            }

            return packageName;
        }

        private void OnDownloadOver(bool isSucceed)
        {
        }

        private void OnStartDownloadFile(string fileName, long sizeBytes)
        {
        }

        public UniTask<TObject> LoadAssetAsync<TObject>(string location, uint priority = 0,
            AssetHandleReleasePolicy releasePolicy = AssetHandleReleasePolicy.Keep,
            CancellationToken cancellationToken = default) where TObject : UnityEngine.Object
        {
            var completionSource = new UniTaskCompletionSource<TObject>();
            var operation = resourcePackage.LoadAssetAsync<TObject>(location, priority);

            operation.Completed += (op) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled();
                    ReleaseAssetHandleIfNeeded(operation, releasePolicy);
                    return;
                }

                if (op.Status == EOperationStatus.Succeed)
                {
                    completionSource.TrySetResult(op.AssetObject as TObject);
                }
                else
                {
                    var errorMessage = $"Failed to load the asset at location {location}. Status: {op.Status}";
                    Debug.LogError($"{DEBUG_FLAG} {errorMessage}");
                    completionSource.TrySetException(new Exception(errorMessage));
                }

                ReleaseAssetHandleIfNeeded(operation, releasePolicy);
            };

            RegisterForCancellation(operation, cancellationToken);

            return completionSource.Task;
        }

        public UniTask<T> LoadRawFileAsync<T>(string location, uint priority = 0,
            AssetHandleReleasePolicy releasePolicy = AssetHandleReleasePolicy.Keep,
            CancellationToken cancellationToken = default) where T : class
        {
            var completionSource = new UniTaskCompletionSource<T>();

            // 异步加载原始文件
            var handle = rawFilePackage?.LoadRawFileAsync(location, priority);
            if (handle == null)
            {
                completionSource.TrySetException(
                    new Exception($"{DEBUG_FLAG} RawFilePackage is not initialized or handle is null."));
                return completionSource.Task;
            }

            handle.Completed += (op) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completionSource.TrySetCanceled();
                    ReleaseAssetHandleIfNeeded(op, releasePolicy);
                    return;
                }

                if (op.Status == EOperationStatus.Succeed)
                {
                    try
                    {
                        // 根据泛型类型参数T来决定如何处理加载后的文件内容
                        if (typeof(T) == typeof(byte[]))
                        {
                            byte[] result = op.GetRawFileData();
                            completionSource.TrySetResult(result as T);
                        }
                        else if (typeof(T) == typeof(string))
                        {
                            string result = op.GetRawFileText();
                            completionSource.TrySetResult(result as T);
                        }
                        else
                        {
                            throw new InvalidOperationException($"{DEBUG_FLAG} Unsupported type parameter provided.");
                        }
                    }
                    catch (Exception ex)
                    {
                        completionSource.TrySetException(ex);
                    }
                    finally
                    {
                        ReleaseAssetHandleIfNeeded(op, releasePolicy);
                    }
                }
                else
                {
                    var errorMessage =
                        $"{DEBUG_FLAG} Failed to load the raw file at location {location}. Status: {op.Status}";
                    completionSource.TrySetException(new Exception(errorMessage));
                    ReleaseAssetHandleIfNeeded(op, releasePolicy);
                }
            };

            // 注册取消操作
            RegisterForCancellation(handle, cancellationToken);

            return completionSource.Task;
        }

        private void ReleaseAssetHandleIfNeeded(YooAsset.AssetHandle operation, AssetHandleReleasePolicy releasePolicy)
        {
            if (releasePolicy == AssetHandleReleasePolicy.ReleaseOnComplete && operation.IsValid)
            {
                operation.Release();
            }
        }

        private void RegisterForCancellation(YooAsset.AssetHandle operation, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                if (!operation.IsDone && operation.IsValid)
                {
                    operation.Release();
                }
            });
        }

        private void ReleaseAssetHandleIfNeeded(YooAsset.RawFileHandle operation,
            AssetHandleReleasePolicy releasePolicy)
        {
            if (releasePolicy == AssetHandleReleasePolicy.ReleaseOnComplete && operation.IsValid)
            {
                operation.Release();
            }
        }

        private void RegisterForCancellation(YooAsset.RawFileHandle operation, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() =>
            {
                if (!operation.IsDone && operation.IsValid)
                {
                    operation.Release();
                }
            });
        }

        public void TryUnloadUnusedAsset(string location)
        {
            resourcePackage?.TryUnloadUnusedAsset(location);
        }

        public void TryUnloadUnusedRawFile(string location)
        {
            rawFilePackage?.TryUnloadUnusedAsset(location);
        }

        public void UnloadUnusedAssets()
        {
            resourcePackage?.UnloadUnusedAssets();
            rawFilePackage?.UnloadUnusedAssets();
        }
        
        public string GetResourcePackageVersion()
        {
            string result = resourcePackage?.GetPackageVersion();
            return string.IsNullOrEmpty(result) ? "Invalid Resource Version" : result;
        }

        public string GetRawFilePackageVersion()
        {
            string result = rawFilePackage?.GetPackageVersion();
            return string.IsNullOrEmpty(result) ? "Invalid RawFile Version" : result;
        }
    }
}