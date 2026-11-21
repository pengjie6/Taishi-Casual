#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace TTBuildDiag
{
    /// <summary>
    /// 抖音小游戏 WebGL 构建诊断工具（独立脚本，不修改任何既有代码）。
    ///
    /// 用途：TTSDK 的构建按钮在 outputDir 打印之后零错误静默退出时，
    ///      用它绕过 TTSDK 直接调用 BuildPipeline，把真实失败原因打印到 Console。
    ///
    /// 菜单：
    ///   Tools → 抖音小游戏 → 1) 环境自检（只读，不构建）
    ///   Tools → 抖音小游戏 → 2) 直接构建 WebGL（诊断用，绕过 TTSDK）
    ///   Tools → 抖音小游戏 → 3) 修复 WebGL 关键设置
    /// </summary>
    public static class TTBuildDirect
    {
        private const string WebGLOutDir = "tt-minigame/webgl";

        // ============================================================ 1. 环境自检

        [MenuItem("Tools/抖音小游戏/1) 环境自检（只读，不构建）", false, 200)]
        public static void CheckEnvironment()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("================ 抖音小游戏构建环境自检 ================");
            sb.AppendLine("Unity 版本          : " + Application.unityVersion);
            sb.AppendLine("项目路径            : " + Directory.GetParent(Application.dataPath).FullName);
            sb.AppendLine("当前 BuildTarget    : " + EditorUserBuildSettings.activeBuildTarget);
            sb.AppendLine("脚本编译失败        : " + EditorUtility.scriptCompilationFailed);
            sb.AppendLine("正在构建            : " + BuildPipeline.isBuildingPlayer);
            sb.AppendLine("正在编译/导入       : " + (EditorApplication.isCompiling || EditorApplication.isUpdating));
            sb.AppendLine("处于 Play 模式      : " + EditorApplication.isPlaying);
            sb.AppendLine();

            // ---- 图形 API ----
            sb.AppendLine("---------------- WebGL 图形 API ----------------");
            bool useDefault = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL);
            GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            sb.AppendLine("  使用默认 API : " + useDefault + (useDefault ? "   ← 必须为 False" : "   OK"));
            sb.AppendLine("  当前 API     : " + JoinApis(apis));
            sb.AppendLine();

            // ---- 所有 webGL* 字段 ----
            sb.AppendLine("---------------- ProjectSettings 中的 webGL 字段 ----------------");
            SerializedObject ps = LoadPlayerSettings();
            if (ps != null)
            {
                SerializedProperty it = ps.GetIterator();
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (it.name.IndexOf("webGL", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    string val;
                    switch (it.propertyType)
                    {
                        case SerializedPropertyType.Integer: val = it.intValue.ToString(); break;
                        case SerializedPropertyType.Boolean: val = it.boolValue.ToString(); break;
                        case SerializedPropertyType.String: val = "\"" + it.stringValue + "\""; break;
                        case SerializedPropertyType.Enum: val = it.enumValueIndex.ToString(); break;
                        default: val = "(" + it.propertyType + ")"; break;
                    }
                    sb.AppendLine("  " + it.name + " = " + val);
                }
            }
            else
            {
                sb.AppendLine("  ! 无法打开 ProjectSettings.asset");
            }
            sb.AppendLine();

            // ---- 脚本后端 ----
            sb.AppendLine("---------------- 脚本后端 ----------------");
            sb.AppendLine("  WebGL : " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL) + "   (必须是 IL2CPP)");
            sb.AppendLine();

            // ---- 场景 ----
            sb.AppendLine("---------------- 构建场景列表 ----------------");
            List<string> scenes = CollectScenes();
            sb.AppendLine("  启用场景数 : " + scenes.Count);
            for (int i = 0; i < scenes.Count; i++) sb.AppendLine("    [" + i + "] " + scenes[i]);
            if (scenes.Count == 0) sb.AppendLine("  ! 没有启用场景，BuildPlayer 会立刻失败");
            sb.AppendLine();

            // ---- 输出目录 ----
            string fullOut = Path.GetFullPath(WebGLOutDir);
            sb.AppendLine("---------------- 输出目录 ----------------");
            sb.AppendLine("  路径 : " + fullOut);
            sb.AppendLine("  长度 : " + fullOut.Length + " 字符");
            sb.AppendLine("  存在 : " + Directory.Exists(fullOut));
            if (Directory.Exists(fullOut))
            {
                string[] files = Directory.GetFiles(fullOut, "*", SearchOption.AllDirectories);
                sb.AppendLine("  文件数 : " + files.Length);
            }
            sb.AppendLine();

            sb.AppendLine("================ 自检结束 ================");
            Debug.Log(sb.ToString());
        }

        // ============================================================ 2. 直接构建

        [MenuItem("Tools/抖音小游戏/2) 直接构建 WebGL（诊断，绕过 TTSDK）", false, 201)]
        public static void BuildDirect()
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogError("[TTBuild] 已经有一个构建在进行中，请等待结束。");
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                Debug.LogError("[TTBuild] 编辑器正在编译或导入资源，请等 Console 右侧转圈结束再点。");
                return;
            }
            if (EditorUtility.scriptCompilationFailed)
            {
                Debug.LogError("[TTBuild] 存在脚本编译错误，Unity 拒绝构建。请先清空 Console 里的红色报错。");
                return;
            }

            StringBuilder log = new StringBuilder();
            log.AppendLine("================ [TTBuild] 直接构建 WebGL ================");

            List<string> scenes = CollectScenes();
            log.AppendLine("启用场景数 : " + scenes.Count);
            if (scenes.Count == 0)
            {
                Debug.LogError("[TTBuild] Build Settings 里没有启用任何场景，无法构建。\n"
                    + "请打开 File → Build Settings，把游戏场景拖进去并勾选。");
                return;
            }
            for (int i = 0; i < scenes.Count; i++) log.AppendLine("   [" + i + "] " + scenes[i]);

            // ---- 准备输出目录（这一步能暴露"目录被占用"）----
            string fullOut = Path.GetFullPath(WebGLOutDir);
            log.AppendLine("输出目录 : " + fullOut);
            try
            {
                if (Directory.Exists(fullOut))
                {
                    Directory.Delete(fullOut, true);
                    log.AppendLine("  · 已删除旧输出目录");
                }
                Directory.CreateDirectory(fullOut);
                log.AppendLine("  · 已重建输出目录（可写）");
            }
            catch (Exception e)
            {
                log.AppendLine("  ! 清理输出目录失败：" + e.GetType().Name + " - " + e.Message);
                log.AppendLine("    常见原因：抖音开发者工具 / 资源管理器 / 杀毒软件正占用该目录。");
                Debug.LogError(log.ToString());
                return;
            }

            // ---- 强制图形 API 为 OpenGLES3 ----
            try
            {
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
                log.AppendLine("图形 API : " + JoinApis(PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL)));
            }
            catch (Exception e)
            {
                log.AppendLine("! 设置图形 API 失败：" + e.Message);
            }

            // ---- 恢复 Debug Symbols ----
            // TTSDK 在转换阶段需要 webgl.symbols.json 做 wasm 代码分包，
            // 关掉会打印 "wasm symbols file missing" 并可能中断转换。
            // 上一次能够产出完整包体（9:57）时该值就是开启状态。
            SerializedObject psSym = LoadPlayerSettings();
            if (psSym != null)
            {
                int oldSym = GetInt(psSym, "webGLDebugSymbols");
                SetInt(psSym, "webGLDebugSymbols", 1);
                psSym.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                log.AppendLine("Debug Symbols : " + oldSym + " → 1（TTSDK wasm 分包需要）");
            }

            log.AppendLine("脚本后端 : " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.WebGL));
            log.AppendLine("开始时间 : " + DateTime.Now.ToString("HH:mm:ss"));
            log.AppendLine("=========================================================");
            Debug.Log(log.ToString());

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = fullOut,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = null;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception e)
            {
                Debug.LogError("[TTBuild] BuildPipeline.BuildPlayer 抛出异常：\n"
                    + e.GetType().FullName + "\n" + e.Message + "\n\n" + e.StackTrace);
                return;
            }

            if (report == null)
            {
                Debug.LogError("[TTBuild] BuildPlayer 返回 null，说明构建在启动阶段就被拒绝了（通常是编译错误或模块缺失）。");
                return;
            }

            BuildSummary sum = report.summary;
            Debug.Log("[TTBuild] 构建结果 : " + sum.result
                + " | 错误 " + sum.totalErrors
                + " | 警告 " + sum.totalWarnings
                + " | 大小 " + (sum.totalSize / 1024f / 1024f).ToString("F1") + " MB"
                + " | 耗时 " + sum.totalTime.ToString(@"mm\:ss"));

            if (sum.result != BuildResult.Succeeded || sum.totalErrors > 0)
            {
                StringBuilder err = new StringBuilder();
                err.AppendLine("================ [TTBuild] 构建失败明细 ================");
                err.AppendLine("输出路径 : " + sum.outputPath);
                foreach (BuildStep step in report.steps)
                {
                    foreach (BuildStepMessage m in step.messages)
                    {
                        if (m.type == LogType.Error || m.type == LogType.Exception || m.type == LogType.Assert)
                            err.AppendLine("[" + step.name + "] " + m.content);
                    }
                }
                err.AppendLine("================ 明细结束 ================");
                Debug.LogError(err.ToString());
            }
            else
            {
                Debug.Log("[TTBuild] ✓ 构建成功，产物目录：" + fullOut
                    + "\n   现在回到 TTSDK 窗口点构建，它会复用这批产物，不再自己跑 IL2CPP。");
            }
        }

        // ============================================================ 3. 修复关键设置

        [MenuItem("Tools/抖音小游戏/3) 修复 WebGL 关键设置", false, 202)]
        public static void FixSettings()
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine("[TTBuild] 修复 WebGL 关键设置：");

            // 图形 API
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            log.AppendLine("  · 图形 API → " + JoinApis(PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL)));

            SerializedObject ps = LoadPlayerSettings();
            if (ps != null)
            {
                log.AppendLine("  · webGLMemorySize 旧值 = " + GetInt(ps, "webGLMemorySize"));
                SetInt(ps, "webGLMemorySize", 256);
                SetInt(ps, "webGLInitialMemorySize", 32);
                SetInt(ps, "webGLDebugSymbols", 1);
                SetString(ps, "webGLEmscriptenArgs", string.Empty);
                ps.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                log.AppendLine("  · webGLMemorySize → 256 / InitialMemory → 32 / DebugSymbols → 1 / EmscriptenArgs → 空");
            }
            else
            {
                log.AppendLine("  ! 打不开 ProjectSettings.asset");
            }

            // TTSDK 自己的设置：WebGL 2.0 开关
            string ttSettingPath = "Assets/Editor/StarkBuilderSetting.asset";
            if (File.Exists(ttSettingPath))
            {
                string text = File.ReadAllText(ttSettingPath);
                if (text.Contains("isWebGL2: 0"))
                {
                    text = text.Replace("isWebGL2: 0", "isWebGL2: 1");
                    File.WriteAllText(ttSettingPath, text);
                    AssetDatabase.ImportAsset(ttSettingPath);
                    log.AppendLine("  · TTSDK StarkBuilderSetting.isWebGL2 → 1（开启 WebGL 2.0）");
                }
                else if (text.Contains("isWebGL2: 1"))
                {
                    log.AppendLine("  · TTSDK isWebGL2 已经是 1，无需改动");
                }
            }

            Debug.Log(log.ToString());
        }

        // ============================================================ 工具

        private static List<string> CollectScenes()
        {
            List<string> scenes = new List<string>();
            EditorBuildSettingsScene[] all = EditorBuildSettings.scenes;
            if (all != null)
            {
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].enabled && !string.IsNullOrEmpty(all[i].path))
                        scenes.Add(all[i].path);
                }
            }
            return scenes;
        }

        private static string JoinApis(GraphicsDeviceType[] apis)
        {
            if (apis == null || apis.Length == 0) return "(空)";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < apis.Length; i++)
            {
                if (i > 0) sb.Append(" + ");
                sb.Append(apis[i]);
            }
            return sb.ToString();
        }

        private static SerializedObject LoadPlayerSettings()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0 || assets[0] == null) return null;
            return new SerializedObject(assets[0]);
        }

        private static int GetInt(SerializedObject so, string name)
        {
            SerializedProperty p = so.FindProperty(name);
            return p != null ? p.intValue : -1;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null) p.intValue = value;
        }

        private static void SetString(SerializedObject so, string name, string value)
        {
            SerializedProperty p = so.FindProperty(name);
            if (p != null) p.stringValue = value;
        }
    }
}
#endif
