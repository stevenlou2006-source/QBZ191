// QBZ191 → REPO 枪械物品（手搭 prefab，不引用任何原版 prefab，避免 shader 编译崩溃）
// 菜单：REPO/生成 QBZ191 枪械包
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

    const float TARGET_LENGTH = 1.00f;
    const float VERTICAL_NUDGE = 0.06f;
    static readonly Vector3 VISUAL_OFFSET = new Vector3(0f, 0f, -0.05f);
    static readonly Vector3 VISUAL_EULER = new Vector3(0f, 0f, 0f);
    const float RECOIL = 2.5f;
    const float COOLDOWN = 0.12f;
    const float BATTERY_DRAIN = 3.3333f;
    const float MUZZLE_Z = 0.52f;

    static Material _partMat;

    [MenuItem("REPO/生成 QBZ191 枪械包")]
    public static void BuildAll()
    {
        try
        {
            if (!Directory.Exists(GEN_DIR)) Directory.CreateDirectory(GEN_DIR);

            var mat = new Material(Shader.Find("Standard"));
            mat.name = "QBZ191 Material";
            mat.color = new Color(0.22f, 0.22f, 0.24f);
            mat.SetFloat("_Metallic", 0.65f);
            mat.SetFloat("_Glossiness", 0.55f);
            SaveAsset(mat, "QBZ191 Material.mat");

            _partMat = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            _partMat.name = "QBZ191 Particle Mat";
            SaveAsset(_partMat, "QBZ191 Particle Mat.mat");

            // ---------- 子弹 ----------
            var bullet = new GameObject("Bullet");
            var bodyGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyGo.name = "bullet";
            bodyGo.transform.SetParent(bullet.transform, false);
            bodyGo.transform.localScale = Vector3.one * 0.04f;
            var bodyCol = bodyGo.GetComponent<SphereCollider>();
            if (bodyCol != null) UnityEngine.Object.DestroyImmediate(bodyCol);

            var hurtGo = new GameObject("Hurt Collider");
            hurtGo.transform.SetParent(bullet.transform, false);
            var hbox = hurtGo.AddComponent<BoxCollider>();
            hbox.size = Vector3.one * 0.15f;
            var hurt = hurtGo.AddComponent<HurtCollider>();
            SetupHurtCollider(hurt);

            var lrGo = new GameObject("Shoot Line");
            lrGo.transform.SetParent(bullet.transform, false);
            var lr = lrGo.AddComponent<LineRenderer>();
            lr.positionCount = 4;
            lr.useWorldSpace = true;
            lr.startWidth = 0.03f;
            lr.endWidth = 0.03f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.9f, 0.5f, 0.9f);
            lr.endColor = new Color(1f, 0.6f, 0.2f, 0.3f);
            lr.enabled = true;

            // Hit Effect 是父节点（游戏在它下面 Find 三个粒子和 Hit Light）
            var hitEffectGo = new GameObject("Hit Effect");
            hitEffectGo.transform.SetParent(bullet.transform, false);
            AddParticles(hitEffectGo, "Particle Sparks", new Color(1f, 0.85f, 0.4f, 1f));
            AddParticles(hitEffectGo, "Particle Smoke", new Color(0.6f, 0.6f, 0.6f, 0.4f));
            AddParticles(hitEffectGo, "Particle Impact", new Color(1f, 0.7f, 0.3f, 0.8f));
            var hitLightGo = new GameObject("Hit Light");
            hitLightGo.transform.SetParent(hitEffectGo.transform, false);
            var hl = hitLightGo.AddComponent<Light>();
            hl.type = LightType.Point;
            hl.range = 2f;
            hl.intensity = 2f;
            hl.color = new Color(1f, 0.8f, 0.4f);
            hl.enabled = true;

            var bulletLogic = bullet.AddComponent<ItemGunBullet>();
            SetPriv(bulletLogic, "hasHurtCollider", true);
            SetPriv(bulletLogic, "hurtCollider", hurt);
            SetPriv(bulletLogic, "hurtColliderTimer", 0.25f);
            SetPriv(bulletLogic, "hitGameObject", bodyGo);
            SetPriv(bulletLogic, "hitGameObjectDestroyTime", 2f);
            var bulletPrefab = SavePrefab(bullet, "Bullet.prefab");

            // ---------- 枪口火焰 ----------
            var flash = new GameObject("MuzzleFlash");
            flash.AddComponent<ItemGunMuzzleFlash>();
            AddParticles(flash, "Particle Smoke", new Color(0.6f, 0.6f, 0.6f, 0.5f));
            AddParticles(flash, "Particle Impact", new Color(1f, 0.7f, 0.3f, 0.8f));
            AddParticles(flash, "Particle Sparks", new Color(1f, 0.85f, 0.4f, 1f));
            var shootLightGo = new GameObject("Shoot Light");
            shootLightGo.transform.SetParent(flash.transform, false);
            var sl = shootLightGo.AddComponent<Light>();
            sl.type = LightType.Point;
            sl.range = 5f;
            sl.intensity = 3f;
            sl.color = new Color(1f, 0.85f, 0.5f);
            sl.enabled = true;
            var flashPrefab = SavePrefab(flash, "MuzzleFlash.prefab");

            // ---------- 枪本体 ----------
            var root = new GameObject("Item Gun QBZ191");
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
                if (longest > 0.001f)
                {
                    float k = TARGET_LENGTH / longest;
                    visual.transform.localScale = visual.transform.localScale * k;
                    Debug.Log("[QBZ191] 模型最长边 " + longest.ToString("F3") + "m，缩放 " + k.ToString("F4"));
                }
                var b2 = renderers[0].bounds;
                foreach (var r in renderers) b2.Encapsulate(r.bounds);
                visual.transform.localPosition = visual.transform.localPosition - (b2.center - root.transform.position);
                visual.transform.localPosition += Vector3.up * VERTICAL_NUDGE + VISUAL_OFFSET;
                visual.transform.localRotation = Quaternion.Euler(VISUAL_EULER);
            }

            var muzzle = new GameObject("Gun Muzzle").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, 0f, MUZZLE_Z);

            AddBox(root, "Grab Box", new Vector3(0f, 0f, 0.05f), new Vector3(0.12f, 0.16f, 0.9f));
            AddBox(root, "Grab Box Grip", new Vector3(0f, -0.1f, -0.12f), new Vector3(0.09f, 0.22f, 0.16f));
            AddBox(root, "Grab Box Mag", new Vector3(0f, -0.06f, -0.32f), new Vector3(0.1f, 0.18f, 0.2f));

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.8f;
            rb.drag = 0.2f;
            rb.angularDrag = 0.5f;

            var grab = root.AddComponent<PhysGrabObject>();
            SetPriv(grab, "rb", rb);
            SetPriv(grab, "overrideTagsAndLayers", true);
            var impact = root.AddComponent<PhysGrabObjectImpactDetector>();
            SetPriv(impact, "fragility", 50f);
            SetPriv(impact, "durability", 100f);

            var pv = root.AddComponent<PhotonView>();
            var ptv = root.AddComponent<PhotonTransformView>();
            pv.ObservedComponents = new List<Component> { ptv };
            pv.Synchronization = ViewSynchronization.UnreliableOnChange;

            var attr = root.AddComponent<ItemAttributes>();
            root.AddComponent<ItemEquippable>();
            root.AddComponent<ItemToggle>();
            root.AddComponent<MapCustom>();
            root.AddComponent<NotValuableObject>();
            root.AddComponent<RoomVolumeCheck>();

            var bat = root.AddComponent<ItemBattery>();
            SetPriv(bat, "batteryBars", 30);
            SetPriv(bat, "batteryLife", 100f);
            SetPriv(bat, "batteryTransform", muzzle);
            SetPriv(bat, "upOffset", 0.5f);
            SetPriv(bat, "batteryDrainRate", 1f);
            SetPriv(bat, "autoDrain", false);
            SetPriv(bat, "hasBatteryVisuals", false);

            var gun = root.AddComponent<ItemGun>();
            SetPriv(gun, "hasOneShot", true);
            SetPriv(gun, "shootTime", 1f);
            SetPriv(gun, "hasBuildUp", false);
            SetPriv(gun, "buildUpTime", 1f);
            SetPriv(gun, "numberOfBullets", 1);
            SetPriv(gun, "gunRandomSpread", 0f);
            SetPriv(gun, "gunRange", 100f);
            SetPriv(gun, "distanceKeep", 1.1f);
            SetPriv(gun, "gunRecoilForce", RECOIL);
            SetPriv(gun, "cameraShakeMultiplier", 0.5f);
            SetPriv(gun, "torqueMultiplier", 1f);
            SetPriv(gun, "grabStrengthMultiplier", 1f);
            SetPriv(gun, "shootCooldown", COOLDOWN);
            SetPriv(gun, "batteryDrain", BATTERY_DRAIN);
            SetPriv(gun, "batteryDrainFullBar", true);
            SetPriv(gun, "batteryDrainFullBars", 1);
            SetPriv(gun, "misfirePercentageChange", 0f);
            SetPriv(gun, "grabVerticalOffset", -0.2f);
            SetPriv(gun, "aimVerticalOffset", -8f);
            SetPriv(gun, "investigateRadius", 20f);
            SetPriv(gun, "gunMuzzle", muzzle);
            SetPriv(gun, "bulletPrefab", bulletPrefab);
            SetPriv(gun, "muzzleFlashPrefab", flashPrefab);
            SetPriv(gun, "gunTrigger", root.transform);
            gun.shootLineWidthCurve = new AnimationCurve(new Keyframe(0f, 0.03f), new Keyframe(1f, 0.03f));

            var shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GEN_DIR + "/QBZ191 Shoot Synth.wav");
            var emptyClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GEN_DIR + "/QBZ191 Empty.wav");
            if (shootClip != null)
            {
                SetSound(gun, "soundShoot", shootClip, 0.75f);
                SetSound(gun, "soundShootGlobal", shootClip, 0.55f);
                SetSound(gun, "soundHit", emptyClip, 0.5f);
            }
            if (emptyClip != null) SetSound(gun, "soundNoAmmoClick", emptyClip, 0.6f);

            var flashInst = (GameObject)PrefabUtility.InstantiatePrefab(flashPrefab);
            flashInst.name = "Muzzle Flash";
            flashInst.transform.SetParent(root.transform, false);
            flashInst.transform.localPosition = muzzle.localPosition;

            var lightGo = new GameObject("Item Light");
            lightGo.transform.SetParent(root.transform, false);
            var lg = lightGo.AddComponent<Light>();
            lg.type = LightType.Point; lg.range = 2f; lg.intensity = 0.4f;
            var il = lightGo.AddComponent<ItemLight>();
            SetPriv(il, "itemLight", lg);

            var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            rt.name = "QBZ191 Icon RT";
            SaveAsset(rt, "QBZ191 Icon RT.renderTexture");
            var iconGo = new GameObject("SemiIcon Maker");
            iconGo.transform.SetParent(root.transform, false);
            var cam = iconGo.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 10f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            cam.enabled = false;
            cam.targetTexture = rt;
            iconGo.transform.localPosition = new Vector3(0.55f, 0.45f, 0.75f);
            iconGo.transform.LookAt(root.transform.position + Vector3.up * 0.02f);
            var iconMaker = iconGo.AddComponent<SemiIconMaker>();
            SetPriv(iconMaker, "iconCamera", cam);
            SetPriv(iconMaker, "renderTexture", rt);

            var gunPrefab = SavePrefab(root, "Item Gun QBZ191.prefab");

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

            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(GEN_DIR + "/Item Gun QBZ191.prefab");
            var pfAttr = pf.GetComponent<ItemAttributes>();
            pfAttr.item = item;
            SetPriv(pfAttr, "item", item);
            SetPriv(pfAttr, "colorPreset", colors);
            EditorUtility.SetDirty(pfAttr);

            var pfNotVal = pf.GetComponent<NotValuableObject>();
            if (pfNotVal != null) { pfNotVal.physAttributePreset = phys; EditorUtility.SetDirty(pfNotVal); }

            var pfBat = pf.GetComponent<ItemBattery>();
            if (pfBat != null) { SetPriv(pfBat, "itemAttributes", pfAttr); EditorUtility.SetDirty(pfBat); }

            var mod = ScriptableObject.CreateInstance<REPOLib.Objects.Sdk.Mod>();
            mod.name = "Mod QBZ191";
            SetPriv(mod, "_name", "QBZ191");
            SetPriv(mod, "_author", "cn_xc");
            SetPriv(mod, "_version", "1.0.0");
            SetPriv(mod, "_description", "QBZ-191 rifle");
            SaveAsset(mod, "Mod QBZ191.asset");

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

    static void SetupHurtCollider(HurtCollider hurt)
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

    static void AddParticles(GameObject parent, string name, Color color)
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
        if (rend != null && _partMat != null)
        {
            rend.material = _partMat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
        }
        ps.Play();
    }

    static void AddBox(GameObject root, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.localPosition = center;
        var b = go.AddComponent<BoxCollider>();
        b.size = size;
        go.AddComponent<PhysGrabObjectCollider>();
        go.AddComponent<PhysGrabObjectBoxCollider>();
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

    static void BuildBundle()
    {
        try
        {
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
