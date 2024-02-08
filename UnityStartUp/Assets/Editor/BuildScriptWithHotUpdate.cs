using System;
using System.Collections.Generic;
using System.IO;
using CycloneGames.HotUpdate;
using UnityEngine;
using UnityEditor;
using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.Settings;
using YooAsset.Editor;
using BuildResult = UnityEditor.Build.Reporting.BuildResult;

namespace CycloneGames.Editor.Build
{
    public class BuildScriptWithHotUpdate
    {
        private const string DEBUG_FLAG = "<color=cyan>[Game Builder]</color>";

        private const string INVALID_FLAG = "INVALID";
        private const string OutputBasePath = "Build";

        private const string ApplicationName = "UnityStartUp";
        private const string BuildDataConfig = "Assets/StartUp/ScriptableObject/BuildConfig/BuildData.asset";
        private const string YooAssetDataConfig = "Assets/CycloneGames.HotUpdate/ScriptableObject/ShouldBeModified/YooAssetData.asset";
        private const string HotUpdateAssetVersion = "v1.0";

        private static InstallerController hybridInstallerController;
        private const string HYBRID_CLR_PATH = "HybridCLRData";
        private static BuildData buildData;
        private static YooAssetData yooAssetData;
        private const string HOT_UPDATE_ASSETS_PREPARE_PATH = "HotUpdateAssetsPreUpload";
        
        private static string savedResAssetOutPath = "INVALID";
        private static string savedRawFileOutPath = "INVALID";
        private static BuildTarget savedHotUpdateAssetsTarget = BuildTarget.NoTarget;

        [MenuItem("Build/Game(WithHotUpdate)/Print Debug Info", priority = 100)]
        public static void PrintDebugInfo()
        {
            var sceneList = GetBuildSceneList();
            if (sceneList == null || sceneList.Length == 0)
            {
                Debug.LogError(
                    $"{DEBUG_FLAG} Invalid scene list, please check the file <color=cyan>{BuildDataConfig}</color>");
                return;
            }

            foreach (var scene_name in sceneList)
            {
                Debug.Log($"{DEBUG_FLAG} Pre Build Scene: {scene_name}");
            }
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Generate HotUpdate DLL (Windows)", priority = 200)]
        public static void GenerateAndCopyHotUpdateDLL_Windows()
        {
            if (!TryGetHybridInstallerController().HasInstalledHybridCLR())
            {
                TryGetHybridInstallerController().InstallDefaultHybridCLR();
            }

            GenerateHotUpdateDLL(BuildTarget.StandaloneWindows64);

            CopyHotUpdateDLL(BuildTarget.StandaloneWindows64);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Generate HotUpdate DLL (Android)", priority = 201)]
        public static void GenerateAndCopyHotUpdateDLL_Android()
        {
            if (!TryGetHybridInstallerController().HasInstalledHybridCLR())
            {
                TryGetHybridInstallerController().InstallDefaultHybridCLR();
            }

            GenerateHotUpdateDLL(BuildTarget.Android);

            CopyHotUpdateDLL(BuildTarget.Android);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Generate HotUpdate DLL (Mac)", priority = 202)]
        public static void GenerateAndCopyHotUpdateDLL_MacOS()
        {
            if (!TryGetHybridInstallerController().HasInstalledHybridCLR())
            {
                TryGetHybridInstallerController().InstallDefaultHybridCLR();
            }

            GenerateHotUpdateDLL(BuildTarget.StandaloneOSX);

            CopyHotUpdateDLL(BuildTarget.StandaloneOSX);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Generate HotUpdate DLL (iOS)", priority = 203)]
        public static void GenerateAndCopyHotUpdateDLL_iOS()
        {
            if (!TryGetHybridInstallerController().HasInstalledHybridCLR())
            {
                TryGetHybridInstallerController().InstallDefaultHybridCLR();
            }

            GenerateHotUpdateDLL(BuildTarget.iOS);

            CopyHotUpdateDLL(BuildTarget.iOS);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Bundle HotUpdate Assets (Windows)", priority = 300)]
        public static void BundleHotUpdateAssets_Windows()
        {
            BundleHotUpdateAssets(BuildTarget.StandaloneWindows64, HotUpdateAssetVersion);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Bundle HotUpdate Assets (Android)", priority = 301)]
        public static void BundleHotUpdateAssets_Android()
        {
            BundleHotUpdateAssets(BuildTarget.Android, HotUpdateAssetVersion);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Bundle HotUpdate Assets (Mac)", priority = 302)]
        public static void BundleHotUpdateAssets_MacOS()
        {
            BundleHotUpdateAssets(BuildTarget.StandaloneOSX, HotUpdateAssetVersion);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Bundle HotUpdate Assets (iOS)", priority = 303)]
        public static void BundleHotUpdateAssets_iOS()
        {
            BundleHotUpdateAssets(BuildTarget.iOS, HotUpdateAssetVersion);
        }
        
        [MenuItem("Build/Game(WithHotUpdate)/Build Android APK (IL2CPP)", priority = 400)]
        public static void PerformBuild_AndroidAPK()
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PerformBuild(
                BuildTarget.Android,
                BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP,
                $"{GetPlatformFolderName(BuildTarget.Android)}/{ApplicationName}.apk",
                bCleanBuild: true,
                bOutputIsFolderTarget: false);
        }
        
        [MenuItem("Build/Game(WithHotUpdate)/Build Windows (IL2CPP)", priority = 401)]
        public static void PerformBuild_Windows()
        {
            PerformBuild(
                BuildTarget.StandaloneWindows64,
                BuildTargetGroup.Standalone,
                ScriptingImplementation.IL2CPP,
                $"{GetPlatformFolderName(BuildTarget.StandaloneWindows64)}/{ApplicationName}.exe",
                bCleanBuild: true,
                bOutputIsFolderTarget: false);
        }
        
        [MenuItem("Build/Game(WithHotUpdate)/Build Mac (IL2CPP)", priority = 402)]
        public static void PerformBuild_Mac()
        {
            PerformBuild(
                BuildTarget.StandaloneOSX,
                BuildTargetGroup.Standalone,
                ScriptingImplementation.IL2CPP,
                $"{GetPlatformFolderName(BuildTarget.StandaloneOSX)}/{ApplicationName}.app",
                bCleanBuild: true,
                bOutputIsFolderTarget: false);
        }
        
        [MenuItem("Build/Game(WithHotUpdate)/Export Android Project (IL2CPP)", priority = 403)]
        public static void PerformBuild_AndroidProject()
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
            PerformBuild(
                BuildTarget.Android,
                BuildTargetGroup.Android,
                ScriptingImplementation.IL2CPP,
                $"{GetPlatformFolderName(BuildTarget.Android)}/{ApplicationName}",
                bCleanBuild: true,
                bOutputIsFolderTarget: true);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Prepare HotUpdate Assets (Windows)", priority = 501)]
        public static void PrepareHotUpdateAssets_Windows()
        {
            PrepareHotUpdateAssets(BuildTarget.StandaloneWindows64);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Prepare HotUpdate Assets (Android)", priority = 502)]
        public static void PrepareHotUpdateAssets_Android()
        {
            PrepareHotUpdateAssets(BuildTarget.Android);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Prepare HotUpdate Assets (Mac)", priority = 503)]
        public static void PrepareHotUpdateAssets_MacOS()
        {
            PrepareHotUpdateAssets(BuildTarget.StandaloneOSX);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Prepare HotUpdate Assets (iOS)", priority = 504)]
        public static void PrepareHotUpdateAssets_iOS()
        {
            PrepareHotUpdateAssets(BuildTarget.iOS);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Upload HotUpdate Assets (Windows)", priority = 600)]
        public static void UploadHotUpdateAssets_Windows()
        {
            //  TODO: Upload From Some WebAPI

            UploadHotUpdateAssets(BuildTarget.StandaloneWindows64);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Upload HotUpdate Assets (Android)", priority = 601)]
        public static void UploadHotUpdateAssets_Android()
        {
            //  TODO: Upload From Some WebAPI

            UploadHotUpdateAssets(BuildTarget.Android);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Upload HotUpdate Assets (Mac)", priority = 602)]
        public static void UploadHotUpdateAssets_MacOS()
        {
            //  TODO: Upload From Some WebAPI

            UploadHotUpdateAssets(BuildTarget.StandaloneOSX);
        }
        
        [MenuItem("Build/HotUpdate Detailed Step/Upload HotUpdate Assets (iOS)", priority = 603)]
        public static void UploadHotUpdateAssets_iOS()
        {
            //  TODO: Upload From Some WebAPI

            UploadHotUpdateAssets(BuildTarget.iOS);
        }
        
        [MenuItem("Build/Generate And Prepare HotUpdate Assets (Windows)", priority = 700)]
        public static void GenerateAndPrepareHotUpdateAssets_Windows()
        {
            GenerateAndCopyHotUpdateDLL_Windows();
            BundleHotUpdateAssets_Windows();
            PrepareHotUpdateAssets_Windows();
        }
        
        [MenuItem("Build/Generate And Prepare HotUpdate Assets (Android)", priority = 701)]
        public static void GenerateAndPrepareHotUpdateAssets_Android()
        {
            GenerateAndCopyHotUpdateDLL_Android();
            BundleHotUpdateAssets_Android();
            PrepareHotUpdateAssets_Android();
        }
        
        [MenuItem("Build/Generate And Prepare HotUpdate Assets (Mac)", priority = 702)]
        public static void GenerateAndPrepareHotUpdateAssets_MacOS()
        {
            GenerateAndCopyHotUpdateDLL_MacOS();
            BundleHotUpdateAssets_MacOS();
            PrepareHotUpdateAssets_MacOS();
        }
        
        [MenuItem("Build/Generate And Prepare HotUpdate Assets (iOS)", priority = 703)]
        public static void GenerateAndPrepareHotUpdateAssets_iOS()
        {
            GenerateAndCopyHotUpdateDLL_iOS();
            BundleHotUpdateAssets_iOS();
            PrepareHotUpdateAssets_iOS();
        }

        [MenuItem("Build/Generate And Upload HotUpdate Assets (Windows)", priority = 800)]
        public static void GenerateAndUploadHotUpdateAssets_Windows()
        {
            GenerateAndCopyHotUpdateDLL_Windows();
            BundleHotUpdateAssets_Windows();
            UploadHotUpdateAssets_Windows();
        }
        
        [MenuItem("Build/Generate And Upload HotUpdate Assets (Android)", priority = 801)]
        public static void GenerateAndUploadHotUpdateAssets_Android()
        {
            GenerateAndCopyHotUpdateDLL_Android();
            BundleHotUpdateAssets_Android();
            UploadHotUpdateAssets_Android();
        }
        
        [MenuItem("Build/Generate And Upload HotUpdate Assets (Mac)", priority = 802)]
        public static void GenerateAndUploadHotUpdateAssets_MacOS()
        {
            GenerateAndCopyHotUpdateDLL_MacOS();
            BundleHotUpdateAssets_MacOS();
            UploadHotUpdateAssets_MacOS();
        }
        
        [MenuItem("Build/Generate And Upload HotUpdate Assets (iOS)", priority = 803)]
        public static void GenerateAndUploadHotUpdateAssets_iOS()
        {
            GenerateAndCopyHotUpdateDLL_iOS();
            BundleHotUpdateAssets_iOS();
            UploadHotUpdateAssets_iOS();
        }
        
        private static string GetPlatformFolderName(BuildTarget TargetPlatform)
        {
            switch (TargetPlatform)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "Mac";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.NoTarget:
                    return INVALID_FLAG;
            }

            return INVALID_FLAG;
        }
        
        private static RuntimePlatform GetRuntimePlatformFromBuildTarget(BuildTarget TargetPlatform)
        {
            switch (TargetPlatform)
            {
                case BuildTarget.Android:
                    return RuntimePlatform.Android;
                case BuildTarget.StandaloneWindows64:
                    return RuntimePlatform.WindowsPlayer;
                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;
                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;
            }

            return RuntimePlatform.WindowsPlayer;
        }
        
        private static YooAssetData TryGetYooAssetData()
        {
            return yooAssetData ??= AssetDatabase.LoadAssetAtPath<YooAssetData>($"{YooAssetDataConfig}");
        }

        private static BuildData TryGetBuildData()
        {
            return buildData ??= AssetDatabase.LoadAssetAtPath<BuildData>($"{BuildDataConfig}");
        }
        
        private static string[] GetBuildSceneList()
        {
            if (!TryGetBuildData())
            {
                Debug.LogError(
                    $"{DEBUG_FLAG} Invalid Build Data Config, please check the file <color=cyan>{BuildDataConfig}</color>");
                return default;
            }

            return new[] { $"{TryGetBuildData().BuildSceneBasePath}/{TryGetBuildData().LaunchScene.name}.unity" };
        }
        
        private static void DeletePlatformBuildFolder(BuildTarget TargetPlatform)
        {
            string platformBuildOutputPath = GetPlatformBuildOutputFolder(TargetPlatform);
            string platformOutputFullPath =
                platformBuildOutputPath != INVALID_FLAG ? Path.GetFullPath(platformBuildOutputPath) : INVALID_FLAG;
            
            if (Directory.Exists(platformOutputFullPath))
            {
                Debug.Log($"{DEBUG_FLAG} Clean old build {Path.GetFullPath(platformBuildOutputPath)}");
                Directory.Delete(platformOutputFullPath, true);
            }
        }

        private static string GetOutputTarget(BuildTarget TargetPlatform, string TargetPath,
            bool bTargetIsFolder = true)
        {
            string platformOutFolder = GetPlatformBuildOutputFolder(TargetPlatform);
            string resultPath = Path.Combine(OutputBasePath, TargetPath);
            
            if (!Directory.Exists(Path.GetFullPath(platformOutFolder)))
            {
                Debug.Log($"{DEBUG_FLAG} result path: {resultPath}, platformFolder: {platformOutFolder}, platform fullPath:{Path.GetFullPath(platformOutFolder)}");
                Directory.CreateDirectory(platformOutFolder);
            }
            
#if UNITY_IOS
            if (!Directory.Exists($"{resultPath}/Unity-iPhone/Images.xcassets/LaunchImage.launchimage"))
            {
                Directory.CreateDirectory($"{resultPath}/Unity-iPhone/Images.xcassets/LaunchImage.launchimage");
            }
#endif
            return resultPath;
        }
        
        private static void PerformBuild(BuildTarget TargetPlatform, BuildTargetGroup TargetGroup,
            ScriptingImplementation BackendScriptImpl, string OutputTarget, bool bCleanBuild = true,
            bool bOutputIsFolderTarget = true)
        {
            if (bCleanBuild)
            {
                DeletePlatformBuildFolder(TargetPlatform);
            }
            
            Debug.Log($"{DEBUG_FLAG} Start Build, Platform: {EditorUserBuildSettings.activeBuildTarget}");
            EditorUserBuildSettings.SwitchActiveBuildTarget(TargetGroup, TargetPlatform);
            HybridPipeline(TargetPlatform); // HybridCLR required BuildTarget first [BuildTarget target = EditorUserBuildSettings.activeBuildTarget;]
            
            var buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = GetBuildSceneList();
            buildPlayerOptions.locationPathName = GetOutputTarget(TargetPlatform, OutputTarget, bOutputIsFolderTarget);
            buildPlayerOptions.target = TargetPlatform;
            buildPlayerOptions.options = BuildOptions.CleanBuildCache;
            buildPlayerOptions.options |= BuildOptions.CompressWithLz4;
            PlayerSettings.SetScriptingBackend(TargetGroup, BackendScriptImpl);
            
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            
            if (summary.result == BuildResult.Succeeded)
                Debug.Log($"{DEBUG_FLAG} Build <color=#29ff50>SUCCESS</color>, size: {summary.totalSize} bytes, path: {summary.outputPath}");
            
            if (summary.result == BuildResult.Failed) Debug.Log($"{DEBUG_FLAG} Build <color=red>FAILURE</color>");
        }

        private static string GetPlatformBuildOutputFolder(BuildTarget TargetPlatform)
        {
            return $"{OutputBasePath}/{GetPlatformFolderName(TargetPlatform)}";
        }

        private static string GetHybridCLRHotUpdateDLLOutFolder(BuildTarget TargetPlatform)
        {
            return $"HotUpdateDlls/{TargetPlatform.ToString()}";
        }
        
        static InstallerController TryGetHybridInstallerController()
        {
            return hybridInstallerController ??= new InstallerController();
        }

        static void HybridPipeline(BuildTarget TargetPlatform)
        {
            string HybridCLRFullPath = Path.GetFullPath(HYBRID_CLR_PATH);
            Debug.Log($"{DEBUG_FLAG} Start Clean HybridCLR {HybridCLRFullPath}");
            if (Directory.Exists(HybridCLRFullPath)) BashUtil.RemoveDir(HybridCLRFullPath, true);
            Debug.Log($"{DEBUG_FLAG} Finish Clean HybridCLR {HybridCLRFullPath}");
            
            Debug.Log($"{DEBUG_FLAG} Start HybridCLR Install");
            TryGetHybridInstallerController().InstallDefaultHybridCLR();
            Debug.Log($"{DEBUG_FLAG} Finish HybridCLR Install");
            
            Debug.Log($"{DEBUG_FLAG} Start Generate HybridCLR Data");
            PrebuildCommand.GenerateAll();
            Debug.Log($"{DEBUG_FLAG} Finish Generate HybridCLR Data");
            
            Debug.Log($"{DEBUG_FLAG} Start Generate HotUpdateDLL");
            GenerateHotUpdateDLL(TargetPlatform);
            Debug.Log($"{DEBUG_FLAG} End Generate HotUpdateDLL");
            
            Debug.Log($"{DEBUG_FLAG} Start Copy HotUpdateDLL");
            CopyHotUpdateDLL(TargetPlatform);
            Debug.Log($"{DEBUG_FLAG} Finish Copy HotUpdateDLL");
        }

        private static void CopyHotUpdateDLL(BuildTarget TargetPlatform)
        {
            List<string> HotUpdateDLLNameList = new List<string>();
            foreach (var asmdef in HybridCLRSettings.Instance.hotUpdateAssemblyDefinitions)
            {
                HotUpdateDLLNameList.Add($"{asmdef.name}");
                Debug.Log($"{DEBUG_FLAG} Add dll to HotUpdateDLL list: <color=#29ff50>{asmdef.name}</color>");
            }

            string hotUpdateDllSrcPath = $"{Path.GetFullPath(HYBRID_CLR_PATH)}/{GetHybridCLRHotUpdateDLLOutFolder(TargetPlatform)}";
            var hotUpdateDLLBasePath = TryGetYooAssetData()?.HotUpdateDLLBasePath;
            if (string.IsNullOrEmpty(hotUpdateDLLBasePath))
            {
                Debug.LogError($"{DEBUG_FLAG} YooAssetConfig Error, check YooAssetData config file");
                return;
            }

            string hotUpdateDllTargetPath =
                $"{Path.GetFullPath(hotUpdateDLLBasePath).TrimEnd('/')}/{GetPlatformFolderName(TargetPlatform)}";

            // Debug.Log($"{DEBUG_FLAG} \n src path: {hotUpdateDllSrcPath}  \n tgt path: {hotUpdateDllTargetPath}");
            CopySpecificDllFilesAsBytes(hotUpdateDllSrcPath, hotUpdateDllTargetPath, HotUpdateDLLNameList);
        }

        // Call this method with the source directory, target directory and an array of the names of the DLL files to copy as parameters
        private static void CopySpecificDllFilesAsBytes(string sourceDirectory, string targetDirectory,
            List<string> dllFileNames)
        {
            // Check if the source directory exists
            if (!Directory.Exists(sourceDirectory))
            {
                Debug.LogError("Source directory does not exist: " + sourceDirectory);
                return;
            }

            // Check if the target directory exists, if not, create it
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            foreach (string dllFileName in dllFileNames)
            {
                // Construct the full path of the source dll file
                string dllFilePath = Path.Combine(sourceDirectory, dllFileName + ".dll");

                // Make sure the source file exists before attempting to copy
                if (!File.Exists(dllFilePath))
                {
                    Debug.LogError("DLL file does not exist: " + dllFilePath);
                    continue;
                }

                // Construct the destination file path with the .dll.byte extension
                string destFilePath = Path.Combine(targetDirectory, dllFileName + ".dll.bytes");

                // Copy the dll file and rename it to .dll.byte
                try
                {
                    // Check if the .dll.byte file exists at the destination and delete it if it does
                    if (File.Exists(destFilePath))
                    {
                        File.Delete(destFilePath);
                    }

                    // Now that we've ensured the .dll.byte file does not exist, copy the dll file and rename it
                    File.Copy(dllFilePath, destFilePath);
                }
                catch (IOException ioEx)
                {
                    Debug.LogError("Error copying file: " + ioEx.Message);
                }
            }

            // Refresh the AssetDatabase after copying the files
            AssetDatabase.Refresh();
        }

        private static void GenerateHotUpdateDLL(BuildTarget TargetPlatform)
        {
            switch (TargetPlatform)
            {
                case BuildTarget.Android:
                    CompileDllCommand.CompileDllAndroid();
                    break;
                case BuildTarget.StandaloneWindows64:
                    CompileDllCommand.CompileDllWin64();
                    break;
                case BuildTarget.StandaloneOSX:
                    CompileDllCommand.CompileDllMacOS();
                    break;
                case BuildTarget.iOS:
                    CompileDllCommand.CompileDllIOS();
                    break;
            }
        }
        
        private static void BundleHotUpdateAssets(BuildTarget TargetPlatform, string PackageVersionStr)
        {
            savedHotUpdateAssetsTarget = BuildTarget.NoTarget;
            
            /********************************************** Resource Asset Build ***********************************************/
            BuiltinBuildParameters resAssetBundleParameters = new BuiltinBuildParameters();
            resAssetBundleParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            resAssetBundleParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            resAssetBundleParameters.BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString();
            resAssetBundleParameters.BuildTarget = TargetPlatform;
            resAssetBundleParameters.BuildMode = EBuildMode.ForceRebuild;
            resAssetBundleParameters.PackageName = TryGetYooAssetData().GetResourcePackageName(GetRuntimePlatformFromBuildTarget(TargetPlatform));
            resAssetBundleParameters.PackageVersion = PackageVersionStr;
            resAssetBundleParameters.VerifyBuildingResult = true;
            resAssetBundleParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;
            resAssetBundleParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
            // resAssetBundleParameters.BuildinFileCopyParams = buildinFileCopyParams;
            // resAssetBundleParameters.EncryptionServices = CreateEncryptionInstance();
            resAssetBundleParameters.CompressOption = ECompressOption.LZ4;

            BuiltinBuildPipeline resAssetBundlePipeline = new BuiltinBuildPipeline();
            var resAssetBuildResult = resAssetBundlePipeline.Run(resAssetBundleParameters, true);

            if (resAssetBuildResult.Success)
            {
                savedResAssetOutPath =
                    $"{resAssetBundleParameters.BuildOutputRoot}/{TargetPlatform.ToString()}/{TryGetYooAssetData().GetResourcePackageName(GetRuntimePlatformFromBuildTarget(TargetPlatform))}/{PackageVersionStr}";
                Debug.Log($"{DEBUG_FLAG} Bundle ResourceAsset <color=#29ff50>SUCCESS</color>, " +
                          $"\n Path: {savedResAssetOutPath}");
            }
            else
            {
                Debug.Log($"{DEBUG_FLAG} Bundle ResourceAsset <color=red>FAILURE</color>");
                
                return;
            }
            /********************************************** Resource Asset Build ***********************************************/
            
            
            /********************************************** RawFile Asset Build ***********************************************/
            RawFileBuildParameters rawFileBundleParameters = new RawFileBuildParameters();
            rawFileBundleParameters.BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
            rawFileBundleParameters.BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            rawFileBundleParameters.BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString();
            rawFileBundleParameters.BuildTarget = TargetPlatform;
            rawFileBundleParameters.BuildMode = EBuildMode.ForceRebuild;
            rawFileBundleParameters.PackageName = TryGetYooAssetData().GetRawFilePackageName(GetRuntimePlatformFromBuildTarget(TargetPlatform));
            rawFileBundleParameters.PackageVersion = PackageVersionStr;
            rawFileBundleParameters.VerifyBuildingResult = true;
            rawFileBundleParameters.FileNameStyle = EFileNameStyle.BundleName_HashName;;
            rawFileBundleParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;;
            // rawFileBundleParameters.BuildinFileCopyParams = buildinFileCopyParams;
            // rawFileBundleParameters.EncryptionServices = CreateEncryptionInstance();
            
            RawFileBuildPipeline pipeline = new RawFileBuildPipeline();
            var buildResult = pipeline.Run(rawFileBundleParameters, true);
            if (buildResult.Success)
            {
                savedRawFileOutPath =
                    $"{rawFileBundleParameters.BuildOutputRoot}/{TargetPlatform.ToString()}/{TryGetYooAssetData().GetRawFilePackageName(GetRuntimePlatformFromBuildTarget(TargetPlatform))}/{PackageVersionStr}";
                Debug.Log($"{DEBUG_FLAG} Bundle RawFile <color=#29ff50>SUCCESS</color>, " +
                          $"\n Path: {savedRawFileOutPath}");

                savedHotUpdateAssetsTarget = TargetPlatform;
            }
            else
            {
                Debug.Log($"{DEBUG_FLAG} Bundle RawFile <color=red>FAILURE</color>");
            }
            /********************************************** RawFile Asset Build ***********************************************/
        }
        
        public static void CopyAllFilesRecursively(string sourceFolderPath, string destinationFolderPath)
        {
            // Check if the source directory exists
            if (!Directory.Exists(sourceFolderPath))
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {sourceFolderPath}");
            }

            // Ensure the destination directory exists
            // 注意：如果目标路径是局域网路径，则需要网络权限才能创建目录
            try
            {
                if (!Directory.Exists(destinationFolderPath))
                {
                    Directory.CreateDirectory(destinationFolderPath);
                }
            }
            catch (Exception ex)
            {
                // 可以处理更加具体的异常，例如对于网络路径无权限访问时的UnauthorizedAccessException
                throw new Exception($"Error creating destination directory: {destinationFolderPath}. Exception: {ex.Message}");
            }

            // Get the files in the source directory and copy them to the destination directory
            foreach (string sourceFilePath in Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
            {
                // Create a relative path that is the same for both source and destination
                string relativePath = sourceFilePath.Substring(sourceFolderPath.Length + 1);
                string destinationFilePath = Path.Combine(destinationFolderPath, relativePath);

                // Ensure the directory for the destination file exists (since it might be a subdirectory that doesn't exist yet)
                string destinationFileDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(destinationFileDirectory))
                {
                    Directory.CreateDirectory(destinationFileDirectory);
                }

                // Copy the file and overwrite if it already exists
                // 注意：如果目标路径是局域网路径，也需要网络权限才能复制文件
                try
                {
                    File.Copy(sourceFilePath, destinationFilePath, true);
                }
                catch (Exception ex)
                {
                    // 同样可以处理更加具体的异常
                    throw new Exception($"Error copying file: {sourceFilePath} to {destinationFilePath}. Exception: {ex.Message}");
                }
            }
        }

        private static void PrepareHotUpdateAssets(BuildTarget TargetPlatform)
        {
            bool bIsPackageBuild = savedHotUpdateAssetsTarget == TargetPlatform;

            if (bIsPackageBuild)
            {
                string prepareFullPath =
                    $"{HOT_UPDATE_ASSETS_PREPARE_PATH}/{GetPlatformFolderName(TargetPlatform)}/{HotUpdateAssetVersion}";
                
                if (Directory.Exists(prepareFullPath))
                {
                    Directory.Delete(prepareFullPath, true);
                    Debug.Log($"{DEBUG_FLAG} Delete Old HotUpdate Assets");
                }
                
                CopyAllFilesRecursively(savedResAssetOutPath, prepareFullPath);
                CopyAllFilesRecursively(savedRawFileOutPath, prepareFullPath);
                Debug.Log($"{DEBUG_FLAG} Success Copy HotUpdate Assets To PreUpload Folder, Path: {Path.GetFullPath(prepareFullPath)}");
            }
            else
            {
                Debug.LogError($"{DEBUG_FLAG} Please Bundle HotUpdate Assets ({GetPlatformFolderName(TargetPlatform)}) First");
            }
        }

        // TODO: Upload File BY WebAPI
        private static void UploadHotUpdateAssets(BuildTarget TargetPlatform)
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
            {
                Debug.Log($"{DEBUG_FLAG} Upload failed, the HotUpdate Asset can be upload from Windows machine only, current machine: {Application.platform}");
                return;
            }
            
            bool bIsPackageBuild = savedHotUpdateAssetsTarget == TargetPlatform;
            
            if (bIsPackageBuild)
            {
                string targetPathUrl = $"http://192.168.50.6/public/webdav/game_package/{ApplicationName}/{GetPlatformFolderName(TargetPlatform)}/{HotUpdateAssetVersion}";
                string modifiedPath = targetPathUrl
                    .Replace("http://", "\\\\")
                    .Replace("/", "\\");
                
                CopyAllFilesRecursively(savedResAssetOutPath, modifiedPath);
                CopyAllFilesRecursively(savedRawFileOutPath, modifiedPath);
                
                Debug.Log($"{DEBUG_FLAG} Success Upload HotUpdate Assets, URL: {modifiedPath}");
            }
            else
            {
                Debug.LogError($"{DEBUG_FLAG} Please Bundle HotUpdate Assets ({GetPlatformFolderName(TargetPlatform)}) First");
            }
        }
    }
}