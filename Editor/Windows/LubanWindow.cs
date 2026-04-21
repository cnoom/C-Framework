using System;
using System.Collections.Generic;
using System.IO;
using CFramework.Editor.Configs;
using CFramework.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace CFramework.Editor.Windows
{
    /// <summary>
    ///     Luban 可视化生成器窗口
    ///     <para>包含生成器和设置面板，配置基于 EditorPrefs 本地存储</para>
    /// </summary>
    public class LubanWindow : EditorWindow
    {
        private enum TabType
        {
            Generator,
            Settings
        }

        private TabType _currentTab = TabType.Generator;
        private List<string> _logs;
        private Vector2 _logScrollPos;
        private Vector2 _settingsScrollPos;
        private string _statusText = "";
        private Color _statusColor = Color.white;
        private string _envCheckText = "正在检查环境...";
        private bool _envOk;
        private bool _isGenerating;

        // 样式缓存
        private GUIStyle _logLabelStyle;
        private GUIStyle _logErrorStyle;
        private GUIStyle _logInfoStyle;
        private GUIStyle _sectionBoxStyle;
        private GUIStyle _headerTitleStyle;
        private GUIStyle _summaryLabelStyle;
        private GUIStyle _summaryValueStyle;
        private bool _stylesInitialized;

        /// <summary>
        ///     打开 Luban 生成器窗口（供外部调用）
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<LubanWindow>("Luban 生成器");
            window.minSize = new Vector2(480, 520);
        }

        private void OnEnable()
        {
            _logs = new List<string>();
            RefreshEnvCheck();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _sectionBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8)
            };

            _headerTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };

            _summaryLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                fixedWidth = 90
            };

            _summaryValueStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true
            };

            _logLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                wordWrap = true,
                richText = true
            };

            _logErrorStyle = new GUIStyle(_logLabelStyle)
            {
                normal = { textColor = new Color(1f, 0.4f, 0.3f) }
            };

            _logInfoStyle = new GUIStyle(_logLabelStyle)
            {
                normal = { textColor = new Color(0.4f, 0.8f, 1f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

            // === 标题 ===
            DrawHeader();

            // === Tab 切换 ===
            DrawTabs();

            EditorGUILayout.Space(4);

            // === 内容区域 ===
            switch (_currentTab)
            {
                case TabType.Generator:
                    DrawGeneratorTab();
                    break;
                case TabType.Settings:
                    DrawSettingsTab();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        #region 公共区域

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Luban 配置生成器", _headerTitleStyle);
            if (!string.IsNullOrEmpty(_statusText))
            {
                var style = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = _statusColor }
                };
                GUILayout.Label(_statusText, style, GUILayout.ExpandWidth(true));
            }
            else
            {
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            var genStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fontStyle = _currentTab == TabType.Generator ? FontStyle.Bold : FontStyle.Normal
            };
            var settStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 12,
                fontStyle = _currentTab == TabType.Settings ? FontStyle.Bold : FontStyle.Normal
            };

            if (GUILayout.Toggle(_currentTab == TabType.Generator, "生成器", genStyle,
                    GUILayout.Height(28)))
            {
                _currentTab = TabType.Generator;
            }

            if (GUILayout.Toggle(_currentTab == TabType.Settings, "设置", settStyle,
                    GUILayout.Height(28)))
            {
                _currentTab = TabType.Settings;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region 生成器 Tab

        private void DrawGeneratorTab()
        {
            // === 环境检查 ===
            DrawEnvCheck();

            // === 配置摘要 ===
            DrawConfigSummary();

            // === 操作按钮 ===
            DrawActionButtons();

            // === 日志面板 ===
            DrawLogPanel();
        }

        private void DrawEnvCheck()
        {
            EditorGUILayout.BeginVertical(_sectionBoxStyle);
            var icon = _envOk ? "✔" : "✘";
            var style = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = _envOk ? new Color(0.3f, 0.8f, 0.3f) : new Color(1f, 0.4f, 0.3f) }
            };
            GUILayout.Label($"{icon} {_envCheckText}", style);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
        }

        private void DrawConfigSummary()
        {
            EditorGUILayout.BeginVertical(_sectionBoxStyle);
            GUILayout.Label("配置摘要", EditorStyles.boldLabel);

            DrawSummaryRow("工具路径", LubanConfig.LubanDllPath);
            DrawSummaryRow("配置文件", LubanConfig.ConfPath);
            DrawSummaryRow("生成目标",
                $"{LubanConfig.TargetName} | 代码: {LubanConfig.CodeTarget} | 数据: {LubanConfig.DataTarget}");
            DrawSummaryRow("代码输出", LubanConfig.OutputCodeDir);
            DrawSummaryRow("数据输出", LubanConfig.OutputDataDir);
            DrawSummaryRow("顶层模块", LubanConfig.TopModule);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);
        }

        private void DrawSummaryRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, _summaryLabelStyle);
            GUILayout.Label(value ?? "-", _summaryValueStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();

            var oldColor = GUI.backgroundColor;
            if (_isGenerating)
            {
                GUI.backgroundColor = Color.gray;
            }
            else if (_envOk)
            {
                GUI.backgroundColor = new Color(0.3f, 0.75f, 0.4f);
            }

            EditorGUI.BeginDisabledGroup(_isGenerating || !_envOk);
            if (GUILayout.Button(_isGenerating ? "生成中..." : "生成代码和数据",
                    GUILayout.Height(32)))
            {
                OnGenerate();
            }
            EditorGUI.EndDisabledGroup();

            GUI.backgroundColor = oldColor;

            if (GUILayout.Button("清空日志", GUILayout.Width(80), GUILayout.Height(32)))
            {
                _logs.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
        }

        private void DrawLogPanel()
        {
            GUILayout.Label("生成日志", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.textArea, GUILayout.ExpandHeight(true));
            _logScrollPos = EditorGUILayout.BeginScrollView(_logScrollPos, GUILayout.ExpandHeight(true));

            if (_logs.Count == 0)
            {
                GUILayout.Label("暂无日志", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var log in _logs)
                {
                    if (log.StartsWith("[ERROR]") || log.StartsWith("[异常]"))
                    {
                        GUILayout.Label(log, _logErrorStyle);
                    }
                    else if (log.StartsWith("[Luban]") || log.StartsWith("[信息]"))
                    {
                        GUILayout.Label(log, _logInfoStyle);
                    }
                    else
                    {
                        GUILayout.Label(log, _logLabelStyle);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    _logScrollPos.y = float.MaxValue;
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region 设置 Tab

        private void DrawSettingsTab()
        {
            _settingsScrollPos = EditorGUILayout.BeginScrollView(_settingsScrollPos);

            // === Luban 工具 ===
            EditorGUILayout.LabelField("Luban 工具", EditorStyles.boldLabel);
            DrawPathField("工具路径", "Luban 可执行文件（.exe 或 .dll，支持绝对路径或项目相对路径）",
                LubanConfig.LubanDllPath, path => LubanConfig.LubanDllPath = path, "exe,dll");
            EditorGUILayout.Space(6);

            // === 配置文件 ===
            EditorGUILayout.LabelField("配置文件", EditorStyles.boldLabel);
            DrawPathField("luban.conf", "Luban 配置文件",
                LubanConfig.ConfPath, path => LubanConfig.ConfPath = path, "conf");
            EditorGUILayout.Space(6);

            // === 生成目标 ===
            EditorGUILayout.LabelField("生成目标", EditorStyles.boldLabel);
            LubanConfig.TargetName = EditorGUILayout.TextField("目标名称", LubanConfig.TargetName);
            LubanConfig.CodeTarget = EditorGUILayout.TextField("代码目标", LubanConfig.CodeTarget);
            LubanConfig.DataTarget = EditorGUILayout.TextField("数据目标", LubanConfig.DataTarget);
            EditorGUILayout.Space(6);

            // === 输出路径 ===
            EditorGUILayout.LabelField("输出路径", EditorStyles.boldLabel);
            DrawFolderField("代码输出目录", "代码输出",
                LubanConfig.OutputCodeDir, path => LubanConfig.OutputCodeDir = path);
            DrawFolderField("数据输出目录", "数据输出",
                LubanConfig.OutputDataDir, path => LubanConfig.OutputDataDir = path);
            EditorGUILayout.Space(6);

            // === 命名空间 ===
            EditorGUILayout.LabelField("命名空间", EditorStyles.boldLabel);
            LubanConfig.TopModule = EditorGUILayout.TextField("顶层模块", LubanConfig.TopModule);
            EditorGUILayout.Space(6);

            // === 高级选项 ===
            EditorGUILayout.LabelField("高级选项", EditorStyles.boldLabel);
            LubanConfig.CleanOutputDir = EditorGUILayout.Toggle("生成前清理输出目录", LubanConfig.CleanOutputDir);
            LubanConfig.IncludeTag = EditorGUILayout.TextField("包含标签", LubanConfig.IncludeTag);
            LubanConfig.ExcludeTag = EditorGUILayout.TextField("排除标签", LubanConfig.ExcludeTag);
            LubanConfig.ValidationFailAsError =
                EditorGUILayout.Toggle("校验失败视为错误", LubanConfig.ValidationFailAsError);
            LubanConfig.Verbose = EditorGUILayout.Toggle("详细日志", LubanConfig.Verbose);
            LubanConfig.WatchDir = EditorGUILayout.TextField("监视目录", LubanConfig.WatchDir);
            EditorGUILayout.Space(8);

            // === 操作按钮 ===
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("检查环境", GUILayout.Height(28)))
            {
                RefreshEnvCheck();
                EditorUtility.DisplayDialog("环境检查",
                    _envOk ? $"环境就绪: {_envCheckText}" : $"环境异常: {_envCheckText}", "确定");
            }

            if (GUILayout.Button("打开 luban.conf 位置", GUILayout.Height(28)))
            {
                OpenFileInExplorer(LubanConfig.ConfPath);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("打开代码输出目录", GUILayout.Height(28)))
            {
                OpenDirectoryInExplorer(LubanConfig.OutputCodeDir);
            }

            if (GUILayout.Button("打开数据输出目录", GUILayout.Height(28)))
            {
                OpenDirectoryInExplorer(LubanConfig.OutputDataDir);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // 重置按钮
            if (GUILayout.Button("重置为默认配置", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("重置配置", "确定要重置所有 Luban 配置为默认值？", "确定", "取消"))
                {
                    LubanConfig.ResetToDefaults();
                    RefreshEnvCheck();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPathField(string label, string tooltip, string currentValue,
            Action<string> setter, string extension)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(new GUIContent(label, tooltip), currentValue);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                var currentDir = currentValue;
                if (!Path.IsPathRooted(currentDir))
                {
                    currentDir = Path.Combine(
                        Directory.GetParent(Application.dataPath).FullName, currentDir);
                }

                EditorApplication.delayCall += () =>
                {
                    var path = EditorUtility.OpenFilePanel($"选择{label}", currentDir, extension);
                    if (!string.IsNullOrEmpty(path))
                    {
                        setter(path.Replace('\\', '/'));
                    }
                };
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFolderField(string label, string tooltip, string currentValue,
            Action<string> setter)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(new GUIContent(label, tooltip), currentValue);
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                var currentDir = currentValue;
                if (!Path.IsPathRooted(currentDir))
                {
                    currentDir = Path.Combine(
                        Directory.GetParent(Application.dataPath).FullName, currentDir);
                }

                EditorApplication.delayCall += () =>
                {
                    var path = EditorUtility.OpenFolderPanel($"选择{label}", currentDir, "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        setter(path.Replace('\\', '/'));
                    }
                };
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void OpenFileInExplorer(string path)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path);
            if (File.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"文件不存在: {fullPath}", "确定");
            }
        }

        private static void OpenDirectoryInExplorer(string path)
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(projectRoot, path);
            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"目录不存在: {fullPath}", "确定");
            }
        }

        #endregion

        #region 操作

        private void OnGenerate()
        {
            if (_isGenerating) return;
            _isGenerating = true;
            _statusText = "正在生成...";
            _statusColor = Color.yellow;
            _logs.Clear();

            AppendLog("[信息] 开始执行 Luban 生成...");

            EditorApplication.delayCall += () =>
            {
                var result = LubanGenerator.Generate(msg =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        AppendLog(msg);
                    };
                });

                _isGenerating = false;
                EditorApplication.delayCall += () =>
                {
                    if (result.Success)
                    {
                        _statusText = $"生成成功 ({result.Duration.TotalSeconds:F1}s)";
                        _statusColor = new Color(0.3f, 0.8f, 0.3f);
                        AppendLog($"[信息] 生成成功！耗时: {result.Duration.TotalSeconds:F1}s");
                    }
                    else
                    {
                        _statusText = "生成失败";
                        _statusColor = new Color(1f, 0.4f, 0.3f);
                        AppendLog($"[ERROR] 生成失败: {result.Error}");
                    }

                    Repaint();
                };
            };

            Repaint();
        }

        private void RefreshEnvCheck()
        {
            if (LubanGenerator.CheckEnvironment(out var message))
            {
                _envCheckText = message;
                _envOk = true;
            }
            else
            {
                _envCheckText = message;
                _envOk = false;
            }
        }

        private void AppendLog(string message)
        {
            _logs.Add(message);
        }

        #endregion
    }
}
