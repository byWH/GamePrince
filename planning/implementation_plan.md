# GamePrince 开发计划

> 基于核心总目标的完整实施路线图

## ⚠️ 核心原则

**GamePrince 是一款纯观察与分析工具**

- ✅ 软件所有功能**仅读取** Godot 项目数据
- ✅ **禁止直接修改** Godot 项目的任何文件或配置
- ✅ 软件仅充当**观察者**、**分析者**、**局外管理者**的角色
- ✅ 仅管理独立于 Godot 项目的辅助数据（任务看板、时间追踪等）

## 项目愿景

**GamePrince** 是一款 **Godot 游戏开发项目管理辅助软件**，目标是成为全方位一体的 **个人开发者专用 Godot 游戏项目辅助工具库**。

---

## 阶段一：基础功能完善 ✅ (已完成)

### 已完成功能

| 功能 | 状态 | 文件 |
|------|------|------|
| Git 仓库选择 | ✅ | `MainWindow.xaml.cs` |
| 提交历史显示 | ✅ | `GitService.cs` |
| 文件类型分布统计 | ✅ | `GitService.cs` |
| 分支信息显示 | ✅ | `GitService.cs` |
| Glassmorphism UI | ✅ | `MainWindow.xaml` |
| Kanban 任务看板 | ✅ | `MainWindow.xaml.cs` |
| 任务增删改查 | ✅ | `TaskEditDialog.xaml` |
| 任务搜索过滤 | ✅ | `MainWindow.xaml.cs` |
| 活跃热力图 | ✅ | `MainWindow.xaml.cs` |

---

## 阶段二：Godot 项目深度集成 ✅ (已完成)

### 目标
识别和解析 Godot 项目结构，提供针对性的项目管理功能（仅读取，不修改）。

### 实现任务

#### 2.1 Godot 项目识别
- [ ] **GodotProjectDetector** - 检测目录是否为 Godot 项目（只读检测）
  - 识别 `project.godot` 文件
  - 解析项目名称和配置
  
- [ ] **项目配置读取**（仅读取）
  - 读取 `project.godot` 内容
  - 解析 Godot 版本信息
  - 显示项目设置概览

#### 2.2 Godot 文件类型支持
- [ ] **扩展支持** - 识别更多 Godot 特有文件类型（仅统计）
  - `.gd` - GDScript 脚本
  - `.tscn` - 场景文件
  - `.tres` - 资源文件
  - `.gdnlib` - GDExtension 库
  - `.gdshader` - 着色器文件

#### 2.3 项目结构树
- [ ] **资源浏览器** - 展示项目目录结构（仅查看）
  - 树形视图显示所有文件
  - 按类型分组（脚本、场景、纹理、音频等）
  - 快速定位文件（只读查看）

---

## 阶段三：资源分析 📦

### 目标
提供游戏资源分析视图，仅分析和统计资源信息，不修改任何资源文件。

### 实现任务

#### 3.1 资源分类视图
- [ ] **ResourceCategorizer** - 自动分析项目资源（仅分析）
  - 纹理 (Textures)
  - 音频 (Audio)
  - 场景 (Scenes)
  - 脚本 (Scripts)
  - 预设 (Prefabs)

#### 3.2 资源统计面板
- [ ] **资源使用分析**（仅统计）
  - 各类资源数量统计
  - 资源大小统计
  - 未使用资源检测（仅检测，不删除）

---

## 阶段四：开发效率工具 ⚡

### 目标
提供更多实用的小工具，提升开发效率。

### 实现任务

#### 4.1 任务增强
- [ ] **Todo 列表增强**
  - 与 Git 提交关联的任务标记
  - 里程碑 (Milestone) 管理
  - 任务依赖关系

#### 4.2 版本对比
- [ ] **GitDiffViewer** - 可视化版本对比
  - 分支对比视图
  - 提交详情查看
  - 文件变更列表

#### 4.3 时间追踪
- [ ] **TimeTracker** - 开发时间统计
  - 按任务记录开发时间
  - 每日/每周开发时长统计
  - 时间日志导出

---

## 阶段五：发布与导出 📤

### 目标
管理游戏版本发布计划（独立数据），查看导出配置（仅读取）。

### 实现任务

#### 5.1 发布管理
- [ ] **ReleaseManager** - 版本发布计划追踪（独立数据）
  - 版本号管理 (语义化版本)
  - 发布计划时间线
  - 发布检查清单

#### 5.2 导出配置查看
- [ ] **ExportPresetViewer** - 查看 Godot 导出配置（仅读取）
  - 预设配置查看
  - 多平台配置查看（Windows/macOS/Linux/Android/iOS）
  - 导出日志查看

---

## 阶段六：生态集成 🔗 ✅ (已完成)

### 目标
与 Godot 生态更好地集成（仅读取信息，不修改任何内容）。

### 实现任务

#### 6.1 插件信息查看
- [x] **PluginViewer** - 查看项目插件信息（仅读取）
  - 插件列表显示
  - 插件版本信息
  - 插件路径查看

#### 6.2 Godot Editor 集成
- [x] **EditorShortcuts** - 常用编辑器快捷方式（仅启动）
  - 一键打开 Godot Editor（仅启动，不操作 Editor）
  - 快速运行游戏（通过命令行）
  - 场景快速预览（通过命令行）

---

## 技术架构

```
GamePrince/
├── App.xaml.cs                    # 应用程序入口
├── Services/                      # 业务服务层
│   ├── GitService.cs              # Git 操作服务
│   ├── DataService.cs             # 数据持久化服务
│   ├── GodotProjectService.cs     # Godot 项目服务 (新增)
│   └── TimeTrackingService.cs     # 时间追踪服务 (新增)
├── ViewModels/                     # MVVM 视图模型
│   ├── MainViewModel.cs
│   ├── KanbanViewModel.cs
│   └── ResourceViewModel.cs
├── Views/                          # 视图层
│   ├── MainWindow.xaml
│   ├── TaskEditDialog.xaml
│   ├── ResourceBrowser.xaml       # 资源浏览器 (新增)
│   └── ReleaseManager.xaml        # 发布管理 (新增)
├── Models/                         # 数据模型
│   ├── TaskItem.cs
│   ├── GitCommit.cs
│   ├── GodotProject.cs            # Godot 项目模型
│   └── ReleaseInfo.cs             # 发布信息模型
└── Helpers/                        # 辅助工具
    ├── GodotFileDetector.cs       # Godot 文件检测
    └── JsonHelper.cs
```

---

## 开发优先级

| 优先级 | 功能 | 预计迭代 |
|--------|------|----------|
| P0 | Godot 项目识别 | v1.1 |
| P0 | 资源分类视图 | v1.1 |
| P1 | 任务增强 | v1.2 |
| P1 | 版本对比 | v1.2 |
| P2 | 时间追踪 | v1.3 |
| P2 | 发布管理 | v1.3 |
| P3 | 插件管理 | v1.4 |

---

## 验证标准

每个功能发布需要满足：
1. ✅ 功能正常运行无崩溃
2. ✅ UI 符合 Glassmorphism 设计风格
3. ✅ **严格遵守只读原则** - 不修改 Godot 项目任何文件
4. ✅ 数据正确持久化到 JSON 文件（独立数据）
5. ✅ 手动测试通过
6. ✅ 代码符合 C# 编码规范
