using System.Collections.Generic;
using UnityEngine;

namespace CycloneGames.HotUpdate
{
    /// <summary>
    /// This File Must Upgrade For Your Project, you must Upload Asset on CDN server at particular path: GetResFullPath()
    /// </summary>
    [CreateAssetMenu(menuName = "CycloneGames/General/YooAssetData")]
    [System.Serializable]
    public class YooAssetData : ScriptableObject
    {
        [SerializeField] private string serverBaseUrl;
        [SerializeField] private string projectName;
        [SerializeField] private string resPath;

        [SerializeField] private string hotUpdateDllBasePath;
        //  TODO: maybe get the list from HybridCLRSettings.hotUpdateAssemblyDefinitions in the future
        [SerializeField] private List<string> hotUpdateDlls;
        public string ServerBaseUrl => serverBaseUrl;
        public string ProjectName => projectName;
        public string ResPackagePath => resPath;
        public string HotUpdateDLLBasePath => hotUpdateDllBasePath;
        public List<string> HotUpdateDLLs => hotUpdateDlls;

        public string GetResFullPath(RuntimePlatform platform)
        {
            string PlatformFolder = "INVALID";
            
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    PlatformFolder = "Windows";
                    break;
                case RuntimePlatform.Android:
                    PlatformFolder = "Android";
                    break;
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    PlatformFolder = "Mac";
                    break;
                case RuntimePlatform.IPhonePlayer:
                    PlatformFolder = "iOS";
                    break;
            }
            
            return $"{serverBaseUrl}/{projectName}/{PlatformFolder}/{resPath}";
        }

        public string GetResourcePackageName(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "WindowsPackage";
                case RuntimePlatform.Android:
                    return "AndroidPackage";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "MacOSPackage";
                case RuntimePlatform.IPhonePlayer:
                    return "iOSPackage";
            }
            return "INVALID";
        }

        public string GetRawFilePackageName(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "WindowsRawFile";
                case RuntimePlatform.Android:
                    return "AndroidRawFile";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "MacOSRawFile";
                case RuntimePlatform.IPhonePlayer:
                    return "iOSRawFile";
            }
            return "INVALID";
        }
    }
}