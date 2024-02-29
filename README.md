# Unity StartUp Project Template(Include HotUpdate Module)
<p align="center">
    <br> English | <a href="README_CHN.md">中文</a>
</p>

## Overview
This project is built upon a DI/IoC framework and presents a type design reminiscent of Unreal Engine's GameplayFramework. It incorporates code and resource hot updates, a straightforward hierarchical UI framework, and an automated packaging script. Tailored for games requiring hot update functionality, especially service-oriented games on mobile platforms, this project serves as an ideal starting template.
## File Directory Structure
-   Assets/CycloneGames (Optional Exclusion)
    -   This assembly provides a framework design akin to Unreal Engine's GameplayFramework, featuring types such as GameInstance, World, GameMode, PlayerController, and PlayerState. It offers a comfortable transition for users familiar with Unreal Engine.
    -   [README](./UnityStartUp/Assets/CycloneGames/README.md)
-   Assets/CycloneGames.Service
    -   This assembly delivers services such as resource management (Addressable) and display management (GraphicsSettings).
    -   [README](./UnityStartUp/Assets/CycloneGames.Service/README.md)
-   Assets/CycloneGames.HotUpdate
    -   This assembly introduces the hot update functionality of the project, encompassing YooAsset (resource hot updates) and HyBridCLR (code hot updates).
    -   [README](./UnityStartUp/Assets/CycloneGames.HotUpdate/README.md)
-   Assets/CycloneGames.UIFramework
    -   This assembly offers a simple hierarchical UI framework.
    -   It relies on Addressable from CycloneGames.Service and YooAsset from CycloneGames.HotUpdate for loading UI Prefabs, currently unable to eliminate dependencies.
    -   [README](./UnityStartUp/Assets/CycloneGames.UIFramework/README.md)
-   Assets/StartUp
    -   This folder serves as the directory for the Sample project, providing a startup scene and initial scenes for hot update testing.
    -   [README](./UnityStartUp/Assets/StartUp/README.md)