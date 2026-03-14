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
            return GetCommitHistory(projectPath).Count;
        }

        public static Dictionary<string, int> GetFileTypeDistribution(string projectPath)
        {
            if (!Directory.Exists(projectPath)) return new Dictionary<string, int>();

            var excludedDirs = new[] { ".git", "bin", "obj", "node_modules", ".vite" };
            var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !excludedDirs.Any(d => f.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar) || f.Contains(Path.Combine(projectPath, d))));

            return files
                .GroupBy(f => Path.GetExtension(f).ToLower())
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.Count());
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
    }
}
