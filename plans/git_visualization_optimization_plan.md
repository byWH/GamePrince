# Git 可视化统计优化与功能延展计划

> 为 GamePrince 提供更丰富的 Git 可视化统计分析功能

---

## 📊 当前功能现状

### 已实现的功能

| 功能 | 文件位置 | 说明 |
|------|----------|------|
| 提交历史显示 | `GitService.cs` / `MainWindow.xaml.cs` | 显示提交哈希、日期、作者、消息 |
| 分支信息 | `GitService.cs` | 获取本地和远程分支列表 |
| 活动热力图 | `GitService.cs` | 按日期显示提交频率 |
| 每日代码行数 | `GitService.cs` | 统计每日代码增删行数 |
| 文件类型分布 | `GitService.cs` | 统计项目中各类文件数量 |
| 版本对比 | `GitService.cs` | 分支/提交之间的差异对比 |
| 提交详情 | `GitService.cs` | 单个提交的文件变更列表 |

---

## 🎯 优化与延展方向

### 1. 贡献者统计分析

**功能描述**：统计各贡献者的提交贡献情况

**实现内容**：

- 按提交数量排序的贡献者排行榜
- 按代码行数（增删）统计贡献度
- 贡献者头像和邮箱域名统计
- 贡献时间分布（活跃时间段）

**新增方法**：

```csharp
public class GitContributor
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int CommitCount { get; set; }
    public int LinesAdded { get; set; }
    public int LinesDeleted { get; set; }
    public Dictionary<DayOfWeek, int> ActivityByDay { get; set; }  // 按星期分布
    public Dictionary<int, int> ActivityByHour { get; set; }      // 按小时分布
}

public static List<GitContributor> GetContributors(string projectPath)
public static Dictionary<string, int> GetContributorEmailDomains(string projectPath)
```

---

### 2. 提交时间分布可视化

**功能描述**：分析提交的时间规律

**实现内容**：

- 按小时分布的热力图（0-23点）
- 按星期分布的热力图（周一到周日）
- 按月份分布的年度趋势
- 最佳提交时间段建议

**新增方法**：

```csharp
public static Dictionary<int, int> GetCommitsByHour(string projectPath)
public static Dictionary<DayOfWeek, int> GetCommitsByDayOfWeek(string projectPath)
public static Dictionary<int, Dictionary<int, int>> GetCommitsByMonth(string projectPath)  // 月份-年份分布
```

---

### 3. 代码吞吐量趋势图

**功能描述**：展示代码行数随时间的变化趋势

**实现内容**：

- 周报视图：每周代码行数变化趋势
- 月报视图：每月代码行数变化趋势
- 移动平均线显示（7天/30天）
- 代码净增长曲线

**新增方法**：

```csharp
public static Dictionary<DateTime, (int Added, int Deleted)> GetWeeklyCodeLines(string projectPath)
public static Dictionary<DateTime, (int Added, int Deleted)> GetMonthlyCodeLines(string projectPath)
```

---

### 4. 文件/目录变更频率

**功能描述**：分析哪些文件和目录最活跃

**实现内容**：

- 最常修改的文件排行（Top 20）
- 最活跃的目录排行
- 文件变更类型统计（新增/修改/删除比例）
- 按文件类型统计变更频率

**新增方法**：

```csharp
public static Dictionary<string, int> GetMostModifiedFiles(string projectPath, int top = 20)
public static Dictionary<string, int> GetMostActiveDirectories(string projectPath, int top = 10)
public static Dictionary<string, (int Added, int Modified, int Deleted)> GetChangesByFileType(string projectPath)
```

---

### 5. 提交消息分析

**功能描述**：统计分析提交消息的特征

**实现内容**：

- 提交消息平均长度
- 常用提交消息关键词云
- 提交消息类型统计（feat/fix/docs/refactor等）
- 未规范提交消息提醒

**新增方法**：

```csharp
public static (double AvgLength, int MaxLength, int MinLength) GetCommitMessageStats(string projectPath)
public static Dictionary<string, int> GetCommitMessageKeywords(string projectPath, int top = 20)
public static Dictionary<string, int> GetCommitTypes(string projectPath)
```

---

### 6. 项目里程碑可视化

**功能描述**：基于Git标签的项目里程碑展示

**实现内容**：

- 版本标签（Tag）列表显示
- 标签对应提交的信息
- 版本间差异统计
- 项目发布历史时间线

**新增方法**：

```csharp
public class GitTag
{
    public string Name { get; set; } = "";
    public string CommitHash { get; set; } = "";
    public DateTime? Date { get; set; }
    public string Message { get; set; } = "";
}

public static List<GitTag> GetTags(string projectPath)
public static GitCommitDetail GetTagDetail(string projectPath, string tagName)
```

---

### 7. 项目健康度指标

**功能描述**：综合评估项目开发健康状况

**实现内容**：

- 活跃度得分（基于提交频率）
- 代码增长趋势指标
- 开发者协作度指标
- 问题修复效率指标

**新增方法**：

```csharp
public class ProjectHealthMetrics
{
    public double ActivityScore { get; set; }           // 0-100 活跃度得分
    public double GrowthScore { get; set; }              // 0-100 增长得分
    public double CollaborationScore { get; set; }       // 0-100 协作得分
    public double ConsistencyScore { get; set; }         // 0-100 一致性得分
    public string HealthLevel { get; set; }               // "健康"/"一般"/"需关注"
}

public static ProjectHealthMetrics GetProjectHealth(string projectPath)
```

---

### 8. 高级搜索和过滤

**功能描述**：增强提交历史的搜索能力

**实现内容**：

- 按日期范围过滤
- 按作者过滤
- 按关键词搜索
- 按文件路径过滤
- 组合条件搜索

**UI改进**：

- 添加高级搜索面板
- 保存搜索条件
- 搜索结果高亮

---

## 🗂️ 功能模块规划

### 模块一：贡献者分析（新增视图）

```
贡献者分析
├── 贡献者排行榜（按提交数）
├── 贡献者排行榜（按代码量）
├── 贡献者时间分布
└── 贡献者详情弹窗
```

### 模块二：时间分析（扩展热力图）

```
时间分析
├── 提交时间热力图（小时/星期）
├── 代码吞吐量趋势
├── 活动周期分析
└── 最佳开发时段建议
```

### 模块三：文件分析（新增统计）

```
文件分析
├── 最活跃文件排行
├── 最活跃目录排行
├── 文件类型变更统计
└── 文件变更详情
```

### 模块四：提交分析（扩展提交历史）

```
提交分析
├── 提交消息统计
├── 提交类型分布
├── 关键词云
└── 未规范消息提醒
```

### 模块五：项目健康（新增仪表盘）

```
项目健康仪表盘
├── 活跃度指标
├── 增长趋势
├── 协作指数
└── 综合健康评分
```

---

## 📐 技术架构设计

### 新增数据模型

```csharp
// Models/GitModels.cs (新增文件)

namespace GamePrince
{
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

    public class GitTag
    {
        public string Name { get; set; } = "";
        public string CommitHash { get; set; } = "";
        public DateTime? Date { get; set; }
        public string Message { get; set; } = "";
    }

    public class ProjectHealthMetrics
    {
        public double ActivityScore { get; set; }
        public double GrowthScore { get; set; }
        public double CollaborationScore { get; set; }
        public double ConsistencyScore { get; set; }
        public string HealthLevel { get; set; } = "";
    }
}
```

### GitService 扩展方法

所有新方法添加到 `GitService.cs`，遵循现有缓存机制：

```csharp
// 贡献者分析
GetContributors()
GetContributorEmailDomains()

// 时间分析
GetCommitsByHour()
GetCommitsByDayOfWeek()
GetCommitsByMonth()
GetWeeklyCodeLines()
GetMonthlyCodeLines()

// 文件分析
GetMostModifiedFiles()
GetMostActiveDirectories()
GetChangesByFileType()

// 提交分析
GetCommitMessageStats()
GetCommitMessageKeywords()
GetCommitTypes()

// 标签管理
GetTags()
GetTagDetail()

// 项目健康
GetProjectHealth()
```

### UI 导航扩展

在主窗口侧边栏添加新导航按钮：

```xml
<!-- MainWindow.xaml 新增 -->
<Button x:Name="NavContributors" Content="👥 贡献者" Style="{StaticResource NavButtonStyle}" Click="ShowContributors"/>
<Button x:Name="NavTimeAnalysis" Content="⏰ 时间分析" Style="{StaticResource NavButtonStyle}" Click="ShowTimeAnalysis"/>
<Button x:Name="NavFileAnalysis" Content="📄 文件分析" Style="{StaticResource NavButtonStyle}" Click="ShowFileAnalysis"/>
<Button x:Name="NavProjectHealth" Content="💚 项目健康" Style="{StaticResource NavButtonStyle}" Click="ShowProjectHealth"/>
```

---

## 📋 实施步骤

### 第一阶段：核心数据方法

- [ ] 添加 `GitContributor`、`GitTag`、`ProjectHealthMetrics` 数据模型
- [ ] 实现 `GetContributors()` 方法
- [ ] 实现 `GetCommitsByHour()` 和 `GetCommitsByDayOfWeek()` 方法
- [ ] 实现 `GetMostModifiedFiles()` 方法
- [ ] 实现 `GetTags()` 方法
- [ ] 实现 `GetProjectHealth()` 方法

### 第二阶段：UI视图开发

- [ ] 创建贡献者分析视图
- [ ] 创建时间分析视图
- [ ] 创建文件分析视图
- [ ] 创建项目健康仪表盘视图

### 第三阶段：现有功能优化

- [ ] 优化热力图渲染性能
- [ ] 优化大数据量下的提交历史加载速度
- [ ] 添加数据刷新按钮和自动刷新

### 第四阶段：高级搜索

- [ ] 实现高级搜索面板
- [ ] 实现多条件组合过滤
- [ ] 添加搜索结果高亮

---

## 🎨 UI 设计建议

### 贡献者排行榜

```
┌─────────────────────────────────────────────────────────┐
│  👥 贡献者排行                                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  🥇 张三                          128 次提交  +3,420 行 │
│     📧 zhangsan@example.com                          │
│     📊 活跃时间: 上午 9:00-11:00                      │
│                                                         │
│  🥈 李四                           89 次提交  +2,156 行 │
│     📧 lisi@example.com                               │
│     📊 活跃时间: 下午 2:00-5:00                        │
│                                                         │
│  🥉 王五                           67 次提交  +1,890 行 │
│     📧 wangwu@example.com                            │
│     📊 活跃时间: 晚上 8:00-10:00                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 项目健康仪表盘

```
┌─────────────────────────────────────────────────────────┐
│  💚 项目健康                                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│    综合健康度: ████████████░░░░░ 82/100 良好          │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                  │
│  │   活跃度     │  │   增长趋势     │                  │
│  │   85/100    │  │   78/100     │                  │
│  │    优秀     │  │    良好      │                  │
│  └──────────────┘  └──────────────┘                  │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐                  │
│  │   协作指数   │  │   一致性     │                  │
│  │   90/100    │  │   75/100     │                  │
│  │    优秀     │  │    良好      │                  │
│  └──────────────┘  └──────────────┘                  │
│                                                         │
│  📈 本周提交: 45 次 (+1,230 / -320 行)                │
│  📅 活跃开发者: 5 人                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## ⚠️ 技术注意事项

1. **缓存策略**：新方法同样需要实现缓存机制，避免频繁调用Git命令
2. **性能优化**：
   - 大量提交时使用 `--numstat` 批处理
   - UI渲染使用虚拟化列表
3. **错误处理**：所有Git命令调用需要完善的异常处理
4. **国际化**：保持中文界面，显示英文Git数据

---

## ✅ 验收标准

- [x] 新增方法能正确获取Git数据
- [x] UI视图符合Glassmorphism风格
- [x] 导航切换流畅无卡顿
- [x] 缓存机制正常工作
- [x] 手动测试通过
- [x] 不影响现有功能

---

## 📎 相关文件

| 文件 | 修改类型 | 说明 |
|------|----------|------|
| `GitService.cs` | 扩展 | 添加新的数据获取方法 |
| `Models/GitModels.cs` | 新增 | 新增数据模型类 |
| `MainWindow.xaml` | 扩展 | 添加新的导航和视图 |
| `MainWindow.xaml.cs` | 扩展 | 添加视图切换和数据绑定逻辑 |
| `LoggerService.cs` | 无变化 | 继续用于日志记录 |

---

## ✅ 已完成实现 (2026-03-18)

### Git 高级统计功能

| 功能 | 实现状态 | 说明 |
|------|----------|------|
| 贡献者统计 | ✅ 已完成 | 显示贡献者列表、提交数、代码行数、活跃时间 |
| Git 标签 | ✅ 已完成 | 显示标签列表、创建日期、对应提交 |
| 项目健康度 | ✅ 已完成 | 综合得分、活跃度、增长趋势、协作指数、一致性 |

### 新增视图

1. **贡献者视图** (`👥 贡献者`)
   - 贡献者排行榜（按提交数）
   - 代码增删行数统计
   - 活跃时间段分析

2. **标签视图** (`🏷️ 标签管理`)
   - Git 标签列表
   - 标签对应提交信息
   - 创建日期显示

3. **项目健康视图** (`💚 项目健康`)
   - 综合健康度评分
   - 四项指标：活跃度、增长趋势、协作指数、一致性
   - 本周统计：提交数、新增/删除行数

---

*文档版本: v1.1*  
*更新时间: 2026-03-18*
