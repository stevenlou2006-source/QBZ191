# QBZ191

给 **R.E.P.O.** 加一把可购买、可开枪的 **QBZ-191** 步枪。

- 一个 `.repobundle`（物品本体，走 **REPOLib** 的 Content 注册）
- 一个小插件 `.dll`（把游戏原生的"开关式扳机"改成**按一下打一发**的真半自动）

## 依赖

| 依赖 | 说明 |
|---|---|
| BepInEx 5.4.x | 必需 |
| [REPOLib](https://thunderstore.io/c/repo/p/Zehs/REPOLib/) | 必需（加载 `.repobundle` 并注册物品）|

## 参数

| 项 | 值 |
|---|---|
| 类型 | Gun（出现在商店武器区）|
| 伤害 | 玩家 90 / 敌人 90（不是一击必杀）|
| 射速 | **半自动**，按一下打一发；冷却 0.28s |
| 射程 | 100 |
| 后坐力 | 2.5 |
| 弹药 | 电池，满电 **30 发** |
| 价格 | 基础 4500~5000（商店 ×4 = 18000~20000）|
| 卡车/商店上限 | 各 2 把 |

## 安装

1. 安装 BepInEx 与 REPOLib
2. 把 `QBZ191.repobundle` 和 `QBZ191.dll` 放进 `BepInEx/plugins/cn_xc-QBZ191/`
3. 启动游戏，商店里会多出 `QBZ-191`

## 已知限制

- **真半自动靠插件**：游戏原生 `ItemGun` 的扳机是开关式（一开就持续开火），插件在每次射击后把开关复位来实现"一次一发"
- 枪身是纯色材质（未使用模型自带的贴图）
- 枪口火焰为自建粒子（不是原版特效）

## 模型来源与许可证

本模组使用的枪械模型：

| 项 | 内容 |
|---|---|
| 模型 | **QBZ191** |
| 作者 | **会飞的购物清单 (flyinglist1)** |
| 模型页面 | https://sketchfab.com/3d-models/qbz191-ccdbe1834e0f49ff9a371b9f51190208 |
| 作者主页 | https://sketchfab.com/flyinglist1 |
| 许可证 | **Creative Commons Attribution 4.0 (CC BY 4.0)** |

> 依据 CC BY 4.0，使用本模型需**注明作者与原链接**；允许商用与修改，无需额外授权。

## 致谢

- [REPOLib](https://github.com/ZehsTeam/REPOLib) — 物品注册与 bundle 加载
- [R.E.P.O. Modding Wiki](https://repomods.com/) — Unity 工程与 SDK 流程
- 模型：**QBZ191** by 会飞的购物清单 (flyinglist1)，CC BY 4.0
- 枪声：**M1 Garand Single** by Mike Koenig（[SoundBible](https://soundbible.com/1547-M1-Garand-Single.html)，CC BY 3.0），已裁切并做后期处理
