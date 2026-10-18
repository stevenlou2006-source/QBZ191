// QBZ191 → REPO 枪械物品
// 路线：克隆原版 Tranq 枪 prefab → 彻底展开嵌套 prefab → 换掉原版粒子/线材质（避免 Particles/Standard Surface 编译崩溃）
//       → 删掉原版模型 → 换 QBZ191 模型 → 参数微调 → 打 .repobundle
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Photon.Pun;

public static class QBZ191Builder
{
    const string MOD_DIR = "Assets/QBZ191MOD";
    const string GEN_DIR = MOD_DIR + "/Generated";
    const string FBX = MOD_DIR + "/QBZ191.fbx";
    const string BUNDLE = "qbz191";
    const string VANILLA_GUN = "Assets/REPO/Game/Resources/items/Item Gun Tranq.prefab";

    const float TARGET_LENGTH = 1.00f;
    const float VERTICAL_NUDGE = 0.06f;
    static readonly Vector3 VISUAL_OFFSET = new Vector3(0f, 0f, -0.05f);
    static readonly Vector3 VISUAL_EULER = new Vector3(0f, 0f, 0f);
    const float RECOIL = 2.5f;
    const float BATTERY_DRAIN = 3.3333f;   // 100 / 3.3333 = 30 发
    const float COOLDOWN = 0.28f;          // 半自动间隔（原版 0.40，调快一点）

    [MenuItem("REPO/生成 QBZ191 枪械包")]
    public static void BuildAll()
    {
        try
        {
            if (!Directory.Exists(GEN_DIR)) Directory.CreateDirectory(GEN_DIR);

            // ---------- 材質 ----------
            var mat = new Material(Shader.Find("Standard"));
            mat.name = "QBZ191 Material";
            mat.color = new Color(0.22f, 0.22f, 0.24f);
            mat.SetFloat("_Metallic", 0.65f);
            mat.SetFloat("_Glossiness", 0.55f);
            SaveAsset(mat, "QBZ191 Material.mat");

            var partMat = new Material(Shader.Find("QBZ191Gun/Particle"));
            partMat.name = "QBZ191 Particle Mat";
            // 圆形柔化贴图：不设的话每个粒子是白色方块（"马赛克一点一点"）
            var ptex = AssetDatabase.LoadAssetAtPath<Texture2D>(GEN_DIR + "/QBZ191 Particle Tex.png");
            if (ptex != null) { partMat.mainTexture = ptex; partMat.SetTexture("_MainTex", ptex); Debug.Log("[QBZ191] 粒子贴图已接上: " + ptex.width + "x" + ptex.height); }
            else Debug.LogWarning("[QBZ191] 没找到粒子贴图（会显示成方块）");
            SaveAsset(partMat, "QBZ191 Particle Mat.mat");

            var lineMat = new Material(Shader.Find("QBZ191Gun/Line"));
            lineMat.name = "QBZ191 Line Mat";
            SaveAsset(lineMat, "QBZ191 Line Mat.mat");
            if (partMat.shader == null || partMat.shader.name != "QBZ191Gun/Particle") Debug.LogWarning("[QBZ191] 自写粒子 shader 没找到，可能渲染异常");
            if (lineMat.shader == null || lineMat.shader.name != "QBZ191Gun/Line") Debug.LogWarning("[QBZ191] 自写线条 shader 没找到，可能渲染异常");

            // ---------- 克隆原版枪并彻底展开 ----------
            var vanilla = AssetDatabase.LoadAssetAtPath<GameObject>(VANILLA_GUN);
            if (vanilla == null) throw new Exception("找不到原版枪 prefab: " + VANILLA_GUN);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(vanilla);
            root.name = "Item Gun QBZ191";

            // 关键：彻底展开所有嵌套 prefab 实例，否则后面换材质只改到外层、原版材质仍会被打包（并触发 shader 编译崩溃）
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Item Gun QBZ191";
            Debug.Log("[QBZ191] 已彻底展开嵌套 prefab");

            // 换掉所有粒子 / 线条材质（内置 shader，不需要现场编译）
            int psrCount = 0, lrCount = 0;
            foreach (var psr in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (psr == null) continue;
                psr.material = partMat;
                psr.trailMaterial = partMat;
                psrCount++;
            }
            foreach (var lr in root.GetComponentsInChildren<LineRenderer>(true))
            {
                if (lr == null) continue;
                lr.material = lineMat;
                lrCount++;
            }
            Debug.Log("[QBZ191] 替换粒子材质 " + psrCount + " 个，线条材质 " + lrCount + " 个");

            // 删掉原版电池 UI（Canvas + TMP 字体）：其字体材质的 shader 不在包内会渲染成紫红色，且对物品无意义
            int canvasKilled = 0;
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas == null) continue;
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                canvasKilled++;
            }
            if (canvasKilled > 0) Debug.Log("[QBZ191] 已移除原版 UI Canvas " + canvasKilled + " 个（含电池 UI 文字）");

            // 删掉原版模型前：先记录原版视觉的局部变换与世界尺寸（用来对齐我们的模型）
            Transform vanillaVisual = null;
            bool hasVanillaBounds = false;
            Bounds vanillaBounds = default(Bounds);
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null) continue;
                if (vanillaVisual == null) { vanillaVisual = r.transform; vanillaBounds = r.bounds; hasVanillaBounds = true; }
                else vanillaBounds.Encapsulate(r.bounds);
            }
            Vector3 vPos = vanillaVisual != null ? vanillaVisual.localPosition : Vector3.zero;
            Quaternion vRot = vanillaVisual != null ? vanillaVisual.localRotation : Quaternion.identity;
            float vLen = hasVanillaBounds ? Mathf.Max(vanillaBounds.size.x, Mathf.Max(vanillaBounds.size.y, vanillaBounds.size.z)) : 0f;
            Debug.Log("[QBZ191] 原版视觉: localPos=" + vPos.ToString("F3") + " rot=" + vRot.eulerAngles.ToString("F1") + " 世界长度=" + vLen.ToString("F3") + "m");

            // 删掉原版枪的模型（带 MeshRenderer 的子物体），保留所有功能子物体
            var kill = new List<GameObject>();
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r == null) continue;
                var t = r.transform;
                if (t == root.transform) continue;
                if (!kill.Contains(t.gameObject)) kill.Add(t.gameObject);
            }
            foreach (var g in kill) UnityEngine.Object.DestroyImmediate(g);
            Debug.Log("[QBZ191] 已移除原版模型子物体 " + kill.Count + " 个");

            // ---------- 换上新模型 ----------
            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FBX);
            if (fbxAsset == null) throw new Exception("找不到 FBX: " + FBX);
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                float longest = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                // 缩放：固定用户认可的 1.00 m（原版视觉包围盒不可靠，实测算出 3.1m）
                float targetLen = TARGET_LENGTH;
                if (longest > 0.001f)
                {
                    float k = targetLen / longest;
                    visual.transform.localScale = visual.transform.localScale * k;
                    Debug.Log("[QBZ191] 模型最长边 " + longest.ToString("F3") + "m → 目标 " + targetLen.ToString("F3") + "m，缩放 " + k.ToString("F4"));
                }
                // 位置/旋转：完全对齐原版视觉
                visual.transform.localPosition = vPos + VISUAL_OFFSET;
                visual.transform.localRotation = vRot * Quaternion.Euler(VISUAL_EULER);
            }

            // 枪口点：贴到模型前端（否则枪口火焰会出现在枪身中段）
            var gunMuzzleT = FindDeep(root.transform, "Gun Muzzle");
            if (gunMuzzleT != null && renderers.Length > 0)
            {
                var wb = renderers[0].bounds;
                foreach (var r in renderers) wb.Encapsulate(r.bounds);
                Vector3 c = root.transform.InverseTransformPoint(wb.center);
                Vector3 s = wb.size;
                int axis = (s.x >= s.y && s.x >= s.z) ? 0 : (s.y >= s.z ? 1 : 2);
                Vector3 pos = c;
                pos[axis] += s[axis] * 0.5f;
                gunMuzzleT.localPosition = pos;
                Debug.Log("[QBZ191] Gun Muzzle 移到模型前端: " + pos.ToString("F3") + "（最长轴=" + axis + "，尺寸=" + s.ToString("F3") + "）");
            }

            // ---------- 资源 ----------
            var phys = ScriptableObject.CreateInstance<PhysAttribute>();
            phys.name = "PhysAttribute QBZ191";
            SetPriv(phys, "mass", 0.5f);
            SaveAsset(phys, "PhysAttribute QBZ191.asset");

            var colors = ScriptableObject.CreateInstance<ColorPresets>();
            colors.name = "ColorPresets QBZ191";
            colors.colorMain = new Color(0.95f, 0.55f, 0.15f, 1f);
            colors.colorLight = new Color(1f, 0.87f, 0.45f, 1f);
            colors.colorDark = new Color(0.35f, 0.18f, 0.05f, 1f);
            SaveAsset(colors, "ColorPresets QBZ191.asset");

            var val = ScriptableObject.CreateInstance<Value>();
            val.name = "Value QBZ191";
            val.valueMin = 4500f;
            val.valueMax = 5000f;
            SaveAsset(val, "Value QBZ191.asset");

            var item = ScriptableObject.CreateInstance<Item>();
            item.name = "Item Gun QBZ191";
            SetPriv(item, "itemAssetName", "Item Gun QBZ191");
            SetPriv(item, "itemName", "QBZ-191");
            SetPriv(item, "itemType", 9);
            SetPriv(item, "emojiIcon", 23);
            SetPriv(item, "itemVolume", 1);
            SetPriv(item, "maxAmount", 2);
            SetPriv(item, "maxAmountInShop", 2);
            SetPriv(item, "maxPurchaseAmount", 2);
            SetPriv(item, "physicalItem", true);
            SetPriv(item, "value", val);
            SetPriv(item, "colorPreset", colors);
            SaveAsset(item, "Item QBZ191.asset");

            // ---------- 接到 prefab ----------
            var attr = root.GetComponent<ItemAttributes>();
            if (attr == null) throw new Exception("克隆出的 prefab 上没有 ItemAttributes");
            attr.item = item;
            SetPriv(attr, "item", item);
            SetPriv(attr, "colorPreset", colors);
            EditorUtility.SetDirty(attr);

            var notVal = root.GetComponent<NotValuableObject>();
            if (notVal != null) { notVal.physAttributePreset = phys; EditorUtility.SetDirty(notVal); }

            var rb = root.GetComponent<Rigidbody>();
            if (rb != null) { rb.mass = 0.8f; EditorUtility.SetDirty(rb); }

            var battery = root.GetComponent<ItemBattery>();
            if (battery != null)
            {
                SetPriv(battery, "itemAttributes", attr);
                SetPriv(battery, "autoDrain", false);
                SetPriv(battery, "batteryBars", 30);        // 弹药格数：明确 30
                SetPriv(battery, "batteryLife", 100f);
                SetPriv(battery, "batteryDrainRate", 1f);
                SetPriv(battery, "hasBatteryVisuals", false);
                EditorUtility.SetDirty(battery);
                Debug.Log("[QBZ191] 电池：格数=30 电量=100");
            }

            var gun = root.GetComponent<ItemGun>();
            if (gun != null)
            {
                // 原版 Tranq 的子弹是"麻醉针"（有模型、慢速），换成我们自己的快速命中子弹
                var vanillaBullet = FindDeep(root.transform, "Bullet");
                if (vanillaBullet != null)
                {
                    UnityEngine.Object.DestroyImmediate(vanillaBullet.gameObject);
                    Debug.Log("[QBZ191] 已删除原版麻醉针子弹模板");
                }
                var bulletPrefab = BuildBullet(mat, lineMat, partMat);
                Debug.Log("[QBZ191] 已生成自己的子弹模板");

                // 自己的枪口火焰（原版麻醉枪的太小/不对）：三粒子 + Shoot Light，游戏按名字 Find
                var flashPrefab = BuildMuzzleFlash(partMat);
                Debug.Log("[QBZ191] 已生成自己的枪口火焰");

                SetPriv(gun, "bulletPrefab", bulletPrefab);
                SetPriv(gun, "muzzleFlashPrefab", flashPrefab);
                SetPriv(gun, "gunRecoilForce", RECOIL);
                SetPriv(gun, "shootCooldown", COOLDOWN);
                SetPriv(gun, "cameraShakeMultiplier", 0.5f);
                SetPriv(gun, "misfirePercentageChange", 0f);
                SetPriv(gun, "batteryDrain", BATTERY_DRAIN);
                SetPriv(gun, "batteryDrainFullBar", true);
                SetPriv(gun, "batteryDrainFullBars", 1);
                // 声音：用真实枪械录音（AR-15 单发裁剪版），空仓用咔哒声
                var shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GEN_DIR + "/QBZ191 Shoot.wav");
                if (shootClip == null) shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GEN_DIR + "/QBZ191 Shoot Synth.wav");
                var emptyClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GEN_DIR + "/QBZ191 Empty.wav");
                if (shootClip != null)
                {
                    SetSound(gun, "soundShoot", shootClip, 0.8f);
                    SetSound(gun, "soundShootGlobal", shootClip, 0.6f);
                    SetSound(gun, "soundHit", emptyClip, 0.5f);
                }
                if (emptyClip != null) SetSound(gun, "soundNoAmmoClick", emptyClip, 0.6f);
                EditorUtility.SetDirty(gun);
                Debug.Log("[QBZ191] 枪参数：后坐力=" + RECOIL + " 每发耗电=" + BATTERY_DRAIN + "（子弹/弹道/粒子沿用原版，声音已换成自己的）");
            }

            var gunPrefab = SavePrefab(root, "Item Gun QBZ191.prefab");

            // ---------- REPOLib: Mod + ItemContent ----------
            var mod = ScriptableObject.CreateInstance<REPOLib.Objects.Sdk.Mod>();
            mod.name = "Mod QBZ191";
            SetPriv(mod, "_name", "QBZ191");
            SetPriv(mod, "_author", "cn_xc");
            SetPriv(mod, "_version", "1.0.0");
            SetPriv(mod, "_description", "QBZ-191 rifle");
            SaveAsset(mod, "Mod QBZ191.asset");

            var pfAttr = gunPrefab.GetComponent<ItemAttributes>();
            var content = ScriptableObject.CreateInstance<REPOLib.Objects.Sdk.ItemContent>();
            content.name = "QBZ191 Content";
            SetPriv(content, "_prefab", pfAttr);
            SaveAsset(content, "QBZ191 Content.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QBZ191] 物品生成完成");
            BuildBundle();
        }
        catch (Exception e)
        {
            Debug.LogError("[QBZ191] 失败: " + e);
        }
    }

    static void BuildBundle()
    {
        try
        {
            // 内置 shader 必须进包，否则运行时会渲染成紫红色
            EnsureAlwaysIncludedShaders("Sprites/Default", "Legacy Shaders/Particles/Alpha Blended",
                                        "TextMeshPro/Mobile/Distance Field", "TextMeshPro/Distance Field");

            var outDir = GEN_DIR + "/BundleOut";
            if (Directory.Exists(outDir)) Directory.Delete(outDir, true);

            foreach (var g in AssetDatabase.FindAssets(string.Empty, new[] { GEN_DIR }))
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (p.Contains("BundleOut")) continue;
                var imp = AssetImporter.GetAtPath(p);
                if (imp != null && !(imp is MonoImporter)) imp.assetBundleName = BUNDLE;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            BuildPipeline.BuildAssetBundles(outDir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);

            var built = outDir + "/" + BUNDLE;
            if (!File.Exists(built)) { Debug.LogError("[QBZ191] 打包失败：未生成 " + built); return; }

            var final = MOD_DIR + "/QBZ191.repobundle";
            if (File.Exists(final)) File.Delete(final);
            File.Copy(built, final);
            Debug.Log("[QBZ191] 打包完成: " + final);

            try
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var dstDir = Path.Combine(appData, "Thunderstore Mod Manager", "DataFolder", "REPO", "profiles", "Default", "BepInEx", "plugins", "cn_xc-QBZ191");
                Directory.CreateDirectory(dstDir);
                File.Copy(final, Path.Combine(dstDir, "QBZ191.repobundle"), true);
                Debug.Log("[QBZ191] 已安装到: " + dstDir);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[QBZ191] 自动安装失败: " + ex.Message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[QBZ191] 打包失败: " + e);
        }
    }

    [MenuItem("REPO/仅打包")]
    public static void BundleOnly() { BuildBundle(); }

    /// <summary>枪口火焰：游戏 ActivateAllEffects() 会在根下 Find("Particle Smoke"/"Particle Impact"/"Particle Sparks") 和 GetComponentInChildren&lt;Light&gt;()。</summary>
    static GameObject BuildMuzzleFlash(Material partMat)
    {
        var flash = new GameObject("MuzzleFlash");
        flash.AddComponent<ItemGunMuzzleFlash>();
        AddPs(flash, "Particle Smoke", new Color(0.55f, 0.55f, 0.55f, 0.45f), partMat);
        AddPs(flash, "Particle Impact", new Color(1f, 0.65f, 0.25f, 0.9f), partMat);
        AddPs(flash, "Particle Sparks", new Color(1f, 0.85f, 0.45f, 1f), partMat);
        var lightGo = new GameObject("Shoot Light");
        lightGo.transform.SetParent(flash.transform, false);
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.range = 6f;
        l.intensity = 4f;
        l.color = new Color(1f, 0.8f, 0.45f);
        l.enabled = true;
        return SavePrefab(flash, "MuzzleFlash.prefab");
    }

    static Transform FindDeep(Transform t, string name)
    {
        if (t == null) return null;
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = FindDeep(t.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>子弹模板：结构完全对齐 AK47（ItemGunBullet + Hurt Collider + Shoot Line + Hit Effect 下三粒子/Hit Light）。</summary>
    static GameObject BuildBullet(Material gunMat, Material lineMat, Material partMat)
    {
        var bullet = new GameObject("Bullet");

        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "bullet";
        body.transform.SetParent(bullet.transform, false);
        body.transform.localScale = Vector3.one * 0.05f;
        var bc = body.GetComponent<SphereCollider>();
        if (bc != null) UnityEngine.Object.DestroyImmediate(bc);
        var mr = body.GetComponent<MeshRenderer>();
        if (mr != null && gunMat != null) mr.sharedMaterial = gunMat;

        var hurtGo = new GameObject("Hurt Collider");
        hurtGo.transform.SetParent(bullet.transform, false);
        var hbox = hurtGo.AddComponent<BoxCollider>();
        hbox.size = Vector3.one * 0.15f;
        var hurt = hurtGo.AddComponent<HurtCollider>();
        SetupHurt(hurt);

        var lrGo = new GameObject("Shoot Line");
        lrGo.transform.SetParent(bullet.transform, false);
        var lr = lrGo.AddComponent<LineRenderer>();
        lr.positionCount = 4;
        lr.useWorldSpace = true;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        if (lineMat != null) lr.material = lineMat;
        lr.startColor = new Color(1f, 0.9f, 0.5f, 0.9f);
        lr.endColor = new Color(1f, 0.6f, 0.2f, 0.3f);
        lr.enabled = true;

        var hitEffect = new GameObject("Hit Effect");
        hitEffect.transform.SetParent(bullet.transform, false);
        AddPs(hitEffect, "Particle Sparks", new Color(1f, 0.85f, 0.4f, 1f), partMat);
        AddPs(hitEffect, "Particle Smoke", new Color(0.6f, 0.6f, 0.6f, 0.4f), partMat);
        AddPs(hitEffect, "Particle Impact", new Color(1f, 0.7f, 0.3f, 0.8f), partMat);
        var hitLightGo = new GameObject("Hit Light");
        hitLightGo.transform.SetParent(hitEffect.transform, false);
        var hl = hitLightGo.AddComponent<Light>();
        hl.type = LightType.Point; hl.range = 2f; hl.intensity = 2f;
        hl.color = new Color(1f, 0.8f, 0.4f);
        hl.enabled = true;

        var logic = bullet.AddComponent<ItemGunBullet>();
        SetPriv(logic, "hasHurtCollider", true);
        SetPriv(logic, "hurtCollider", hurt);
        SetPriv(logic, "hurtColliderTimer", 0.25f);
        SetPriv(logic, "hitGameObject", body);
        SetPriv(logic, "hitGameObjectDestroyTime", 2f);

        return SavePrefab(bullet, "Bullet.prefab");
    }

    static void AddPs(GameObject parent, string name, Color color, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.startLifetime = 0.25f;
        main.startSpeed = 3f;
        main.startSize = 0.08f;
        main.duration = 0.2f;
        main.loop = false;
        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });
        var rend = go.GetComponent<ParticleSystemRenderer>();
        if (rend != null && mat != null) { rend.material = mat; rend.renderMode = ParticleSystemRenderMode.Billboard; }
        ps.Play();
    }

    static void SetupHurt(HurtCollider hurt)
    {
        SetPriv(hurt, "playerLogic", true);
        SetPriv(hurt, "playerDamage", 90);
        SetPriv(hurt, "playerKill", false);
        SetPriv(hurt, "playerHitForce", 10f);
        SetPriv(hurt, "playerDamageCooldown", 2f);
        SetPriv(hurt, "playerRayCast", true);
        SetPriv(hurt, "playerTumbleForce", 15f);
        SetPriv(hurt, "playerTumbleTorque", 20f);
        SetPriv(hurt, "playerTumbleTime", 2f);
        SetPriv(hurt, "physLogic", true);
        SetPriv(hurt, "physDestroy", false);
        SetPriv(hurt, "physImpact", 3);
        SetPriv(hurt, "physDamageCooldown", 2f);
        SetPriv(hurt, "physHitForce", 10f);
        SetPriv(hurt, "physHitTorque", 20f);
        SetPriv(hurt, "enemyLogic", true);
        SetPriv(hurt, "enemyDamage", 90);
        SetPriv(hurt, "enemyKill", false);
        SetPriv(hurt, "enemyStun", true);
        SetPriv(hurt, "enemyStunTime", 2.5f);
        SetPriv(hurt, "enemyStunType", 2);
        SetPriv(hurt, "enemyFreezeTime", 0.2f);
        SetPriv(hurt, "enemyImpact", 3);
        SetPriv(hurt, "enemyDamageCooldown", 2f);
        SetPriv(hurt, "enemyHitForce", 15f);
        SetPriv(hurt, "enemyHitTorque", 30f);
        SetPriv(hurt, "hitSpread", 10f);
        SetPriv(hurt, "timer", 0.2f);
    }

    static void EnsureAlwaysIncludedShaders(params string[] names)
    {
        try
        {
            var objs = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (objs == null || objs.Length == 0) { Debug.LogWarning("[QBZ191] 读不到 GraphicsSettings"); return; }
            var so = new SerializedObject(objs[0]);
            var arr = so.FindProperty("m_AlwaysIncludedShaders");
            if (arr == null) { Debug.LogWarning("[QBZ191] 找不到 m_AlwaysIncludedShaders"); return; }
            var have = new HashSet<string>();
            for (int i = 0; i < arr.arraySize; i++)
            {
                var sh = arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
                if (sh != null) have.Add(sh.name);
            }
            foreach (var n in names)
            {
                var sh = Shader.Find(n);
                if (sh == null) { Debug.LogWarning("[QBZ191] 找不到 shader: " + n); continue; }
                if (have.Contains(sh.name)) continue;
                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = sh;
                have.Add(sh.name);
                Debug.Log("[QBZ191] 已加入 Always Included Shader: " + n);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
        catch (Exception e) { Debug.LogWarning("[QBZ191] 处理 Always Included Shader 失败: " + e.Message); }
    }

    static void SetSound(UnityEngine.Object target, string field, AudioClip clip, float volume)
    {
        try
        {
            var so = new SerializedObject(target);
            var arr = so.FindProperty(field + ".Sounds");
            if (arr != null && arr.isArray)
            {
                arr.arraySize = 1;
                arr.GetArrayElementAtIndex(0).objectReferenceValue = clip;
            }
            var vol = so.FindProperty(field + ".Volume");
            if (vol != null) vol.floatValue = volume;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        catch { }
    }

    static void SaveAsset(UnityEngine.Object o, string file)
    {
        var path = GEN_DIR + "/" + file;
        AssetDatabase.CreateAsset(o, path);
        EditorUtility.SetDirty(o);
    }

    static GameObject SavePrefab(GameObject go, string file)
    {
        var path = GEN_DIR + "/" + file;
        var p = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return p;
    }

    static void SetPriv(UnityEngine.Object target, string field, object value)
    {
        try
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) return;
            if (value is bool b) p.boolValue = b;
            else if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is string s) p.stringValue = s;
            else if (value is UnityEngine.Object o) p.objectReferenceValue = o;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        catch { }
    }
}
