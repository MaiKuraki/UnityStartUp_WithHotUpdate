namespace CycloneGames.HotUpdate
{
    public class YooAssetBuildinQueryServices : YooAsset.IBuildinQueryServices
    {
        public bool Query(string packageName, string fileName, string fileCRC)
        {
            //  TODO:   There is no inner resource package, always return false here. maybe in the future,some in-package resource will be placed in StreamingAssets.
            return false;
        }
    }
}