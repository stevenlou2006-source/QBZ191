# QBZ191Simple

给 **R.E.P.O.** 加一把可购买、可开枪的 **QBZ-191** 步枪。

- 一个 `.repobundle`（物品本体，走 **REPOLib** 的 Content 注册）
- 一个小插件 `.dll`（把游戏原生的"开关式扳机"改成**按一下打一发**的真半自动）

## 依赖

| 依赖 | 说明 |
|---|---|
| BepInEx 5.4.x | 必需 |
| [REPOLib](https://thunderstore.io/c/repo/p/Zehs/REPOLib/) | 必需（加载 `.repobundle` 并注册物品）|
| 原版枪的模型/贴图**不需要** | 本包自带模型与材质 |

## 参数

| 项 | 值 |
|---|---|
| 类型 | Gun（出现在商店武器区）|
| 伤害 | 玩家 90 / 敌人 90（不是一击必杀）|
| 射速 | **半自动**，按一下打一发；冷却 0.12s |
| 射程 | 100 |
| 后坐力 | 2.5 |
| 弹药 | 电池，满电 **30 发** |
| 价格 | 基础 4500~5000（商店 ×4 = 18000~20000）|
| 卡车/商店上限 | 各 2 把 |

## 安装

1. 安装 BepInEx 与 REPOLib
2. 把 `dist/QBZ191.repobundle` 和 `dist/QBZ191Simple.dll` 放进 `BepInEx/plugins/cn_xc-QBZ191/`
3. 启动游戏，商店里会多出 `QBZ-191`

## 从源码构建

### 插件（半自动）

```
csc / MSBuild QBZ191Simple.csproj
```
需要把 `Assembly-CSharp.dll`、`BepInEx.dll`、`0Harmony.dll`、`Photon*.dll`、`UnityEngine*.dll` 放在本目录的**上一级**（与其它 REPO 模组工程一致）。

### 物品包（`.repobundle`）

需要 **Unity 2022.3.62f3** + 已用 *Unity Project Patcher* 打过补丁的 REPO 工程（`Assets/REPO` 为游戏原生资源）：

1. 把 `unity/QBZ191Builder.cs` 放到 `Assets/QBZ191MOD/Editor/`
2. 把 `dist/QBZ191.fbx` 放到 `Assets/QBZ191MOD/`
3. 菜单 **REPO → 生成 QBZ191 枪械包**

脚本会自动：换算模型尺寸 → 建 prefab（子物体名字按游戏 `Find()` 要求命名为 `Shoot Line` / `Hit Effect` / `Particle Sparks` 等）→ 建 `Item`/`Value`/`PhysAttribute`/`ColorPresets`/REPOLib `Mod`+`ItemContent` → 打 AssetBundle → 复制到 plugins。

## 已知限制

- **真半自动靠插件**：游戏原生 `ItemGun` 的扳机是开关式（一开就持续开火），插件在每次射击后把开关复位来实现"一次一发"
- 材质是纯色 Standard（未接贴图）
- 枪口火焰粒子为 Unity 内置粒子（不是原版特效）

## 致谢

- [REPOLib](https://github.com/ZehsTeam/REPOLib) — 物品注册与 bundle 加载
- [R.E.P.O. Modding Wiki](https://repomods.com/) — Unity 工程与 SDK 流程
- 枪声：**M1 Garand Single** by Mike Koenig（[SoundBible](https://soundbible.com/1547-M1-Garand-Single.html)，CC BY 3.0），已裁为 0.30 秒单发