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
            // Editor environment - no need to load hot update DLLs, as they are automatically loaded; loading again may cause issues.
#if UNITY_EDITOR
            await UniTask.DelayFrame(1);
            Debug.Log("No need to load hot update DLLs in editor mode.");
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
            var registration = cancellationToken.Register(() =>
            {
                if (!handle.IsDone)
                {
                    // TODO: to be implemented
                    // handle.Release();
                }
            });
            handle.Completed += _ => registration.Dispose();

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
        
        public async UniTask<TObject> LoadAssetAsync<TObject>(string location, uint priority = 0,
            AssetHandleReleasePolicy releasePolicy = AssetHandleReleasePolicy.Keep,
            CancellationToken cancellationToken = default) where TObject : UnityEngine.Object
        {
            var handle = resourcePackage.LoadAssetAsync(location, priority);
            RegisterForCancellation(handle, cancellationToken); // Register early for cancellation
            await handle.ToUniTask(cancellationToken: cancellationToken); // Use cancellationToken to support cancellation operations
            return ProcessLoadedAsset<TObject>(handle, releasePolicy, cancellationToken);
        }

        public async UniTask<TRawFile> LoadRawFileAsync<TRawFile>(string location, uint priority = 0,
            AssetHandleReleasePolicy releasePolicy = AssetHandleReleasePolicy.Keep,
            CancellationToken cancellationToken = default) where TRawFile : class
        {
            var handle = rawFilePackage.LoadRawFileAsync(location, priority);
            RegisterForCancellation(handle, cancellationToken); // Register early for cancellation
            await handle.ToUniTask(cancellationToken: cancellationToken); // Use cancellationToken to support cancellation operations
            return ProcessLoadedRawFile<TRawFile>(handle, releasePolicy, cancellationToken);
        }

        private TObject ProcessLoadedAsset<TObject>(AssetHandle handle, AssetHandleReleasePolicy releasePolicy,
            CancellationToken cancellationToken) where TObject : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                handle?.Release(); // 确保在取消时释放资源
                throw new OperationCanceledException();
            }

            if (!handle.IsValid)
            {
                throw new Exception($"{DEBUG_FLAG} Asset load failed or handle is invalid.");
            }

            TObject asset = handle.AssetObject as TObject;
            if (releasePolicy == AssetHandleReleasePolicy.ReleaseOnComplete)
            {
                handle.Release();
            }
            return asset;
        }

        private T ProcessLoadedRawFile<T>(RawFileHandle handle, AssetHandleReleasePolicy releasePolicy,
            CancellationToken cancellationToken) where T : class
        {
            if (cancellationToken.IsCancellationRequested)
            {
                handle?.Release(); // 确保在取消时释放资源
                throw new OperationCanceledException();
            }

            if (!handle.IsValid)
            {
                throw new Exception($"{DEBUG_FLAG} Raw file load failed or handle is invalid.");
            }

            T result = null;
            if (typeof(T) == typeof(byte[]))
            {
                result = handle.GetRawFileData() as T;
            }
            else if (typeof(T) == typeof(string))
            {
                result = handle.GetRawFileText() as T;
            }
            else
            {
                throw new InvalidOperationException($"{DEBUG_FLAG} Unsupported type parameter provided.");
            }

            if (releasePolicy == AssetHandleReleasePolicy.ReleaseOnComplete)
            {
                handle.Release();
            }
            return result;
        }

        // Ensures that the resource is released upon cancellation.
        private void RegisterForCancellation(AssetHandle handle, CancellationToken cancellationToken)
        {
            var registration = cancellationToken.Register(() =>
            {
                if (handle.IsValid && !handle.IsDone)
                {
                    handle.Release();
                }
            });
            
            // Dispose of the registration when the asset loading is complete.
            handle.Completed += _ => registration.Dispose();
        }
        
        // Ensures that the resource is released upon cancellation.
        private void RegisterForCancellation(YooAsset.RawFileHandle handle, CancellationToken cancellationToken)
        {
            var registration = cancellationToken.Register(() =>
            {
                if (handle.IsValid && !handle.IsDone)
                {
                    handle.Release();
                }
            });
            
            // Dispose of the registration when the asset loading is complete.
            handle.Completed += _ => registration.Dispose();
        }

        // Attempts to unload an unused asset located at the specified location.
        public void TryUnloadUnusedAsset(string location)
        {
            resourcePackage?.TryUnloadUnusedAsset(location);
        }

        // Attempts to unload an unused raw file located at the specified location.
        public void TryUnloadUnusedRawFile(string location)
        {
            rawFilePackage?.TryUnloadUnusedAsset(location);
        }

        // Calls the appropriate package to unload any unused assets.
        public void UnloadUnusedAssets()
        {
            resourcePackage?.UnloadUnusedAssets();
            rawFilePackage?.UnloadUnusedAssets();
        }
        
        // Retrieves the version of the resource package.
        public string GetResourcePackageVersion()
        {
            string result = resourcePackage?.GetPackageVersion();
            return string.IsNullOrEmpty(result) ? "Invalid Resource Version" : result;
        }

        // Retrieves the version of the raw file package.
        public string GetRawFilePackageVersion()
        {
            string result = rawFilePackage?.GetPackageVersion();
            return string.IsNullOrEmpty(result) ? "Invalid RawFile Version" : result;
        }
    }
}