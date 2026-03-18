using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GamePrince
{
    /// <summary>
    /// 数据服务异常
    /// </summary>
    public class DataServiceException : Exception
    {
        public string FilePath { get; set; } = "";
        public DataServiceException(string message) : base(message) { }
        public DataServiceException(string message, Exception innerException) : base(message, innerException) { }
        public DataServiceException(string message, string filePath, Exception innerException) : base(message, innerException)
        {
            FilePath = filePath;
        }
    }

    public class TaskItem
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Status { get; set; } = "Task Pool"; // Task Pool, In Progress, Completed
        public string DateCreated { get; set; } = System.DateTime.Now.ToString("yyyy-MM-dd");
        public string? DateCompleted { get; set; }
        public int Urgency { get; set; } = 3; // 1-5
        public int Importance { get; set; } = 3; // 1-5
        public List<string> Tags { get; set; } = new();
        public string MilestoneId { get; set; } = "";
        public string? DueDate { get; set; }
        public double EstimatedHours { get; set; } = 0;
        public double LoggedHours { get; set; } = 0;
        public string? LastTimerStart { get; set; }
    }

    public class Milestone
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "";
        public string StartDate { get; set; } = System.DateTime.Now.ToString("yyyy-MM-dd");
        public string TargetDate { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
    }

    public class ReleaseInfo
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();
        public string Version { get; set; } = "";  // 语义化版本号
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string ReleaseDate { get; set; } = "";
        public string Status { get; set; } = "Planning"; // Planning, InProgress, Released
        public List<string> Checklist { get; set; } = new();
        public List<bool> ChecklistCompleted { get; set; } = new();
    }

    public static class DataService
    {
        private static readonly string FilePath = "tasks.json";
        private static readonly string MilestonesPath = "milestones.json";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// 加载任务列表（带错误处理）
        /// </summary>
        public static List<TaskItem> LoadTasks()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    LoggerService.Info("DataService", "任务文件不存在，创建默认数据");
                    return CreateDefaultTasks();
                }

                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LoggerService.Warning("DataService", "任务文件为空，创建默认数据");
                    return CreateDefaultTasks();
                }

                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, JsonOptions);
                if (tasks == null)
                {
                    LoggerService.Warning("DataService", "任务数据解析失败，创建默认数据");
                    return CreateDefaultTasks();
                }

                LoggerService.Info("DataService", $"成功加载 {tasks.Count} 个任务");
                return tasks;
            }
            catch (JsonException ex)
            {
                LoggerService.Error("DataService", $"JSON解析错误: {ex.Message}", ex);
                return CreateDefaultTasks();
            }
            catch (UnauthorizedAccessException ex)
            {
                LoggerService.Error("DataService", $"文件访问权限错误: {FilePath}", ex);
                return CreateDefaultTasks();
            }
            catch (IOException ex)
            {
                LoggerService.Error("DataService", $"文件IO错误: {ex.Message}", ex);
                return CreateDefaultTasks();
            }
            catch (Exception ex)
            {
                LoggerService.Fatal("DataService", $"加载任务时发生未知错误: {ex.Message}", ex);
                return CreateDefaultTasks();
            }
        }

        /// <summary>
        /// 保存任务列表（带错误处理）
        /// </summary>
        public static void SaveTasks(List<TaskItem> tasks)
        {
            if (tasks == null)
            {
                LoggerService.Warning("DataService", "尝试保存空任务列表");
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(tasks, JsonOptions);
                File.WriteAllText(FilePath, json);
                LoggerService.Debug("DataService", $"成功保存 {tasks.Count} 个任务");
            }
            catch (UnauthorizedAccessException ex)
            {
                LoggerService.Error("DataService", $"文件写入权限错误: {FilePath}", ex);
                throw new DataServiceException("无法写入任务文件，请检查文件权限", FilePath, ex);
            }
            catch (IOException ex)
            {
                LoggerService.Error("DataService", $"文件写入IO错误: {ex.Message}", ex);
                throw new DataServiceException("写入任务文件时发生IO错误", FilePath, ex);
            }
            catch (Exception ex)
            {
                LoggerService.Fatal("DataService", $"保存任务时发生未知错误: {ex.Message}", ex);
                throw new DataServiceException("保存任务时发生未知错误", FilePath, ex);
            }
        }

        /// <summary>
        /// 加载里程碑列表（带错误处理）
        /// </summary>
        public static List<Milestone> LoadMilestones()
        {
            try
            {
                if (!File.Exists(MilestonesPath))
                {
                    LoggerService.Info("DataService", "里程碑文件不存在，创建默认数据");
                    return CreateDefaultMilestones();
                }

                string json = File.ReadAllText(MilestonesPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LoggerService.Warning("DataService", "里程碑文件为空，创建默认数据");
                    return CreateDefaultMilestones();
                }

                var milestones = JsonSerializer.Deserialize<List<Milestone>>(json, JsonOptions);
                if (milestones == null)
                {
                    LoggerService.Warning("DataService", "里程碑数据解析失败，创建默认数据");
                    return CreateDefaultMilestones();
                }

                LoggerService.Info("DataService", $"成功加载 {milestones.Count} 个里程碑");
                return milestones;
            }
            catch (JsonException ex)
            {
                LoggerService.Error("DataService", $"里程碑JSON解析错误: {ex.Message}", ex);
                return CreateDefaultMilestones();
            }
            catch (Exception ex)
            {
                LoggerService.Fatal("DataService", $"加载里程碑时发生未知错误: {ex.Message}", ex);
                return CreateDefaultMilestones();
            }
        }

        /// <summary>
        /// 保存里程碑列表（带错误处理）
        /// </summary>
        public static void SaveMilestones(List<Milestone> milestones)
        {
            if (milestones == null)
            {
                LoggerService.Warning("DataService", "尝试保存空里程碑列表");
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(milestones, JsonOptions);
                File.WriteAllText(MilestonesPath, json);
                LoggerService.Debug("DataService", $"成功保存 {milestones.Count} 个里程碑");
            }
            catch (Exception ex)
            {
                LoggerService.Error("DataService", $"保存里程碑失败: {ex.Message}", ex);
                throw new DataServiceException("保存里程碑失败", MilestonesPath, ex);
            }
        }

        private static readonly string ReleasesPath = "releases.json";

        /// <summary>
        /// 加载发布信息列表（带错误处理）
        /// </summary>
        public static List<ReleaseInfo> LoadReleases()
        {
            try
            {
                if (!File.Exists(ReleasesPath))
                {
                    LoggerService.Info("DataService", "发布信息文件不存在");
                    return new List<ReleaseInfo>();
                }

                string json = File.ReadAllText(ReleasesPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    LoggerService.Warning("DataService", "发布信息文件为空");
                    return new List<ReleaseInfo>();
                }

                var releases = JsonSerializer.Deserialize<List<ReleaseInfo>>(json, JsonOptions);
                LoggerService.Info("DataService", $"成功加载 {releases?.Count ?? 0} 个发布信息");
                return releases ?? new List<ReleaseInfo>();
            }
            catch (JsonException ex)
            {
                LoggerService.Error("DataService", $"发布信息JSON解析错误: {ex.Message}", ex);
                return new List<ReleaseInfo>();
            }
            catch (Exception ex)
            {
                LoggerService.Fatal("DataService", $"加载发布信息时发生未知错误: {ex.Message}", ex);
                return new List<ReleaseInfo>();
            }
        }

        /// <summary>
        /// 保存发布信息列表（带错误处理）
        /// </summary>
        public static void SaveReleases(List<ReleaseInfo> releases)
        {
            if (releases == null)
            {
                LoggerService.Warning("DataService", "尝试保存空发布信息列表");
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(releases, JsonOptions);
                File.WriteAllText(ReleasesPath, json);
                LoggerService.Debug("DataService", $"成功保存 {releases.Count} 个发布信息");
            }
            catch (Exception ex)
            {
                LoggerService.Error("DataService", $"保存发布信息失败: {ex.Message}", ex);
                throw new DataServiceException("保存发布信息失败", ReleasesPath, ex);
            }
        }

        /// <summary>
        /// 创建默认任务数据
        /// </summary>
        private static List<TaskItem> CreateDefaultTasks()
        {
            return new List<TaskItem> 
            {
                new TaskItem { Title = "设计主角 3D 模型", Category = "美术", Status = "Task Pool" },
                new TaskItem { Title = "实现核心战斗逻辑", Category = "程序", Status = "In Progress" }
            };
        }

        /// <summary>
        /// 创建默认里程碑数据
        /// </summary>
        private static List<Milestone> CreateDefaultMilestones()
        {
            return new List<Milestone> 
            {
                new Milestone { Title = "v1.0 核心原型", Version = "1.0.0", Description = "完成基础移动与战斗逻辑" }
            };
        }
    }
}
