using CycloneGames.HotUpdate;
using CycloneGames.UIFramework;
using Cysharp.Threading.Tasks;
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
            UnloadLaunchScene().Forget();
        }

        async UniTask UnloadLaunchScene()
        {
            uiService.OpenUI(UI.PageName.StartUpPage);
            await SceneManager.UnloadSceneAsync("Scene_Launch");
            yooAssetService.UnloadUnusedAssets();
            uiService.CloseUI(UI.PageName.TitlePage);
            uiService.CloseUI(UI.PageName.AssetUpdatePage);
        }
    }
}