using UnityEngine;
using YooAsset;
using Zenject;
using CycloneGames.HotUpdate;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

public class Boot : MonoBehaviour
{
    [Inject] private IYooAssetService yooAssetService;

    [SerializeField] private EPlayMode playModeInEditor = EPlayMode.EditorSimulateMode;

    private EPlayMode runtimePlayMode = EPlayMode.HostPlayMode;

    private void Awake()
    {
#if UNITY_EDITOR
        runtimePlayMode = playModeInEditor;
#endif

        Debug.Log($"CurrentPlayMode: {runtimePlayMode.ToString()}");
    }

    private void Start()
    {
        yooAssetService.RunHotUpdateDone += EnterGameScene;
        yooAssetService.RunHotUpdateTasks(runtimePlayMode);
    }

    void EnterGameScene()
    {
        DelayEnterGameScene(500).Forget();
    }

    async UniTask DelayEnterGameScene(int milliSecond)
    {
        await UniTask.Delay(milliSecond);
        await yooAssetService.LoadSceneAsync("Scene_StartUp", LoadSceneMode.Additive);
    }
}