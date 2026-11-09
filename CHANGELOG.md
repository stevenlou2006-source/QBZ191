## [0.1.5]

> 测试范围：**仅单人游戏测试**，联机未验证。

### 变更
- **枪声更闷**：对枪声做了低通滤波（截止 3.5kHz，两级），高频能量大幅减少，听感更沉
  - 频谱重心 3924Hz → 1605Hz，8kHz 以上能量降低约 89%
  - 仍为 48kHz / 双声道 / 1.024 秒，烘焙在 `QBZ191.repobundle` 内

### 说明
- 其余功能（半自动、电池弹药、命中与弹道、黑球修复、`soundShootGlobal` 静音）不变

## [0.1.4]

> 测试范围：**仅单人游戏测试**，联机未验证。

### 变更
- **枪声换成新音频**：48kHz / 双声道 / 1.024 秒，在 Unity 工程里替换后烘焙进 `QBZ191.repobundle`
  - 不裁剪、不改增益，保留原始听感

### 说明
- 其余功能（半自动、电池弹药、命中与弹道、黑球修复、`soundShootGlobal` 静音）沿用 0.1.3

## [0.1.3]

> 测试范围：**仅单人游戏测试**，联机未验证。

### 变更
- **枪声换成新素材**：4.00 秒 / 48kHz / 单声道，在 Unity 工程里替换后烘焙进 `QBZ191.repobundle`
  - 插件不加载任何外部音频，也没有运行时替换 → 玩家无法替换枪声
- 依赖维持 `REPOLib 4.3.0`

### 修复
- **一次开枪听到两声**：游戏 `ItemGun.Shoot()` 会连续播 `soundShoot` 与 `soundShootGlobal` 两条，
  此前两条都指向同一个枪声。现在把 `soundShootGlobal` 设为**静音**（音量 0），只保留本地那条。
- **开火时的黑色小球**：那是子弹模型 `ItemGun.bulletPrefab` 材质丢失导致。
  插件把 `bulletPrefab` 替换为「渲染器全部关闭」的副本 —— 子弹照常生成、照常命中，只是看不见。
  - 配置 `[修复] 隐藏黑色小球`，默认开启；关掉会重新出现黑球

### 说明
- 半自动、电池弹药、命中判定、弹道均沿用 0.1.x，未改动
- 枪声音效由 AI 音效工具生成（[SummerEngine / ElevenLabs text-to-sound v2](https://www.summerengine.com/zh/create/audio?mode=sfx&model=eleven_text_to_sound_v2)）

## [0.1.2]

### 变更
- 依赖更新：REPOLib 4.2.0 → 4.3.0（物品仍走 REPOLib 的 Content 注册，玩法不变）。

### 说明
- 除版本号外，插件代码与 0.1.1 完全一致。

## [0.1.1]

### 修复
- **电池 UI 完全保持原版**：此前为了让电池格文字不显示异常而隐藏/删除过 UI Canvas，导致 `ItemBattery.Update()` 每帧抛 `NullReferenceException`（单人测试中日志出现上千条）。现改为不改动原版电池 UI，异常归零。
- 枪口火焰收紧为短寿命高密度粒子（不再「散开的烟」）。

### 说明
- 仅经过单人游戏测试，联机未验证。

## [0.1.0]

> 测试范围：**仅单人游戏测试**，联机未验证。

### 新增
- QBZ-191 自定义枪械物品（REPOLib Content 注册，`.repobundle` 自包含）
- 半自动插件：按一下打一发（游戏原生扳机为开关式，一开即持续开火）
- 电池弹药：满电 30 发
- 命中判定：`Hurt Collider` 按游戏 `ActivateAll()` 要求挂为独立子物体
- 弹道：`Shoot Line`（LineRenderer，4 顶点）
- 枪口火焰 / 命中粒子 / 命中灯

### 修复
- 修复「开枪即 NullReferenceException」：`ItemGunMuzzleFlash` 与 `ItemGunBullet` 会用 `transform.Find()` 按名字找粒子与灯，缺失即崩，导致子弹不生成、无伤害、无弹道
- 修复粒子材质找不到 shader 导致的紫红色渲染
