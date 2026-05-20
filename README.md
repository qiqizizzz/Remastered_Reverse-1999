# Remastered Reverse: 1999 

演示视频链接： [复刻重返未来1999演示视频](https://www.bilibili.com/video/BV1GmXGBWE9i/?share_source=copy_web&vd_source=cda38e122deb042ea0b8dff342c81240)

## 项目简介
本项目是一个基于《重返未来：1999》核心玩法的由本人独立完成的复刻项目（复刻了基本的内容，只是一个Demo）。项目涵盖了从底层架构、数据库持久化、网络通信到核心战斗系统的前后端完整闭环。整体采用了 **MVC 架构**，深度应用了多种**面向对象设计模式（如命令模式、状态模式）**。

## 技术栈与核心架构
* **整体架构**：MVC (Model-View-Controller) 设计模式，确保数据、逻辑与表现的解耦。
* **战斗架构**：
  * **命令模式 (Command Pattern)**：卡牌的所有操作被严格封装为命令对象，完美支持动作队列处理、复杂结算顺序及前后端状态同步。
  * **状态模式 (State Pattern)**：PVE 玩法中，所有实体角色（敌方与英雄）的逻辑皆使用状态机（FSM）进行封装，使得状态切换（受击、施法、待机等）清晰且易于扩展。
* **前后端与存储**：基于本地服务端与客户端（C/S 架构）实现，并接入 **MySQL** 实现真正的用户数据持久化存储。
* **数据通信**：使用 Protocol Buffers (Protobuf) 进行高效的序列化与网络数据交互。
* **资源管理**：基于 Addressables (AA包) 与对象池（Object Pool）的高效 `ResManager`。
* **视觉表现**：自己制作的水面特效与其他特效等。

## 目录

- [服务端](https://github.com/qiqizizzz/Remastered_Reverse-1999/tree/main/AAAGameServer/ConsoleMock_Server/GameServer)
- [Module模块](https://github.com/qiqizizzz/Remastered_Reverse-1999/tree/main/Assets/Scripts/Module)
- [Protobuf](https://github.com/qiqizizzz/Remastered_Reverse-1999/tree/main/Assets/Scripts/%5Buseful%5D)

---

## 感想
> **开发者注**：本项目时间为：*2026.3 - 2026.5*，从0到1实现了现在比较完整的游戏Demo，有所收获，虽然还不是很完美，未来希望可以继续优化~





