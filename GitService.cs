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
        public static List<GitCommit> GetCommitHistory(string projectPath, int limit = 50)
        {
            var commits = new List<GitCommit>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                return commits;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = $"log --pretty=format:\"%h|%ai|%an|%s\" -n {limit}",
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
            }
            catch (Exception ex)
            {
                // Git not installed or other error - log for debugging
                System.Diagnostics.Debug.WriteLine($"Git error in GetCommitHistory: {ex.Message}");
            }

            return commits;
        }

        public static Dictionary<DateTime, int> GetActivityHeatmap(string projectPath)
        {
            // Get all commits without limit for heatmap
            var history = GetCommitHistory(projectPath, 10000);
            return history
                .GroupBy(c => c.Date.Date)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static int GetTotalCommits(string projectPath)
        {
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                return 0;

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
                            return count;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Git error in GetTotalCommits: {ex.Message}");
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
        /// 获取 Git 分支列表
        /// </summary>
        public static List<GitBranch> GetBranches(string projectPath)
        {
            var branches = new List<GitBranch>();
            
            if (!Directory.Exists(Path.Combine(projectPath, ".git")))
                return branches;

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
            }
            catch (Exception ex)
            {
                // Git not installed or other error - log for debugging
                System.Diagnostics.Debug.WriteLine($"Git error: {ex.Message}");
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
                return detail;

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
                    CreateNoWindow = true
                };

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
                    CreateNoWindow = true
                };

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Git error in GetCommitDetail: {ex.Message}");
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
                    CreateNoWindow = true
                };

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
                    CreateNoWindow = true
                };

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
        /// 获取分支列表（用于分支选择下拉框）
        /// </summary>
        public static List<string> GetBranchNames(string projectPath)
        {
            return GetBranches(projectPath).Select(b => b.Name).ToList();
        }
    }
}
