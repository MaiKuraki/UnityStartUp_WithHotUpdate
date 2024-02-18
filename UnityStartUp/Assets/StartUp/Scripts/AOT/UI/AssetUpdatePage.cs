using CycloneGames.HotUpdate;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using CycloneGames.UIFramework;
using Cysharp.Threading.Tasks;

public class AssetUpdatePage : UIPage
{
    [Inject] private IYooAssetService yooAssetService;

    [SerializeField] private Transform AssetsUpdatePanelTF;
    [SerializeField] private Slider HotUpdateProgressBar;
    [SerializeField] private TMP_Text PackageProgressText;
    [SerializeField] private TMP_Text PackageNameText;

    protected override void Awake()
    {
        base.Awake();

        RegisterYooAssetServiceEvent().Forget();
    }

    protected override void Start()
    {
        base.Start();

        HotUpdateProgressBar.value = 0;
        AssetsUpdatePanelTF.gameObject.SetActive(false);
    }

    async UniTask RegisterYooAssetServiceEvent()
    {
        await UniTask.WaitUntil(() => yooAssetService.IsServiceReady());
        
        yooAssetService.RegisterHotUpdateDownloadCallback(
            OnHotUpdateStartDownload: UpdateStartDownloadUI,
            OnHotUpdateFinishDownload: UpdateFinishDownloadUI,
            OnHotUpdateProgressUpdate: UpdateDownloadProgressUI
        );

        yooAssetService.RunHotUpdateDone += OnHotUpdateCompleted;
    }

    void UpdateStartDownloadUI(string packageNameStr)
    {
        AssetsUpdatePanelTF.gameObject.SetActive(true);
        PackageNameText.text = $"Update Assets [{packageNameStr}]";
    }

    void UpdateFinishDownloadUI(string packageNameStr)
    {
    }

    void UpdateDownloadProgressUI(float progress)
    {
        HotUpdateProgressBar.value = progress;
        string progressText = $"{(progress * 100):f2}%";
        PackageProgressText.text = progressText;
    }

    void OnHotUpdateCompleted()
    {
        AssetsUpdatePanelTF.gameObject.SetActive(false);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        yooAssetService.UnRegisterHotUpdateDownloadCallback(
            OnHotUpdateStartDownload: UpdateStartDownloadUI,
            OnHotUpdateFinishDownload: UpdateFinishDownloadUI,
            OnHotUpdateProgressUpdate: UpdateDownloadProgressUI
        );

        yooAssetService.RunHotUpdateDone -= OnHotUpdateCompleted;
    }
}