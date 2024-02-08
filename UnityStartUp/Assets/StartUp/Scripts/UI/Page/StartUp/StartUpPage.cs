using CycloneGames.HotUpdate;
using CycloneGames.UIFramework;
using TMPro;
using UnityEngine;
using Zenject;

namespace StartUp.UI
{
    public class StartUpPage : UIPage
    {
        [Inject] private IYooAssetService yooAssetService;
        
        [SerializeField] private TMP_Text Text_Version;

        protected override void Start()
        {
            base.Start();
            
            Text_Version.text = yooAssetService.GetRawFilePackageVersion();
        }
    }
}