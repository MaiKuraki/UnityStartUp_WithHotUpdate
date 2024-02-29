# Unity初始项目模板(包含资源/代码热更新模块)
<p align="center">
    <br> <a href="README.md">English</a> | 中文
</p>

## 关于
该项目基于 DI/IoC 框架，提供了一套类似于虚幻引擎 GameplayFramework 的类型设计。它包括代码和资源的热更新、一个简易的分层 UI 框架以及一个自动化的打包脚本。适用于需要热更新功能的游戏项目，尤其是移动平台上的服务型游戏，本项目可作为理想的起始模板。
## 文件目录结构
-   Assets/CycloneGames (可剔除)
    -   该程序集提供了类似于虚幻引擎 GameplayFramework 的框架设计，包含 GameInstance、World、GameMode、PlayerController 和 PlayerState 等类型。对于熟悉虚幻引擎的用户，提供了舒适的过渡。
    -   [README](./UnityStartUp/Assets/CycloneGames/README_CHN.md)
-   Assets/CycloneGames.Service
    -   该程序集提供了资源管理（Addressable）和显示管理（GraphicsSettings）等服务。
    -   [README](./UnityStartUp/Assets/CycloneGames.Service/README_CHN.md)
-   Assets/CycloneGames.HotUpdate
    -   该程序集提供了项目的热更新功能，包括 YooAsset（资源热更新）和 HyBridCLR（代码热更新）。
    -   [README](./UnityStartUp/Assets/CycloneGames.HotUpdate/README_CHN.md)
-   Assets/CycloneGames.UIFramework
    -   该程序集提供了一个简易的分层 UI 框架。
    -   它依赖于 CycloneGames.Service 中的 Addressable 和 CycloneGames.HotUpdate 中的 YooAsset 用于加载 UI Prefab，目前无法消除依赖关系。
    -   [README](./UnityStartUp/Assets/CycloneGames.UIFramework/README_CHN.md)
-   Assets/StartUp
    -   该文件夹是 Sample 项目的目录，提供了一个启动场景和开始场景，用作热更新测试。
    -   [README](./UnityStartUp/Assets/StartUp/README.md)