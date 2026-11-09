# QBZ191

给 **R.E.P.O.** 加一把可购买、可开枪的 **QBZ-191 步枪**。

- 一个 `.repobundle`（物品本体，走 **REPOLib** 的 Content 注册）
- 一个小插件 `.dll`（半自动扳机 + 黑球修复）
- 枪声（1.024 秒 / 48kHz / 双声道）已烘焙进 `.repobundle`

> ⚠️ **测试范围**：本版本**仅经过单人游戏测试**（模型、开枪、弹道、伤害、电量、枪声）。
> 联机（多人）环境下尚未验证，可能出现同步或表现差异，欢迎反馈。

交流：**#腾讯频道：pd77525911**

---

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

---

## 功能

| 功能 | 说明 |
|---|---|
| **真半自动** | 游戏原生扳机是开关式（一开就持续开火），插件在每次射击后复位开关 → 一次按键 = 一发 |
| **枪声烘焙进模型** | 1.024 秒 / 48kHz / 双声道，已打包进 `QBZ191.repobundle`；插件不加载任何外部音频，玩家无法替换 |
| **黑球修复** | 开火时那颗黑色小球是子弹模型 `bulletPrefab` 材质丢失导致。现在子弹照常命中，但看不见 |

---

## 枪声（已烘焙进模型，不可替换）

枪声是在 Unity 工程里替换后**重新打包进 `QBZ191.repobundle`** 的：
**1.024 秒 / 48kHz / 双声道 16bit**。
插件不读取任何外部文件，也**没有**运行时替换 → 玩家无法替换枪声。

配置项（`BepInEx/config/cn_xc.QBZ191.cfg`）：

| 配置 | 默认 | 说明 |
|---|---:|---|
| `[修复] 隐藏黑色小球` | true | 关掉会重新出现那颗黑球 |
| `[调试] 输出预制体信息` | false | 排查用 |

---

## 依赖

| 依赖 | 说明 |
|---|---|
| BepInEx 5.4.x | 必需 |
| [REPOLib](https://thunderstore.io/c/repo/p/Zehs/REPOLib/) **4.3.0** | 必需（加载 `.repobundle` 并注册物品）|

---

## 安装

1. 安装 BepInEx 与 REPOLib
2. 把 `QBZ191.dll` 与 `QBZ191.repobundle` 放进 `BepInEx/plugins/cn_xc-QBZ191/`
3. 启动游戏，商店里会多出 `QBZ-191`

---

## 一次开枪两声？已修好

游戏 `ItemGun.Shoot` 会连续播 `soundShoot` 与 `soundShootGlobal` 两条音效。
本模组把 `soundShootGlobal` 设为静音（音量 0），枪声使用完整 4 秒素材，所以**一次开枪只有一声**，尾音也完整。

---

## 已知限制

- **半自动靠插件**：游戏原生 `ItemGun` 的扳机是开关式，插件在每次射击后复位开关来实现「一次一发」
- 枪身是纯色材质（未使用模型自带的贴图）
- 枪口火焰为自建粒子（不是原版特效）
- 仅单人测试，联机未验证

---

## 模型来源与许可证

| 项 | 内容 |
|---|---|
| 模型 | **QBZ191** |
| 作者 | **会飞的购物清单 (flyinglist1)** |
| 模型页面 | https://sketchfab.com/3d-models/qbz191-ccdbe1834e0f49ff9a371b9f51190208 |
| 作者主页 | https://sketchfab.com/flyinglist1 |
| 许可证 | **Creative Commons Attribution 4.0 (CC BY 4.0)** |

> 依据 CC BY 4.0，使用本模型需**注明作者与原链接**；允许商用与修改。

---

## 致谢

- [REPOLib](https://github.com/ZehsTeam/REPOLib) — 物品注册与 bundle 加载
- [R.E.P.O. Modding Wiki](https://repomods.com/) — Unity 工程与 SDK 流程
- **模型**：QBZ191 by 会飞的购物清单 (flyinglist1)，CC BY 4.0
- **枪声（0.1.3 起）**：由 AI 音效工具生成 —— [SummerEngine / ElevenLabs text-to-sound v2](https://www.summerengine.com/zh/create/audio?mode=sfx&model=eleven_text_to_sound_v2)，
  48kHz / 双声道 / 1.024 秒，烘焙进 repobundle
