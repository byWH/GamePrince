using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GamePrince
{
    public class GitCommit
    {
        public DateTime Date { get; set; }
        public string Hash { get; set; } = "";
        public string Message { get; set; } = "";
        public string Author { get; set; } = "";
    }

    public class GitBranch
    {
        public string Name { get; set; } = "";
        public bool IsCurrent { get; set; }
        public string LastCommitHash { get; set; } = "";
    }

    public class GitFileChange
    {
        public string FilePath { get; set; } = "";
        public string ChangeType { get; set; } = ""; // Added, Modified, Deleted, Renamed
        public int LinesAdded { get; set; }
        public int LinesDeleted { get; set; }
    }

    public class GitCommitDetail
    {
        public string Hash { get; set; } = "";
        public string FullHash { get; set; } = "";
        public DateTime Date { get; set; }
        public string Message { get; set; } = "";
        public string Author { get; set; } = "";
        public string AuthorEmail { get; set; } = "";
        public List<GitFileChange> Changes { get; set; } = new();
    }

    public class GitDiffResult
    {
        public string FromRef { get; set; } = "";
        public string ToRef { get; set; } = "";
        public int TotalFilesChanged { get; set; }
        public int TotalAdditions { get; set; }
        public int TotalDeletions { get; set; }
        public List<GitFileChange> Changes { get; set; } = new();
    }

    public static class GitService
    {
        private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
        private static readonly Dictionary<string, (DateTime Timestamp, object Data)> _cache = new();
        private static readonly object _cacheLock = new object();

        /// <summary>
        /// 贡献者统计信息
        /// </summary>
        public class GitContributor
        {
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
            public int CommitCount { get; set; }
            public int LinesAdded { get; set; }
            public int LinesDeleted { get; set; }
            public Dictionary<DayOfWeek, int> ActivityByDay { get; set; } = new();
            public Dictionary<int, int> ActivityByHour { get; set; } = new();
        }

        /// <summary>
        /// Git标签信息
        /// </summary>
        public class GitTag
        {
            public string Name { get; set; } = "";
            public string CommitHash { get; set; } = "";
            public DateTime? Date { get; set; }
            public string Message { get; set; } = "";
        }

        /// <summary>
        /// 项目健康度指标
        /// </summary>
        public class ProjectHealthMetrics
        {
            public double ActivityScore { get; set; }
            public double GrowthScore { get; set; }
            public double CollaborationScore { get; set; }
            public double ConsistencyScore { get; set; }
            public string HealthLevel { get; set; } = "";
            public int TotalCommits { get; set; }
            public int ActiveContributors { get; set; }
            public int WeeklyCommits { get; set; }
            public int WeeklyAdditions { get; set; }
            public int WeeklyDeletions { get; set; }
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                LoggerService.Debug("GitService", "缓存已清除");
            }
        }

        /// <summary>
        /// 获取提交历史（带缓存）
        /// </summary>
        public static List<GitCommit> GetCommitHistory(string projectPath, int limit = 50)
        {
            var commits = new List<GitCommit>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return commits;
            }

            // 检查缓存
            string cacheKey = $"commits_{projectPath}_{limit}";
            if (TryGetCache(cacheKey, out var cached))
            {
                LoggerService.Debug("GitService", "从缓存获取提交历史");
                return (List<GitCommit>)cached;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"log --pretty=format:\"%h|%ai|%an|%s\" -n {limit}",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };

                // 设置环境变量确保Git输出UTF-8编码
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            var parts = line.Split('|');
                            if (parts.Length >= 4)
                            {
                                if (DateTime.TryParse(parts[1], out DateTime date))
                                {
                                    commits.Add(new GitCommit 
                                    { 
                                        Hash = parts[0], 
                                        Date = date,
                                        Author = parts[2],
                                        Message = parts[3]
                                    });
                                }
                            }
                            else if (parts.Length == 2)
                            {
                                // Backward compatibility for simple format
                                if (DateTime.TryParse(parts[1], out DateTime date))
                                {
                                    commits.Add(new GitCommit { Hash = parts[0], Date = date });
                                }
                            }
                        }
                        process.WaitForExit();
                    }
                }

                LoggerService.Info("GitService", $"成功获取 {commits.Count} 条提交记录");
                
                // 存入缓存
                SetCache(cacheKey, commits);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LoggerService.Error("GitService", $"Git未安装或不在PATH中: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取提交历史失败: {ex.Message}", ex);
            }

            return commits;
        }

        /// <summary>
        /// 获取活动热力图数据（带缓存）
        /// </summary>
        public static Dictionary<DateTime, int> GetActivityHeatmap(string projectPath)
        {
            // 检查缓存
            string cacheKey = $"heatmap_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (Dictionary<DateTime, int>)cached;
            }

            // Get all commits without limit for heatmap
            var history = GetCommitHistory(projectPath, 10000);
            var result = history
                .GroupBy(c => c.Date.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            // 存入缓存
            SetCache(cacheKey, result);
            return result;
        }

        /// <summary>
        /// 获取每日代码行数变化（带缓存）
        /// </summary>
        public static Dictionary<DateTime, (int Added, int Deleted)> GetDailyCodeLines(string projectPath)
        {
            // 检查缓存
            string cacheKey = $"codelines_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (Dictionary<DateTime, (int Added, int Deleted)>)cached;
            }

            var result = new Dictionary<DateTime, (int Added, int Deleted)>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return result;
            }

            try
            {
                // 使用 git log 获取每天的代码行数变化
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "log --pretty=format:\"%ai\" --numstat --date=short",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        DateTime currentDate = DateTime.MinValue;
                        int dailyAdded = 0;
                        int dailyDeleted = 0;

                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            // 解析日期行 (ISO格式日期，如 2024-01-15)
                            if (line.Length >= 10 && DateTime.TryParse(line.Substring(0, 10), out DateTime commitDate))
                            {
                                // 保存前一天的数据
                                if (currentDate != DateTime.MinValue && currentDate != commitDate)
                                {
                                    if (!result.ContainsKey(currentDate))
                                        result[currentDate] = (0, 0);
                                    var (prevAdded, prevDeleted) = result[currentDate];
                                    result[currentDate] = (prevAdded + dailyAdded, prevDeleted + dailyDeleted);
                                    
                                    // 重置计数器
                                    dailyAdded = 0;
                                    dailyDeleted = 0;
                                }
                                currentDate = commitDate;
                            }
                            else if (!string.IsNullOrWhiteSpace(line) && line.Contains("\t"))
                            {
                                // 解析 numstat 行: "100\t50\tfilename"
                                var parts = line.Split('\t');
                                if (parts.Length >= 2)
                                {
                                    if (int.TryParse(parts[0], out int added) && added > 0)
                                        dailyAdded += added;
                                    if (int.TryParse(parts[1], out int deleted) && deleted > 0)
                                        dailyDeleted += deleted;
                                }
                            }
                        }

                        // 保存最后一天的数据
                        if (currentDate != DateTime.MinValue)
                        {
                            if (!result.ContainsKey(currentDate))
                                result[currentDate] = (0, 0);
                            var (prevAdded, prevDeleted) = result[currentDate];
                            result[currentDate] = (prevAdded + dailyAdded, prevDeleted + dailyDeleted);
                        }
                    }
                    process.WaitForExit();
                }

                LoggerService.Info("GitService", $"成功获取 {result.Count} 天的代码行数数据");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LoggerService.Error("GitService", $"Git未安装或不在PATH中: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取代码行数失败: {ex.Message}", ex);
            }

            // 存入缓存
            SetCache(cacheKey, result);
            return result;
        }

        /// <summary>
        /// 获取总提交数
        /// </summary>
        public static int GetTotalCommits(string projectPath)
        {
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                return 0;

            // 检查缓存
            string cacheKey = $"totalcommits_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (int)cached;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-list --count HEAD",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? output = process.StandardOutput.ReadToEnd().Trim();
                        process.WaitForExit();
                        if (int.TryParse(output, out int count))
                        {
                            SetCache(cacheKey, count);
                            return count;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取总提交数失败: {ex.Message}", ex);
            }

            return 0;
        }

        public static Dictionary<string, int> GetFileTypeDistribution(string projectPath)
        {
            if (!Directory.Exists(projectPath)) return new Dictionary<string, int>();

            var excludedDirs = new[] { ".git", "bin", "obj", "node_modules", ".vite", ".import", "export_presets" };
            var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !excludedDirs.Any(d => f.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar) || f.Contains(Path.Combine(projectPath, d))));

            return files
                .GroupBy(f => Path.GetExtension(f).ToLower())
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// 获取 Godot 特有文件类型的分布统计
        /// </summary>
        public static Dictionary<string, int> GetGodotFileTypeDistribution(string projectPath)
        {
            var distribution = new Dictionary<string, int>();
            
            // 初始化 Godot 特有文件类型
            var godotExtensions = new[] { ".gd", ".tscn", ".tres", ".gdshader", ".shader", ".gdnlib", ".gdext" };
            foreach (var ext in godotExtensions)
            {
                distribution[ext] = 0;
            }
            
            if (!Directory.Exists(projectPath)) return distribution;

            var excludedDirs = new[] { ".git", "bin", "obj", "node_modules", ".vite", ".import", "export_presets" };
            var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !excludedDirs.Any(d => f.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar) || f.Contains(Path.Combine(projectPath, d))));

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file).ToLower();
                if (godotExtensions.Contains(ext) && distribution.ContainsKey(ext))
                {
                    distribution[ext]++;
                }
            }

            return distribution;
        }

        /// <summary>
        /// 获取分支列表
        /// </summary>
        public static List<GitBranch> GetBranches(string projectPath)
        {
            var branches = new List<GitBranch>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return branches;
            }

            // 检查缓存
            string cacheKey = $"branches_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (List<GitBranch>)cached;
            }

            try
            {
                // 获取所有分支
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch -a",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            var trimmed = line.Trim();
                            if (string.IsNullOrEmpty(trimmed)) continue;
                            
                            bool isCurrent = trimmed.StartsWith("*");
                            string branchName = trimmed.TrimStart('*').Trim();
                            
                            branches.Add(new GitBranch
                            {
                                Name = branchName,
                                IsCurrent = isCurrent
                            });
                        }
                        process.WaitForExit();
                    }
                }

                // 获取每个分支的最新提交哈希
                for (int i = 0; i < branches.Count; i++)
                {
                    var branch = branches[i];
                    string hash = GetBranchLastCommitHash(projectPath, branch.Name);
                    branch.LastCommitHash = hash;
                }

                LoggerService.Info("GitService", $"成功获取 {branches.Count} 个分支");
                SetCache(cacheKey, branches);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LoggerService.Error("GitService", $"Git未安装或不在PATH中: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取分支列表失败: {ex.Message}", ex);
            }

            return branches;
        }

        private static string GetBranchLastCommitHash(string projectPath, string branchName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"log -1 --pretty=\"%h\" {branchName}",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? hash = process.StandardOutput.ReadLine();
                        process.WaitForExit();
                        return hash ?? "";
                    }
                }
            }
            catch { }
            return "";
        }

        /// <summary>
        /// 获取单个提交的详细信息
        /// </summary>
        public static GitCommitDetail GetCommitDetail(string projectPath, string commitHash)
        {
            var detail = new GitCommitDetail { Hash = commitHash, FullHash = commitHash };
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return detail;
            }

            try
            {
                // 获取提交详情
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"log -1 --pretty=\"%H|%ai|%an|%ae|%s\" {commitHash}",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line = process.StandardOutput.ReadLine();
                        if (line != null)
                        {
                            var parts = line.Split('|');
                            if (parts.Length >= 5)
                            {
                                detail.FullHash = parts[0];
                                if (DateTime.TryParse(parts[1], out DateTime date))
                                    detail.Date = date;
                                detail.Author = parts[2];
                                detail.AuthorEmail = parts[3];
                                detail.Message = parts[4];
                            }
                        }
                        process.WaitForExit();
                    }
                }

                // 获取提交的文件变更
                psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"show {commitHash} --name-status --pretty=\"\"",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split('\t');
                            if (parts.Length >= 2)
                            {
                                var change = new GitFileChange
                                {
                                    ChangeType = parts[0] switch
                                    {
                                        "A" => "Added",
                                        "M" => "Modified",
                                        "D" => "Deleted",
                                        "R" => "Renamed",
                                        "C" => "Copied",
                                        _ => parts[0]
                                    },
                                    FilePath = parts[1]
                                };
                                detail.Changes.Add(change);
                            }
                        }
                        process.WaitForExit();
                    }
                }

                LoggerService.Debug("GitService", $"获取提交 {commitHash} 详情，包含 {detail.Changes.Count} 个文件变更");
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取提交详情失败: {ex.Message}", ex);
            }

            return detail;
        }

        /// <summary>
        /// 比较两个分支或提交的差异
        /// </summary>
        public static GitDiffResult CompareRefs(string projectPath, string fromRef, string toRef)
        {
            var result = new GitDiffResult 
            { 
                FromRef = fromRef, 
                ToRef = toRef 
            };
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                return result;

            try
            {
                // 使用 diff-tree 获取统计信息
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"diff --stat {fromRef}..{toRef}",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            // 解析形如: "src/Main.tscn | 5 ++---" 的行
                            if (line.Contains("|"))
                            {
                                var parts = line.Split('|');
                                if (parts.Length == 2)
                                {
                                    var filePath = parts[0].Trim();
                                    var stats = parts[1].Trim();
                                    
                                    int additions = 0, deletions = 0;
                                    var plusMatches = System.Text.RegularExpressions.Regex.Matches(stats, @"\+");
                                    var minusMatches = System.Text.RegularExpressions.Regex.Matches(stats, @"-");
                                    additions = plusMatches.Count;
                                    deletions = minusMatches.Count;
                                    
                                    result.Changes.Add(new GitFileChange
                                    {
                                        FilePath = filePath,
                                        ChangeType = "Modified",
                                        LinesAdded = additions,
                                        LinesDeleted = deletions
                                    });
                                    
                                    result.TotalAdditions += additions;
                                    result.TotalDeletions += deletions;
                                }
                            }
                        }
                        process.WaitForExit();
                    }
                }

                // 获取新增和删除的文件
                psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"diff --name-status {fromRef}..{toRef}",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split('\t');
                            if (parts.Length >= 2)
                            {
                                string changeType = parts[0];
                                string filePath = parts[1];
                                
                                // 检查是否已存在
                                if (!result.Changes.Any(c => c.FilePath == filePath))
                                {
                                    result.Changes.Add(new GitFileChange
                                    {
                                        FilePath = filePath,
                                        ChangeType = changeType switch
                                        {
                                            "A" => "Added",
                                            "M" => "Modified",
                                            "D" => "Deleted",
                                            "R" => "Renamed",
                                            _ => changeType
                                        }
                                    });
                                }
                            }
                        }
                        process.WaitForExit();
                    }
                }

                result.TotalFilesChanged = result.Changes.Count;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Git error in CompareRefs: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 获取贡献者统计信息（带缓存）
        /// </summary>
        public static List<GitContributor> GetContributors(string projectPath)
        {
            // 检查缓存
            string cacheKey = $"contributors_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (List<GitContributor>)cached;
            }

            var contributors = new List<GitContributor>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return contributors;
            }

            try
            {
                // 获取所有提交作者的统计信息
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "log --pretty=format:\"%an|%ae\" --numstat",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                var authorData = new Dictionary<string, GitContributor>();
                string currentAuthor = "";
                string currentEmail = "";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            
                            // 检查是否是作者行（不包含制表符）
                            if (!line.Contains('\t'))
                            {
                                var parts = line.Split('|');
                                if (parts.Length >= 2)
                                {
                                    currentAuthor = parts[0];
                                    currentEmail = parts[1];
                                    
                                    if (!authorData.ContainsKey(currentAuthor))
                                    {
                                        authorData[currentAuthor] = new GitContributor
                                        {
                                            Name = currentAuthor,
                                            Email = currentEmail
                                        };
                                    }
                                }
                            }
                            else if (currentAuthor != "" && line.Contains('\t'))
                            {
                                // 这是 numstat 行，包含代码行数
                                var parts = line.Split('\t');
                                if (parts.Length >= 2 && authorData.ContainsKey(currentAuthor))
                                {
                                    if (int.TryParse(parts[0], out int added) && added > 0)
                                        authorData[currentAuthor].LinesAdded += added;
                                    if (int.TryParse(parts[1], out int deleted) && deleted > 0)
                                        authorData[currentAuthor].LinesDeleted += deleted;
                                    authorData[currentAuthor].CommitCount++;
                                }
                            }
                        }
                        process.WaitForExit();
                    }
                }

                // 获取提交按日期分布的信息
                psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "log --pretty=format:\"%an|%ai\" -n 500",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split('|');
                            if (parts.Length >= 2)
                            {
                                string authorName = parts[0];
                                if (authorData.ContainsKey(authorName) && DateTime.TryParse(parts[1], out DateTime date))
                                {
                                    // 按星期分布
                                    var dayOfWeek = date.DayOfWeek;
                                    if (!authorData[authorName].ActivityByDay.ContainsKey(dayOfWeek))
                                        authorData[authorName].ActivityByDay[dayOfWeek] = 0;
                                    authorData[authorName].ActivityByDay[dayOfWeek]++;
                                    
                                    // 按小时分布
                                    int hour = date.Hour;
                                    if (!authorData[authorName].ActivityByHour.ContainsKey(hour))
                                        authorData[authorName].ActivityByHour[hour] = 0;
                                    authorData[authorName].ActivityByHour[hour]++;
                                }
                            }
                        }
                        process.WaitForExit();
                    }
                }

                contributors = authorData.Values.OrderByDescending(c => c.CommitCount).ToList();
                LoggerService.Info("GitService", $"成功获取 {contributors.Count} 个贡献者");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LoggerService.Error("GitService", $"Git未安装或不在PATH中: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取贡献者统计失败: {ex.Message}", ex);
            }

            // 存入缓存
            SetCache(cacheKey, contributors);
            return contributors;
        }

        /// <summary>
        /// 获取 Git 标签列表（带缓存）
        /// </summary>
        public static List<GitTag> GetTags(string projectPath)
        {
            // 检查缓存
            string cacheKey = $"tags_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (List<GitTag>)cached;
            }

            var tags = new List<GitTag>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return tags;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "tag -l --sort=-creatordate --format=\"%(refname:short)|%(creatordate:format:%Y-%m-%d)|%(objectname:short)|%(contents:subject)\"",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            var parts = line.Split('|');
                            if (parts.Length >= 1)
                            {
                                var tag = new GitTag
                                {
                                    Name = parts[0]
                                };
                                
                                if (parts.Length >= 2 && DateTime.TryParse(parts[1], out DateTime date))
                                    tag.Date = date;
                                
                                if (parts.Length >= 3)
                                    tag.CommitHash = parts[2];
                                
                                if (parts.Length >= 4)
                                    tag.Message = parts[3];
                                
                                tags.Add(tag);
                            }
                        }
                        process.WaitForExit();
                    }
                }

                LoggerService.Info("GitService", $"成功获取 {tags.Count} 个标签");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                LoggerService.Error("GitService", $"Git未安装或不在PATH中: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取标签列表失败: {ex.Message}", ex);
            }

            // 存入缓存
            SetCache(cacheKey, tags);
            return tags;
        }

        /// <summary>
        /// 获取项目健康度指标（带缓存）
        /// </summary>
        public static ProjectHealthMetrics GetProjectHealth(string projectPath)
        {
            // 检查缓存
            string cacheKey = $"health_{projectPath}";
            if (TryGetCache(cacheKey, out var cached))
            {
                return (ProjectHealthMetrics)cached;
            }

            var metrics = new ProjectHealthMetrics();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
            {
                LoggerService.Warning("GitService", $"目录不是Git仓库: {projectPath}");
                return metrics;
            }

            try
            {
                // 获取总提交数
                metrics.TotalCommits = GetTotalCommits(projectPath);
                
                // 获取贡献者数量
                var contributors = GetContributors(projectPath);
                metrics.ActiveContributors = contributors.Count;

                // 计算本周提交数
                var weekAgo = DateTime.Now.AddDays(-7);
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"log --since=\"{weekAgo:yyyy-MM-dd}\" --pretty=format:\"%h\" --numstat",
                    WorkingDirectory = projectPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = System.Text.Encoding.UTF8
                };
                psi.EnvironmentVariables["LANG"] = "zh_CN.UTF-8";

                int weeklyCommits = 0;
                int weeklyAdditions = 0;
                int weeklyDeletions = 0;
                var processedCommits = new HashSet<string>();

                using (Process? process = Process.Start(psi))
                {
                    if (process != null)
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (string.IsNullOrWhiteSpace(line)) continue;
                            
                            // 检查是否是提交行（不包含制表符）
                            if (!line.Contains('\t') && line.Length >= 7)
                            {
                                string commitHash = line.Trim();
                                if (!processedCommits.Contains(commitHash))
                                {
                                    processedCommits.Add(commitHash);
                                    weeklyCommits++;
                                }
                            }
                            else if (line.Contains('\t'))
                            {
                                var parts = line.Split('\t');
                                if (parts.Length >= 2)
                                {
                                    if (int.TryParse(parts[0], out int added))
                                        weeklyAdditions += added;
                                    if (int.TryParse(parts[1], out int deleted))
                                        weeklyDeletions += deleted;
                                }
                            }
                        }
                        process.WaitForExit();
                    }
                }

                metrics.WeeklyCommits = weeklyCommits;
                metrics.WeeklyAdditions = weeklyAdditions;
                metrics.WeeklyDeletions = weeklyDeletions;

                // 计算活跃度得分 (0-100)
                // 基于每周提交数：0-5=低(0-40分), 6-15=中(40-70分), 16+=高(70-100分)
                metrics.ActivityScore = weeklyCommits switch
                {
                    >= 20 => 100,
                    >= 15 => 90,
                    >= 10 => 75,
                    >= 5 => 60,
                    >= 3 => 45,
                    >= 1 => 30,
                    _ => 15
                };

                // 计算增长得分 (0-100)
                // 基于代码净增长
                int netChange = weeklyAdditions - weeklyDeletions;
                metrics.GrowthScore = netChange switch
                {
                    > 500 => 100,
                    > 300 => 85,
                    > 100 => 70,
                    > 0 => 55,
                    > -100 => 45,
                    > -300 => 30,
                    _ => 20
                };

                // 计算协作得分 (0-100)
                // 基于贡献者数量和提交分布
                metrics.CollaborationScore = contributors.Count switch
                {
                    >= 10 => 100,
                    >= 5 => 85,
                    >= 3 => 70,
                    >= 2 => 55,
                    1 => 40,
                    _ => 30
                };

                // 计算一致性得分 (0-100)
                // 基于提交时间分布的规律性
                if (contributors.Count > 0)
                {
                    var avgCommitsPerContributor = (double)metrics.TotalCommits / contributors.Count;
                    double variance = contributors.Sum(c => Math.Pow(c.CommitCount - avgCommitsPerContributor, 2)) / contributors.Count;
                    double stdDev = Math.Sqrt(variance);
                    double coefficientOfVariation = avgCommitsPerContributor > 0 ? stdDev / avgCommitsPerContributor : 1;
                    
                    // CV 越低，一致性越高
                    metrics.ConsistencyScore = coefficientOfVariation switch
                    {
                        < 0.3 => 100,
                        < 0.5 => 85,
                        < 0.7 => 70,
                        < 1.0 => 55,
                        < 1.5 => 40,
                        _ => 25
                    };
                }
                else
                {
                    metrics.ConsistencyScore = 50;
                }

                // 计算综合健康等级
                double overallScore = (metrics.ActivityScore + metrics.GrowthScore + metrics.CollaborationScore + metrics.ConsistencyScore) / 4;
                metrics.HealthLevel = overallScore switch
                {
                    >= 80 => "健康",
                    >= 60 => "良好",
                    >= 40 => "一般",
                    _ => "需关注"
                };

                LoggerService.Info("GitService", $"项目健康度: {metrics.HealthLevel} ({overallScore:F1}分)");
            }
            catch (Exception ex)
            {
                LoggerService.Error("GitService", $"获取项目健康度失败: {ex.Message}", ex);
            }

            // 存入缓存
            SetCache(cacheKey, metrics);
            return metrics;
        }

        /// <summary>
        /// 尝试从缓存获取数据
        /// </summary>
        private static bool TryGetCache(string key, out object? data)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (DateTime.Now - entry.Timestamp < CacheExpiration)
                    {
                        data = entry.Data;
                        return true;
                    }
                    // 过期移除
                    _cache.Remove(key);
                }
                data = null;
                return false;
            }
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        private static void SetCache(string key, object data)
        {
            lock (_cacheLock)
            {
                _cache[key] = (DateTime.Now, data);
            }
        }
    }
}
