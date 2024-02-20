using System.Collections;
using CycloneGames.HotUpdate;
using CycloneGames.UIFramework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace StartUp.Gameplay
{
    public class DemoScript : MonoBehaviour
    {
        [Inject] private IYooAssetService yooAssetService;
        [Inject] private IUIService uiService;
        private void Start()
        {
            StartDemo();
        }

        void StartDemo()
        {
            StartCoroutine(UnloadLaunchScene());
        }

        IEnumerator UnloadLaunchScene()
        {
            uiService.OpenUI(UI.PageName.StartUpPage);
            var UnloadSceneTask = SceneManager.UnloadSceneAsync("Scene_Launch");
            yield return new WaitUntil(() => UnloadSceneTask.isDone);
            yooAssetService.UnloadUnusedAssets();
            uiService.CloseUI(UI.PageName.TitlePage);
            uiService.CloseUI(UI.PageName.AssetUpdatePage);
        }
    }
}