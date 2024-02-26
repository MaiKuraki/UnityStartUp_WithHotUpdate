## 关于热更新
-   这个热更新模块利用了HybridCLR + YooAsset框架来实现代码热修复和资源更新。
    -   [HybridCLR](https://github.com/focus-creative-games/hybridclr) 用于代码热更新
    -   [YooAsset](https://github.com/tuyoogame/YooAsset) 用于资源热更新
## CAUTION: 
-   您必须将 Cyclonegames.HotUpdate/Prefabs/ZenjectInstaller/YooAssetInstaller 拖动到您的 ProjectContext 的 PrefabInstaller 列表中，才能使热更新正常工作。
-   您必须在 ScriptableObject 中配置自己的 'YooAssetData'。AssetServerURL 必须是您自己的存储服务器 URL。
-   如果启用了 HybridCLR 热更新，Zenject 的 ProjectContext 预制件中的 Installer 不能包含非 AOT 脚本，这意味着它不能包含与热修复相关的脚本。包含非 AOT 脚本将导致 Zenject 无法初始化，从而在引导脚本中引发错误。
-   如果游戏已经启动，SceneManager 不应该加载 Launch_Scene 两次。