using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace QBZ191Simple
{
    /// <summary>
    /// QBZ-191 真·半自动（按一下打一发）。
    ///
    /// 游戏机制：枪的扳机是 ItemToggle（开关式）。一旦打开，ItemGun 在 Update 里会不断
    /// 尝试开火（受 shootCooldown 限制）→ 表现就是连发/加特林。
    ///
    /// 做法：在 ItemGun.Shoot() 成功打出一发之后，立刻把 ItemToggle 复位成关闭。
    /// 这样下一发必须重新按一下扳机 → 严格 1 次按键 = 1 发。
    /// 只影响名字里带 QBZ191 的物品。
    /// </summary>
    [BepInPlugin("cn_xc.QBZ191Simple", "QBZ-191 SemiAuto", "0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        private static MethodInfo _toggleItem;
        private static MethodInfo _localViewId;

        private void Awake()
        {
            try
            {
                _toggleItem = AccessTools.Method(typeof(ItemToggle), "ToggleItem", new Type[] { typeof(bool), typeof(int) });
                try { _localViewId = AccessTools.Method(typeof(SemiFunc), "PhotonViewIDPlayerAvatarLocal"); } catch { }
                MethodInfo shoot = AccessTools.Method(typeof(ItemGun), "Shoot", new Type[0]);
                _harmony = new Harmony("cn_xc.QBZ191Simple");
                if (shoot != null && _toggleItem != null)
                {
                    _harmony.Patch(shoot, null, new HarmonyMethod(typeof(Plugin).GetMethod("ShootPostfix", BindingFlags.Static | BindingFlags.NonPublic)));
                    Logger.LogInfo("QBZ-191 半自动补丁已加载（Shoot 后置复位）。");
                }
                else
                {
                    Logger.LogWarning("QBZ-191 半自动补丁：找不到 ItemGun.Shoot 或 ItemToggle.ToggleItem。");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("QBZ-191 补丁加载失败: " + ex);
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

        internal static void ShootPostfix(ItemGun __instance, bool __result)
        {
            try
            {
                if (!__result) return;                     // 没打出去（没电/哑火）就不复位
                if (!IsOurGun(__instance)) return;
                ItemToggle toggle = __instance.GetComponent<ItemToggle>();
                if (toggle == null || !toggle.toggleState) return;
                int id = 0;
                try { if (_localViewId != null) id = (int)_localViewId.Invoke(null, null); } catch { }
                _toggleItem.Invoke(toggle, new object[] { false, id });   // 复位 → 必须再按一次
            }
            catch { }
        }
    }
}
