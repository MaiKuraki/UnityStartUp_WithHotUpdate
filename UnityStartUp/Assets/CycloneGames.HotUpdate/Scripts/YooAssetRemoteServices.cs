using YooAsset;

namespace CycloneGames.HotUpdate
{
    public class YooAssetRemoteServices : IRemoteServices
    {
        private string defaultHostServer;
        private string fallbackHostServer;
        public YooAssetRemoteServices(string NewDefaultHostServer, string NewFallbackHostServer)
        {
            defaultHostServer = NewDefaultHostServer;
            fallbackHostServer = NewFallbackHostServer;
        }
        public string GetRemoteMainURL(string fileName)
        {
            return $"{defaultHostServer.TrimEnd('/')}/{fileName}";
        }

        public string GetRemoteFallbackURL(string fileName)
        {
            return $"{fallbackHostServer.TrimEnd('/')}/{fileName}";
        }
    }
}