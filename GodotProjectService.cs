using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GamePrince
{
    /// <summary>
    /// Godot 项目信息模型
    /// </summary>
    public class GodotProject
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Version { get; set; } = "";
        public string MainScene { get; set; } = "";
        public string Author { get; set; } = "";
        public bool IsValid { get; set; } = false;
    }

    /// <summary>
    /// 项目文件/目录节点（用于资源浏览器）
    /// </summary>
    public class ProjectNode
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public string FileType { get; set; } = "";  // 扩展名（小写）
        public List<ProjectNode> Children { get; set; } = new();
        public int Level { get; set; } = 0;  // 目录层级深度
    }

    /// <summary>
    /// 资源分类统计
    /// </summary>
    public class ResourceStats
    {
        public int Scripts { get; set; }       // .gd
        public int Scenes { get; set; }        // .tscn
        public int Resources { get; set; }     // .tres
        public int Shaders { get; set; }        // .gdshader
        public int Extensions { get; set; }    // .gdnlib
        public int Textures { get; set; }      // .png, .jpg, .webp, .svg
        public int Audio { get; set; }         // .ogg, .wav, .mp3
        public int Fonts { get; set; }          // .ttf, .otf
        public int Other { get; set; }
        
        public int Total => Scripts + Scenes + Resources + Shaders + Extensions + Textures + Audio + Fonts + Other;
    }

    /// <summary>
    /// Godot 插件信息
    /// </summary>
    public class GodotPluginInfo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Description { get; set; } = "";
        public string Author { get; set; } = "";
        public string Version { get; set; } = "";
        public bool IsEnabled { get; set; } = true;
    }

    public static class GodotProjectService
    {
        /// <summary>
        /// 检测指定目录是否为 Godot 项目
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>GodotProject 对象</returns>
        public static GodotProject DetectProject(string path)
        {
            var project = new GodotProject { Path = path };

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return project;

            string projectGodotPath = Path.Combine(path, "project.godot");
            if (!File.Exists(projectGodotPath))
                return project;

            // 解析 project.godot 文件
            try
            {
                string[] lines = File.ReadAllLines(projectGodotPath);
                foreach (string line in lines)
                {
                    // 解析项目名称
                    var nameMatch = Regex.Match(line, @"config/name\s*=\s*""([^""]+)""");
                    if (nameMatch.Success)
                    {
                        project.Name = nameMatch.Groups[1].Value;
                        continue;
                    }

                    // 解析主场景
                    var sceneMatch = Regex.Match(line, @"run/main_scene\s*=\s*""([^""]+)""");
                    if (sceneMatch.Success)
                    {
                        project.MainScene = sceneMatch.Groups[1].Value;
                        continue;
                    }

                    // 解析作者
                    var authorMatch = Regex.Match(line, @"config/features\s*=\s*PoolStringArray\s*\(\s*""([^""]+)""");
                    if (authorMatch.Success)
                    {
                        project.Author = authorMatch.Groups[1].Value;
                    }
                }

                project.IsValid = !string.IsNullOrEmpty(project.Name);
            }
            catch
            {
                // 解析失败，返回无效项目
            }

            return project;
        }

        /// <summary>
        /// 获取项目的资源结构树
        /// </summary>
        /// <param name="projectPath">项目根目录</param>
        /// <param name="maxDepth">最大递归深度</param>
        /// <returns>项目根节点</returns>
        public static ProjectNode GetProjectTree(string projectPath, int maxDepth = 5)
        {
            var root = new ProjectNode
            {
                Name = Path.GetFileName(projectPath),
                FullPath = projectPath,
                IsDirectory = true,
                Level = 0
            };

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return root;

            try
            {
                BuildTree(root, projectPath, 1, maxDepth);
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略无权限访问的目录
            }
            catch (Exception)
            {
                // 忽略其他错误
            }

            return root;
        }

        private static void BuildTree(ProjectNode parent, string path, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth)
                return;

            try
            {
                // 获取目录下的所有项
                var entries = Directory.GetFileSystemEntries(path);
                
                foreach (var entry in entries)
                {
                    string name = Path.GetFileName(entry);
                    
                    // 跳过隐藏文件和常见的忽略目录
                    if (name.StartsWith(".") || 
                        name == "node_modules" || 
                        name == ".git" || 
                        name == "build" || 
                        name == "export_presets" ||
                        name == ".import")
                        continue;

                    bool isDirectory = Directory.Exists(entry);
                    string extension = isDirectory ? "" : Path.GetExtension(entry).ToLower();

                    var node = new ProjectNode
                    {
                        Name = name,
                        FullPath = entry,
                        IsDirectory = isDirectory,
                        FileType = extension,
                        Level = currentDepth
                    };

                    // 如果是目录，递归添加子节点
                    if (isDirectory)
                    {
                        BuildTree(node, entry, currentDepth + 1, maxDepth);
                        
                        // 如果目录为空（没有子节点），可以选择不添加
                        // 但为了显示完整结构，这里保留空目录
                    }

                    parent.Children.Add(node);
                }

                // 对子节点排序：目录在前，文件在后，同类按名称排序
                parent.Children = parent.Children
                    .OrderByDescending(c => c.IsDirectory)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch (UnauthorizedAccessException)
            {
                // 忽略无权限访问
            }
            catch (Exception)
            {
                // 忽略其他错误
            }
        }

        /// <summary>
        /// 获取项目的资源统计信息
        /// </summary>
        /// <param name="projectPath">项目根目录</param>
        /// <returns>资源统计对象</returns>
        public static ResourceStats GetResourceStats(string projectPath)
        {
            var stats = new ResourceStats();

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return stats;

            try
            {
                var allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles)
                {
                    string name = Path.GetFileName(file);
                    
                    // 跳过隐藏文件和常见忽略目录
                    if (name.StartsWith(".") || 
                        file.Contains("/.git/") || 
                        file.Contains("/node_modules/") ||
                        file.Contains("/build/") ||
                        file.Contains("/.import/"))
                        continue;

                    string ext = Path.GetExtension(file).ToLower();
                    
                    switch (ext)
                    {
                        case ".gd":
                            stats.Scripts++;
                            break;
                        case ".tscn":
                            stats.Scenes++;
                            break;
                        case ".tres":
                            stats.Resources++;
                            break;
                        case ".gdshader":
                        case ".shader":
                            stats.Shaders++;
                            break;
                        case ".gdnlib":
                            stats.Extensions++;
                            break;
                        case ".png":
                        case ".jpg":
                        case ".jpeg":
                        case ".webp":
                        case ".svg":
                        case ".bmp":
                        case ".tga":
                        case ".hdr":
                            stats.Textures++;
                            break;
                        case ".ogg":
                        case ".wav":
                        case ".mp3":
                        case ".flac":
                        case ".aac":
                            stats.Audio++;
                            break;
                        case ".ttf":
                        case ".otf":
                        case ".woff":
                        case ".woff2":
                            stats.Fonts++;
                            break;
                        default:
                            // 排除一些常见的非资源文件
                            if (ext != ".godot" && ext != ".meta" && ext != ".import")
                                stats.Other++;
                            break;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return stats;
        }

        /// <summary>
        /// 获取 Godot 特有文件类型的分布统计（仅统计项目文件）
        /// </summary>
        /// <param name="projectPath">项目根目录</param>
        /// <returns>文件类型-数量 字典</returns>
        public static Dictionary<string, int> GetGodotFileTypeDistribution(string projectPath)
        {
            var distribution = new Dictionary<string, int>
            {
                { ".gd", 0 },       // GDScript
                { ".tscn", 0 },    // 场景
                { ".tres", 0 },    // 资源
                { ".gdshader", 0 }, // 着色器
                { ".shader", 0 },  // 着色器（旧格式）
                { ".gdnlib", 0 },  // 扩展库
                { ".gdext", 0 }    // 扩展
            };

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return distribution;

            try
            {
                var allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
                
                foreach (var file in allFiles)
                {
                    string name = Path.GetFileName(file);
                    
                    // 跳过隐藏文件
                    if (name.StartsWith("."))
                        continue;

                    string ext = Path.GetExtension(file).ToLower();
                    
                    if (distribution.ContainsKey(ext))
                    {
                        distribution[ext]++;
                    }
                }
            }
            catch
            {
                // 忽略错误
            }

            return distribution;
        }

        /// <summary>
        /// 获取项目的插件列表
        /// </summary>
        /// <param name="projectPath">项目根目录</param>
        /// <returns>插件信息列表</returns>
        public static List<GodotPluginInfo> GetPlugins(string projectPath)
        {
            var plugins = new List<GodotPluginInfo>();

            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return plugins;

            string pluginDir = Path.Combine(projectPath, "addons");
            if (!Directory.Exists(pluginDir))
                return plugins;

            try
            {
                var addonDirs = Directory.GetDirectories(pluginDir);
                foreach (var addonPath in addonDirs)
                {
                    string pluginName = Path.GetFileName(addonPath);
                    var plugin = new GodotPluginInfo
                    {
                        Name = pluginName,
                        Path = addonPath,
                        IsEnabled = true
                    };

                    // 尝试读取 plugin.cfg 获取插件信息
                    string pluginCfg = Path.Combine(addonPath, "plugin.cfg");
                    if (File.Exists(pluginCfg))
                    {
                        try
                        {
                            var lines = File.ReadAllLines(pluginCfg);
                            foreach (var line in lines)
                            {
                                if (line.StartsWith("name="))
                                    plugin.Name = line.Substring(5).Trim('"');
                                else if (line.StartsWith("description="))
                                    plugin.Description = line.Substring(12).Trim('"');
                                else if (line.StartsWith("author="))
                                    plugin.Author = line.Substring(7).Trim('"');
                                else if (line.StartsWith("version="))
                                    plugin.Version = line.Substring(8).Trim('"');
                            }
                        }
                        catch
                        {
                            // 解析失败，使用默认名称
                        }
                    }

                    plugins.Add(plugin);
                }
            }
            catch
            {
                // 忽略错误
            }

            return plugins;
        }

        /// <summary>
        /// 尝试启动 Godot Editor
        /// </summary>
        /// <param name="projectPath">项目路径</param>
        /// <returns>是否成功启动</returns>
        public static bool OpenInEditor(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return false;

            // 常见的 Godot 可执行文件位置
            var godotExecutables = new[]
            {
                "godot",
                "godot4",
                "godot4.2",
                "godot4.1",
                "godot3",
                "godot-editor",
                @"C:\Godot\Godot.exe",
                @"C:\Godot\godot.exe",
                @"C:\Program Files\Godot\Godot.exe",
                @"C:\Program Files (x86)\Godot\Godot.exe"
            };

            // 先检查项目目录是否有 Godot
            string projectGodotExe = Path.Combine(projectPath, "godot.exe");
            if (File.Exists(projectGodotExe))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = projectGodotExe,
                        Arguments = projectPath,
                        UseShellExecute = true
                    });
                    return true;
                }
                catch { }
            }

            // 尝试在 PATH 中查找
            foreach (var godotExe in godotExecutables)
            {
                try
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = godotExe,
                        Arguments = projectPath,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(startInfo);
                    return true;
                }
                catch
                {
                    // 继续尝试下一个
                }
            }

            return false;
        }
    }
}
