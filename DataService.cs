using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace GamePrince
{
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

    public static class DataService
    {
        private static readonly string FilePath = "tasks.json";
        private static readonly string MilestonesPath = "milestones.json";

        public static List<TaskItem> LoadTasks()
        {
            if (!File.Exists(FilePath))
                return new List<TaskItem> {
                    new TaskItem { Title = "设计主角 3D 模型", Category = "美术", Status = "Task Pool" },
                    new TaskItem { Title = "实现核心战斗逻辑", Category = "程序", Status = "In Progress" }
                };

            string json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }

        public static void SaveTasks(List<TaskItem> tasks)
        {
            string json = JsonSerializer.Serialize(tasks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }

        public static List<Milestone> LoadMilestones()
        {
            if (!File.Exists(MilestonesPath))
                return new List<Milestone> {
                    new Milestone { Title = "v1.0 核心原型", Version = "1.0.0", Description = "完成基础移动与战斗逻辑" }
                };

            string json = File.ReadAllText(MilestonesPath);
            return JsonSerializer.Deserialize<List<Milestone>>(json) ?? new List<Milestone>();
        }

        public static void SaveMilestones(List<Milestone> milestones)
        {
            string json = JsonSerializer.Serialize(milestones, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(MilestonesPath, json);
        }
    }
}
