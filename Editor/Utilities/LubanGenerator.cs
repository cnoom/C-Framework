using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CFramework.Editor.Utilities
{
    /// <summary>
    ///     Luban 命令行生成器
    ///     <para>封装 Luban CLI 的调用逻辑，从 LubanConfig（EditorPrefs）读取配置</para>
    /// </summary>
    public static class LubanGenerator
    {
        /// <summary>
        ///     生成结果
        /// </summary>
        public struct GenerateResult
        {
            public bool Success;
            public string Output;
            public string Error;
            public int ExitCode;
            public TimeSpan Duration;
        }

        /// <summary>
        ///     执行 Luban 生成
        /// </summary>
        /// <param name="onLog">日志回调（可选）</param>
        /// <returns>生成结果</returns>
        public static GenerateResult Generate(Action<string> onLog = null)
        {
            var projectRoot = GetProjectRoot();
            var lubanDll = ResolvePath(Configs.LubanConfig.LubanDllPath, projectRoot);
            var confFile = ResolvePath(Configs.LubanConfig.ConfPath, projectRoot);
            var outputCodeDir = ResolvePath(Configs.LubanConfig.OutputCodeDir, projectRoot);
            var outputDataDir = ResolvePath(Configs.LubanConfig.OutputDataDir, projectRoot);

            // 校验路径
            if (!File.Exists(lubanDll))
            {
                return new GenerateResult
                {
                    Success = false,
                    Error = $"Luban DLL 不存在: {lubanDll}\n请在 CFramework → Luban → 设置 中配置正确的路径"
                };
            }

            if (!File.Exists(confFile))
            {
                return new GenerateResult
                {
                    Success = false,
                    Error = $"luban.conf 不存在: {confFile}\n请先创建 Luban 配置文件"
                };
            }

            // 确保输出目录存在
            Directory.CreateDirectory(outputCodeDir);
            Directory.CreateDirectory(outputDataDir);

            // 构建命令行参数
            var args = BuildArguments(confFile, outputCodeDir, outputDataDir);

            onLog?.Invoke($"[Luban] 开始生成...");
            onLog?.Invoke($"[Luban] DLL: {lubanDll}");
            onLog?.Invoke($"[Luban] Conf: {confFile}");
            onLog?.Invoke($"[Luban] Target: {Configs.LubanConfig.TargetName}");
            onLog?.Invoke($"[Luban] CodeTarget: {Configs.LubanConfig.CodeTarget}");
            onLog?.Invoke($"[Luban] DataTarget: {Configs.LubanConfig.DataTarget}");
            onLog?.Invoke($"[Luban] OutputCode: {outputCodeDir}");
            onLog?.Invoke($"[Luban] OutputData: {outputDataDir}");

            var stopwatch = Stopwatch.StartNew();

            // 自动检测运行方式：自包含 exe 直接运行，框架依赖 dll 通过 dotnet 运行
            var (fileName, arguments) = ResolveExecutable(lubanDll, args, onLog);

            onLog?.Invoke($"[Luban] 执行方式: {fileName}");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = projectRoot
                };

                using var process = new Process { StartInfo = startInfo };
                var outputBuilder = new System.Text.StringBuilder();
                var errorBuilder = new System.Text.StringBuilder();

                process.OutputDataReceived += (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    outputBuilder.AppendLine(e.Data);
                    onLog?.Invoke(e.Data);
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    errorBuilder.AppendLine(e.Data);
                    onLog?.Invoke($"[ERROR] {e.Data}");
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                stopwatch.Stop();

                var result = new GenerateResult
                {
                    ExitCode = process.ExitCode,
                    Output = outputBuilder.ToString(),
                    Error = errorBuilder.ToString(),
                    Duration = stopwatch.Elapsed,
                    Success = process.ExitCode == 0
                };

                if (result.Success)
                {
                    onLog?.Invoke($"[Luban] 生成完成！耗时: {stopwatch.Elapsed.TotalSeconds:F1}s");
                    // 刷新 Unity 资产数据库
                    AssetDatabase.Refresh(ImportAssetOptions.ImportRecursive);
                }
                else
                {
                    onLog?.Invoke($"[Luban] 生成失败！退出码: {process.ExitCode}");
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                onLog?.Invoke($"[Luban] 异常: {ex.Message}");
                return new GenerateResult
                {
                    Success = false,
                    Error = ex.Message,
                    Duration = stopwatch.Elapsed
                };
            }
        }

        /// <summary>
        ///     构建命令行参数
        /// </summary>
        private static string BuildArguments(string confFile, string outputCodeDir, string outputDataDir)
        {
            var args = new List<string>();

            // 必选参数
            args.Add($"-t {Configs.LubanConfig.TargetName}");
            args.Add($"-c {Configs.LubanConfig.CodeTarget}");
            args.Add($"-d {Configs.LubanConfig.DataTarget}");
            args.Add($"--conf \"{confFile}\"");

            // xargs 扩展参数
            args.Add($"-x {Configs.LubanConfig.CodeTarget}.outputCodeDir=\"{outputCodeDir}\"");
            args.Add($"-x outputDataDir=\"{outputDataDir}\"");

            if (!string.IsNullOrEmpty(Configs.LubanConfig.TopModule))
            {
                args.Add($"-x topModule={Configs.LubanConfig.TopModule}");
            }

            // 标签过滤
            if (!string.IsNullOrEmpty(Configs.LubanConfig.IncludeTag))
            {
                args.Add($"-i {Configs.LubanConfig.IncludeTag}");
            }

            if (!string.IsNullOrEmpty(Configs.LubanConfig.ExcludeTag))
            {
                args.Add($"-e {Configs.LubanConfig.ExcludeTag}");
            }

            // 高级选项
            if (Configs.LubanConfig.ValidationFailAsError)
            {
                args.Add("--validationFailAsError");
            }

            if (Configs.LubanConfig.Verbose)
            {
                args.Add("-v");
            }

            if (!Configs.LubanConfig.CleanOutputDir)
            {
                args.Add($"-x outputSaver.{Configs.LubanConfig.CodeTarget}.cleanUpOutputDir=0");
            }

            if (!string.IsNullOrEmpty(Configs.LubanConfig.WatchDir))
            {
                args.Add($"-w {Configs.LubanConfig.WatchDir}");
            }

            return string.Join(" ", args);
        }

        /// <summary>
        ///     获取项目根目录（Assets 的上一级）
        /// </summary>
        private static string GetProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        /// <summary>
        ///     解析路径：绝对路径直接使用，相对路径拼接项目根目录
        /// </summary>
        private static string ResolvePath(string path, string projectRoot)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            return Path.GetFullPath(Path.Combine(projectRoot, path));
        }

        /// <summary>
        ///     解析 Luban 可执行文件路径和参数
        ///     <para>自动检测运行模式：</para>
        ///     <para>1. 自包含 exe（如 Luban.Core.exe）→ 直接运行</para>
        ///     <para>2. 框架依赖 dll（如 Luban.Core.dll）→ 通过 dotnet 运行</para>
        /// </summary>
        private static (string fileName, string arguments) ResolveExecutable(string lubanPath, string args,
            Action<string> onLog)
        {
            var extension = Path.GetExtension(lubanPath).ToLowerInvariant();

            if (extension == ".exe")
            {
                // 自包含发布版，直接运行 exe
                return (lubanPath, args);
            }

            if (extension == ".dll")
            {
                // 检查是否存在同名的 exe（自包含发布版）
                var exePath = Path.ChangeExtension(lubanPath, ".exe");
                if (File.Exists(exePath))
                {
                    onLog?.Invoke("[Luban] 检测到同目录 .exe，使用自包含模式运行");
                    return (exePath, args);
                }

                // 纯 dll，通过 dotnet 运行（框架依赖模式）
                return ("dotnet", $"\"{lubanPath}\" {args}");
            }

            return (lubanPath, args);
        }

        /// <summary>
        ///     检查 Luban 环境是否就绪
        /// </summary>
        /// <param name="message">状态信息</param>
        /// <returns>是否就绪</returns>
        public static bool CheckEnvironment(out string message)
        {
            var projectRoot = GetProjectRoot();
            var lubanPath = ResolvePath(Configs.LubanConfig.LubanDllPath, projectRoot);
            var confFile = ResolvePath(Configs.LubanConfig.ConfPath, projectRoot);

            if (!File.Exists(lubanPath))
            {
                message = $"Luban 文件不存在: {Configs.LubanConfig.LubanDllPath}\n" +
                          "请在 CFramework → Luban → 设置 中配置正确的路径\n" +
                          "支持 .exe（自包含版）或 .dll（框架依赖版）";
                return false;
            }

            if (!File.Exists(confFile))
            {
                message = $"luban.conf 不存在: {Configs.LubanConfig.ConfPath}\n" +
                          "请在设置中指定正确的配置文件路径";
                return false;
            }

            var ext = Path.GetExtension(lubanPath).ToLowerInvariant();

            // 如果是 dll，需要检查 dotnet 是否可用
            if (ext == ".dll")
            {
                // 优先检查是否有同目录 exe
                var exePath = Path.ChangeExtension(lubanPath, ".exe");
                if (File.Exists(exePath))
                {
                    message = "环境就绪（自包含模式）";
                    return true;
                }

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    process?.WaitForExit(5000);
                }
                catch
                {
                    message = "当前为 DLL 模式但未检测到 .NET SDK\n" +
                              "解决方案：\n" +
                              "1. 安装 .NET SDK 7.0+，或\n" +
                              "2. 将 Luban 路径改为同目录的 .exe 文件（自包含模式）\n" +
                              "下载地址: https://dotnet.microsoft.com/download";
                    return false;
                }
            }

            message = ext == ".exe"
                ? "环境就绪（自包含模式）"
                : "环境就绪";
            return true;
        }
    }
}
