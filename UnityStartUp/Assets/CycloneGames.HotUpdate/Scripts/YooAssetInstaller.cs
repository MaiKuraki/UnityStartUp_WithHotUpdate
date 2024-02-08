using Zenject;


namespace CycloneGames.HotUpdate
{
    public class YooAssetInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<YooAssetService>().AsSingle().NonLazy();
        }
    }
}