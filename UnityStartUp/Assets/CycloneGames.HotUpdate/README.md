## About HotUpdate
-   This hotupdate module utilizes the HybridCLR + YooAsset framework to implement code hotfix and asset updates.
    -   [HybridCLR](https://github.com/focus-creative-games/hybridclr) code hotfix
    -   [YooAsset](https://github.com/tuyoogame/YooAsset) asset hot update
## CAUTION: 
-   You must drag the Cyclonegames.HotUpdate/Prefabs/ZenjectInstaller/YooAssetInstaller to your ProjectContext's PrefabInstaller list to make the hot update working.
-   You must configure your own 'YooAssetData' in the ScriptableObject. The AssetServerURL must be your own storage server URL.
-   If HybridCLR hotfix is enabled, the Installer in Zenject's ProjectContext prefab cannot include non-AOT scripts, which means it cannot include scripts related to hotfix. Including non-AOT scripts will result in Zenject failing to initialize, causing errors in the Boot script.
-   If the game has already been launched, the SceneManager should not load the Launch_Scene twice.