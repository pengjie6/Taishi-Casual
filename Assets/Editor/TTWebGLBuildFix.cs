#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace TTBuildFix
{
    /// <summary>
    /// 抖音小游戏（WebGL）构建配置修复工具。
    ///
    /// 解决的问题：
    ///   1. 图形 API 被写死为 WebGL 1.0（OpenGLES2）。URP 14 要求 WebGL 2.0（OpenGLES3），
    ///      在 WebGL 1.0 下 URP 的着色器与 RenderTexture 格式不受支持，
    ///      表现就是加载界面走完 "Done" 之后相机不渲染 → 蓝屏 / 黑屏。
    ///   2. 堆内存 128MB 偏小，且 Emscripten 参数里写死 -s TOTAL_MEMORY=128MB 禁止增长。
    ///   3. Debug Symbols 开启，每次构建产出约 11MB 的 webgl.symbols.json，
    ///      内含全部函数名与代码结构，是逆向的第一手资料。
    ///   4. Unity Analytics / Unity Ads 服务开着，会持续请求
    ///      config.uca.cloud.unity3d.com、cdp.cloud.unity3d.com，
    ///      这些域名不在抖音小游戏白名单内，运行时会不停刷错误。
    ///
    /// 使用方法：
    ///   菜单 Tools → 抖音小游戏 · 修复 WebGL 构建配置
    ///
    /// 注意：本工具已改为「纯手动执行」。
    ///   早期版本用 [InitializeOnLoad] 在每次脚本编译后自动执行，
    ///   会在构建过程中（SetScriptingDefineSymbols 触发域重载后）再次访问 PlayerSettings，
    ///   导致 BuildPlayer 被静默打断，表现为点构建后零报错秒退。
    /// </summary>
    public static class TTWebGLBuildFix
    {
        private const string MenuPath = "Tools/抖音小游戏 · 修复 WebGL 构建配置";
        private const string SettingsAssetPath = "ProjectSettings/ProjectSettings.asset";
        private const string ConnectAssetPath = "ProjectSettings/UnityConnectSettings.asset";

        private const int TargetMemoryMB = 256;

        [MenuItem(MenuPath)]
        private static void ApplyFromMenu()
        {
            Apply(false);
        }

        // ---------------------------------------------------------------- 检测

        private static bool NeedsFix()
        {
            // 1. 图形 API 必须是「非自动 + 仅 OpenGLES3」
            bool apiOk = false;
            if (!PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL))
            {
                GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
                apiOk = apis != null && apis.Length == 1 && apis[0] == GraphicsDeviceType.OpenGLES3;
            }

            if (!apiOk) return true;

            // 2. 内存 / 调试符号
            SerializedObject so = LoadPlayerSettings();
            if (so == null) return true;

            if (ReadInt(so, "webGLMemorySize", -1) != TargetMemoryMB) return true;
            if (ReadInt(so, "webGLDebugSymbols", -1) != 0) return true;

            return false;
        }

        // ---------------------------------------------------------------- 应用

        private static void Apply(bool automatic)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine(automatic
                ? "[TTBuildFix] 检测到 WebGL 配置不正确，自动修正："
                : "[TTBuildFix] 开始修正 WebGL 构建配置：");

            // ---- 1. 图形 API：WebGL 2.0 ----
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            log.AppendLine("  · 图形 API → WebGL 2.0 (OpenGLES3)");

            // ---- 2. 内存 / Emscripten 参数 / 调试符号 ----
            // 直接写 ProjectSettings.asset 里的同名字段，避免不同 Unity 版本
            // PlayerSettings.WebGL.* 属性名差异（如 debugSymbols / debugSymbolMode）
            SerializedObject ps = LoadPlayerSettings();
            if (ps != null)
            {
                int oldMem = WriteInt(ps, "webGLMemorySize", TargetMemoryMB);
                int oldSym = WriteInt(ps, "webGLDebugSymbols", 0);
                string oldArgs = WriteString(ps, "webGLEmscriptenArgs", string.Empty);
                ps.ApplyModifiedPropertiesWithoutUndo();

                log.AppendLine("  · 堆内存 → " + TargetMemoryMB + "MB（原 " + oldMem + "MB）");
                log.AppendLine("  · Emscripten 参数 → 空（原 \"" + (oldArgs ?? "") + "\"）");
                log.AppendLine("  · Debug Symbols → 关闭（原 " + oldSym + "）");
            }
            else
            {
                log.AppendLine("  ! 未能打开 ProjectSettings.asset，内存与符号项跳过，请在 Player Settings 里手动确认");
            }

            // ---- 3. 关闭 Unity Analytics / Unity Ads 服务 ----
            SerializedObject uc = LoadConnectSettings();
            if (uc != null)
            {
                WriteBool(uc, "UnityAnalyticsSettings.m_Enabled", false);
                WriteBool(uc, "UnityAnalyticsSettings.m_InitializeOnStartup", false);
                WriteBool(uc, "UnityAdsSettings.m_Enabled", false);
                WriteBool(uc, "UnityAdsSettings.m_InitializeOnStartup", false);
                uc.ApplyModifiedPropertiesWithoutUndo();
                log.AppendLine("  · Unity Analytics / Unity Ads 服务 → 关闭");
            }

            AssetDatabase.SaveAssets();

            // ---- 4. 回读确认 ----
            GraphicsDeviceType[] result = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            log.AppendLine("  · 校验：当前 WebGL 图形 API = " + JoinApis(result));

            SerializedObject check = LoadPlayerSettings();
            if (check != null)
            {
                log.AppendLine("  · 校验：内存 = " + ReadInt(check, "webGLMemorySize", -1) + "MB"
                    + "，DebugSymbols = " + ReadInt(check, "webGLDebugSymbols", -1)
                    + "，Args = \"" + ReadString(check, "webGLEmscriptenArgs", "") + "\"");
            }

            log.AppendLine("[TTBuildFix] 完成。请重新构建抖音小游戏，构建后日志中应出现 "
                + "\"Creating OpenGL ES 3\" 而不是 \"OpenGL ES 2.0\"。");

            Debug.Log(log.ToString());
        }

        // ---------------------------------------------------------------- 工具

        private static string JoinApis(GraphicsDeviceType[] apis)
        {
            if (apis == null || apis.Length == 0) return "(空)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < apis.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(apis[i]);
            }
            return sb.ToString();
        }

        private static SerializedObject LoadPlayerSettings()
        {
            return LoadSettingsAsset(SettingsAssetPath);
        }

        private static SerializedObject LoadConnectSettings()
        {
            return LoadSettingsAsset(ConnectAssetPath);
        }

        private static SerializedObject LoadSettingsAsset(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                Debug.LogWarning("[TTBuildFix] 打不开 " + path);
                return null;
            }
            return new SerializedObject(assets[0]);
        }

        private static int ReadInt(SerializedObject so, string name, int fallback)
        {
            SerializedProperty p = so.FindProperty(name);
            return p != null ? p.intValue : fallback;
        }

        private static string ReadString(SerializedObject so, string name, string fallback)
        {
            SerializedProperty p = so.FindProperty(name);
            return p != null ? p.stringValue : fallback;
        }

        private static int WriteInt(SerializedObject so, string name, int value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
            {
                Debug.LogWarning("[TTBuildFix] 未找到字段 " + name);
                return -1;
            }
            int old = p.intValue;
            p.intValue = value;
            return old;
        }

        private static string WriteString(SerializedObject so, string name, string value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
            {
                Debug.LogWarning("[TTBuildFix] 未找到字段 " + name);
                return null;
            }
            string old = p.stringValue;
            p.stringValue = value;
            return old;
        }

        private static void WriteBool(SerializedObject so, string name, bool value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p == null)
            {
                Debug.LogWarning("[TTBuildFix] 未找到字段 " + name);
                return;
            }
            p.boolValue = value;
        }
    }
}
#endif
