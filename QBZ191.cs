using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace QBZ191
{
    /// <summary>
    /// QBZ-191 增强插件：
    ///   1) 真·半自动（按一下打一发）
    ///   2) 黑球修复：开火时那颗黑色小球是 ItemGun.bulletPrefab 材质丢失导致，
    ///      把 bulletPrefab 换成「渲染器全部关闭」的副本，子弹照常命中但看不见。
    ///
    /// 枪声已经烘焙进 QBZ191.repobundle（4.00 秒 / 48kHz / 双声道），插件不再处理声音。
    /// 只影响名字里带 QBZ191 的物品。
    /// </summary>
    [BepInPlugin("cn_xc.QBZ191", "QBZ-191", "0.1.5")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private static MethodInfo _toggleItem;
        private static MethodInfo _localViewId;

        internal static ManualLogSource Log;
        private static ConfigEntry<bool> _hideBullet;
        private static ConfigEntry<bool> _logPrefabs;

        private static readonly HashSet<int> _applied = new HashSet<int>();

        private void Awake()
        {
            Log = Logger;

            _hideBullet = Config.Bind("修复", "隐藏黑色小球", true,
                "开火时的黑色小球其实是子弹模型 bulletPrefab 材质丢失。开启后子弹照常命中，只是看不见。");
            _logPrefabs = Config.Bind("调试", "输出预制体信息", false,
                "把 bulletPrefab 的名字与渲染器材质写进日志。");

            try
            {
                _toggleItem = AccessTools.Method(typeof(ItemToggle), "ToggleItem", new Type[] { typeof(bool), typeof(int) });
                try { _localViewId = AccessTools.Method(typeof(SemiFunc), "PhotonViewIDPlayerAvatarLocal"); } catch { }
                MethodInfo shoot = AccessTools.Method(typeof(ItemGun), "Shoot", new Type[0]);
                _harmony = new Harmony("cn_xc.QBZ191");
                if (shoot != null && _toggleItem != null)
                {
                    _harmony.Patch(shoot,
                        new HarmonyMethod(typeof(Plugin).GetMethod("ShootPrefix", BindingFlags.Static | BindingFlags.NonPublic)),
                        new HarmonyMethod(typeof(Plugin).GetMethod("ShootPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                    Log.LogInfo("QBZ-191 已加载（半自动 + 黑球修复）。");
                }
                else
                {
                    Log.LogWarning("QBZ-191：找不到 ItemGun.Shoot 或 ItemToggle.ToggleItem。");
                }
            }
            catch (Exception ex)
            {
                Log.LogError("QBZ-191 补丁加载失败: " + ex);
            }
        }

        internal static bool IsOurGun(ItemGun gun)
        {
            try
            {
                if (gun == null) return false;
                string n = gun.gameObject.name;
                return !string.IsNullOrEmpty(n) && n.IndexOf("QBZ191", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch { return false; }
        }

        private static void Apply(ItemGun gun)
        {
            try
            {
                int id = gun.gameObject.GetInstanceID();
                bool first = _applied.Add(id);
                if (!_hideBullet.Value || gun.bulletPrefab == null) return;

                GameObject src = gun.bulletPrefab;
                if (first && _logPrefabs.Value)
                {
                    Renderer[] rs = src.GetComponentsInChildren<Renderer>(true);
                    Log.LogInfo("[预制体] bulletPrefab=" + src.name + " 渲染器数=" + rs.Length);
                    for (int i = 0; i < rs.Length && i < 6; i++)
                    {
                        Material m = rs[i].sharedMaterial;
                        Log.LogInfo("  [" + i + "] " + rs[i].name + " 材质=" + (m != null ? m.name : "(null)")
                            + " 着色器=" + (m != null && m.shader != null ? m.shader.name : "(null)"));
                    }
                }
                if (src.name.EndsWith("_NoVisual")) return;

                GameObject clone = UnityEngine.Object.Instantiate(src);
                clone.name = src.name + "_NoVisual";
                Renderer[] all = clone.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < all.Length; i++) all[i].enabled = false;
                gun.bulletPrefab = clone;
                Log.LogInfo("[黑球] bulletPrefab 已换成无渲染副本（原 " + src.name + "，隐藏渲染器 " + all.Length + " 个）");
            }
            catch (Exception ex) { Log.LogWarning("应用失败：" + ex.Message); }
        }

        internal static void ShootPrefix(ItemGun __instance)
        {
            try
            {
                if (!IsOurGun(__instance)) return;
                Apply(__instance);
            }
            catch { }
        }

        internal static void ShootPostfix(ItemGun __instance, bool __result)
        {
            try
            {
                if (!IsOurGun(__instance)) return;
                if (!__result) return;
                ItemToggle toggle = __instance.GetComponent<ItemToggle>();
                if (toggle == null || !toggle.toggleState) return;
                int viewId = 0;
                try { if (_localViewId != null) viewId = (int)_localViewId.Invoke(null, null); } catch { }
                _toggleItem.Invoke(toggle, new object[] { false, viewId });
            }
            catch { }
        }
    }
}
