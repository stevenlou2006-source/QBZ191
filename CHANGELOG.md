# Changelog

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
- 修复"开枪即 NullReferenceException"：`ItemGunMuzzleFlash` 与 `ItemGunBullet` 会用 `transform.Find()` 按名字找粒子与灯，缺失即崩，导致子弹不生成、无伤害、无弹道
- 修复粒子材质找不到 shader 导致的紫红色渲染