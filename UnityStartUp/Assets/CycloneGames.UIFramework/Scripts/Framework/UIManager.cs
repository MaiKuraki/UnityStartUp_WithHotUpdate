using CycloneGames.HotUpdate;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;
using Zenject;

namespace CycloneGames.UIFramework
{
    internal static class UIPathBuilder
    {
        public static string GetConfigPath(string pageName)
            => $"ScriptableObject_{pageName}";

        public static string GetPrefabPath(string pageName)
            => $"Prefab_{pageName}";
    }

    public class UIManager : MonoBehaviour
    {
        private const string DEBUG_FLAG = "[UIManager]";
        [Inject] private ISubscriber<UIMessage> uiMsgSub;
        [Inject] private IYooAssetService yooAssetService;
        [Inject] private UIRoot uiRoot;
        [Inject] private DiContainer diContainer;
        
        private void Start()
        {
            uiMsgSub.Subscribe(msg =>
            {
                if (msg.MessageCode == UIMessageCode.OPEN_UI)
                {
                    OpenUI(msg.Params[0]);
                }

                if (msg.MessageCode == UIMessageCode.CLOSE_UI)
                {
                    CloseUI(msg.Params[0]);
                }
            });
        }

        internal void OpenUI(string PageName)
        {
            OpenUIAsync(PageName).Forget();
        }

        internal void CloseUI(string PageName)
        {
            UILayer layer = uiRoot.TryGetUILayerFromPageName(PageName);

            if (!layer)
            {
                Debug.LogError($"{DEBUG_FLAG} Can not find layer from PageName: {PageName}");
                return;
            }

            layer.RemovePage(PageName);
        }

        async UniTask OpenUIAsync(string PageName)
        {
            Debug.Log($"{DEBUG_FLAG} Attempting to open UI: {PageName}");
            UIPageConfiguration pageConfig = null;
            GameObject pagePrefab = null;

            try
            {
                // Attempt to load the configuration
                pageConfig = await yooAssetService.LoadAssetAsync<UIPageConfiguration>(
                    UIPathBuilder.GetConfigPath(PageName), 
                    AssetHandleReleasePolicy.ReleaseOnComplete);

                // If the configuration load fails, log the error and exit
                if (pageConfig == null)
                {
                    Debug.LogError($"{DEBUG_FLAG} Failed to load UI Config, PageName: {PageName}");
                    return;
                }

                // Attempt to load the Prefab
                pagePrefab = await yooAssetService.LoadAssetAsync<GameObject>(
                    UIPathBuilder.GetPrefabPath(PageName), 
                    AssetHandleReleasePolicy.ReleaseOnComplete);

                // If the Prefab load fails, log the error and exit
                if (pagePrefab == null)
                {
                    Debug.LogError($"{DEBUG_FLAG} Failed to load UI Prefab, PageName: {PageName}");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                // Catch any exceptions, log the error message
                Debug.LogError($"{DEBUG_FLAG} An exception occurred while loading the UI: {PageName}: {ex.Message}");
                // Perform any necessary cleanup here
                return; // Handle the exception here instead of re-throwing it
            }
    
            // If there are no exceptions and the resources have been successfully loaded, proceed to instantiate and setup the UI page
            string layerName = pageConfig.Layer.LayerName;
            UILayer uiLayer = uiRoot.GetUILayer(layerName);
            UIPage uiPage = diContainer.InstantiatePrefab(pagePrefab).GetComponent<UIPage>();
            uiPage.SetPageConfiguration(pageConfig);
            uiPage.SetPageName(PageName);
            uiLayer.AddPage(uiPage);
        }
        
    }
}