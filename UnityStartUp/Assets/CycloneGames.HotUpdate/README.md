## CAUTION: 
-   You must drag the Cyclonegames.HotUpdate/Prefabs/ZenjectInstaller/YooAssetInstaller to your ProjectContext's PrefabInstaller list to make the hot update working.
-   You must configure your own 'YooAssetData' in the ScriptableObject. The AssetServerURL must be your own storage server URL.
-   If HybridCLR hotfix is enabled, the Installer in Zenject's ProjectContext prefab cannot include non-AOT scripts, which means it cannot include scripts related to hotfix. Including non-AOT scripts will result in Zenject failing to initialize, causing errors in the Boot script.
-   如果启用了 HybridCLR 热更新，在 Zenject 的 ProjectContext 预设体中，Installer 不能包含非 AOT 的脚本即不能包含热更新的代码脚本，如果包含非AOT脚本，将会导致 Zenject 无法初始化，导致 Boot 脚本会报错。
-   If the game has already been launched, the SceneManager should not load the Launch_Scene twice.