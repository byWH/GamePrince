using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GamePrince
{
    public partial class MainWindow : Window
    {
        private List<TaskItem> _tasks = new();
        private List<Milestone> _milestones = new();
        private List<ReleaseInfo> _releases = new();
        private string _currentProjectPath = "";
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private DateTime _calendarCurrentMonth = DateTime.Now.Date;

        public MainWindow()
        {
            InitializeComponent();
            _tasks = DataService.LoadTasks();
            _milestones = DataService.LoadMilestones();
            _releases = DataService.LoadReleases();
            UpdateMilestoneFilter();
            UpdateKanban();
            PopulateHeatmap();

            // Set default view to Project Overview
            UpdateNavButtons("ProjectOverview");
            ShowProjectOverview(new object(), new RoutedEventArgs());

            _uiTimer = new System.Windows.Threading.DispatcherTimer();
            _uiTimer.Interval = TimeSpan.FromSeconds(1);
            _uiTimer.Tick += (s, e) =>
            {
                if (_tasks.Any(t => t.LastTimerStart != null))
                    UpdateKanban();
            };
            _uiTimer.Start();
        }

        private void SelectProject_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "选择游戏项目目录",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                _currentProjectPath = dialog.FolderName;
                ProjectNameText.Text = System.IO.Path.GetFileName(_currentProjectPath);
                ReloadProjectData();
            }
        }

        private void ReloadProjectData()
        {
            if (string.IsNullOrEmpty(_currentProjectPath)) return;

            var commits = GitService.GetCommitHistory(_currentProjectPath);
            var totalCommits = GitService.GetTotalCommits(_currentProjectPath);
            var fileDist = GitService.GetFileTypeDistribution(_currentProjectPath);
            var branches = GitService.GetBranches(_currentProjectPath);
            int totalFiles = fileDist.Values.Sum();

            var topExtensions = fileDist.OrderByDescending(kv => kv.Value)
                .Take(3)
                .Select(kv => $"{kv.Key}: {kv.Value}");

            StatsText.Text = $"提交总数: {totalCommits} | 文件总数: {totalFiles} ({string.Join(", ", topExtensions)})";

            // 显示当前分支
            var currentBranch = branches.FirstOrDefault(b => b.IsCurrent);
            if (currentBranch != null)
            {
                BranchText.Text = $"分支: {currentBranch.Name} ({branches.Count}个分支)";
            }
            else if (branches.Count > 0)
            {
                BranchText.Text = $"分支: {branches[0].Name} ({branches.Count}个分支)";
            }
            else
            {
                BranchText.Text = "分支: -";
            }

            PopulateHeatmap();
        }

        private void ShowKanban(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "任务看板";
            KanbanView.Visibility = Visibility.Visible;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("Kanban");
            UpdateKanban();
        }

        private void ShowProjectOverview(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "项目概览";
            ProjectOverviewView.Visibility = Visibility.Visible;
            KanbanView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("ProjectOverview");
            UpdateProjectOverview();
        }

        private void UpdateNavButtons(string activeButton)
        {
            NavProjectOverview.Tag = activeButton == "ProjectOverview" ? "Active" : null;
            NavTasks.Tag = activeButton == "Tasks" ? "Active" : null;
            NavPlan.Tag = activeButton == "Plan" ? "Active" : null;
            NavHeatmap.Tag = activeButton == "Heatmap" ? "Active" : null;
            NavResources.Tag = activeButton == "Resources" ? "Active" : null;
            NavCalendar.Tag = activeButton == "Calendar" ? "Active" : null;
            NavWeeklyReport.Tag = activeButton == "WeeklyReport" ? "Active" : null;
            NavGitDiff.Tag = activeButton == "GitDiff" ? "Active" : null;
            NavReleases.Tag = activeButton == "Releases" ? "Active" : null;
            NavPlugins.Tag = activeButton == "Plugins" ? "Active" : null;
        }

        private void UpdateProjectOverview()
        {
            OverviewTotalTasks.Text = _tasks.Count.ToString();
            OverviewInProgress.Text = _tasks.Count(t => t.Status == "In Progress").ToString();
            OverviewCompleted.Text = _tasks.Count(t => t.Status == "Completed").ToString();
            OverviewMilestones.Text = _milestones.Count.ToString();

            OverviewProjectName.Text = string.IsNullOrEmpty(_currentProjectPath) ? "未选择项目" : System.IO.Path.GetFileName(_currentProjectPath);
            OverviewProjectPath.Text = _currentProjectPath;

            if (!string.IsNullOrEmpty(_currentProjectPath))
            {
                var totalCommits = GitService.GetTotalCommits(_currentProjectPath);
                var branches = GitService.GetBranches(_currentProjectPath);
                var currentBranch = branches.FirstOrDefault(b => b.IsCurrent);
                OverviewGitStats.Text = $"Git提交: {totalCommits} | 分支: {currentBranch?.Name ?? "-"}";
            }
            else
            {
                OverviewGitStats.Text = "Git提交: 0 | 分支: -";
            }

            // Recent tasks
            OverviewRecentTasks.Children.Clear();
            var recentTasks = _tasks.OrderByDescending(t => t.DateCreated).Take(5).ToList();
            if (recentTasks.Count == 0)
            {
                OverviewRecentTasks.Children.Add(new TextBlock { Text = "暂无任务", Foreground = Brushes.Gray, FontSize = 14 });
            }
            else
            {
                foreach (var task in recentTasks)
                {
                    var statusColor = task.Status == "Completed" ? Brushes.Green : (task.Status == "In Progress" ? Brushes.Orange : Brushes.Gray);
                    var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                    row.Children.Add(new TextBlock { Text = task.Title, Foreground = Brushes.White, FontSize = 14, Width = 250, TextTrimming = TextTrimming.CharacterEllipsis });
                    row.Children.Add(new TextBlock { Text = task.Status switch { "Task Pool" => "📋 任务池", "In Progress" => "🔄 进行中", "Completed" => "✅ 完成", _ => task.Status }, Foreground = statusColor, FontSize = 12 });
                    OverviewRecentTasks.Children.Add(row);
                }
            }

            // Upcoming milestones
            OverviewUpcomingMilestones.Children.Clear();
            var upcomingMilestones = _milestones.Where(m => !m.IsCompleted && !string.IsNullOrEmpty(m.TargetDate) && DateTime.TryParse(m.TargetDate, out DateTime target) && target >= DateTime.Now).OrderBy(m => DateTime.Parse(m.TargetDate)).Take(3).ToList();
            if (upcomingMilestones.Count == 0)
            {
                OverviewUpcomingMilestones.Children.Add(new TextBlock { Text = "暂无即将到期的里程碑", Foreground = Brushes.Gray, FontSize = 14 });
            }
            else
            {
                foreach (var milestone in upcomingMilestones)
                {
                    var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                    row.Children.Add(new TextBlock { Text = milestone.Title, Foreground = Brushes.White, FontSize = 14, Width = 200, TextTrimming = TextTrimming.CharacterEllipsis });
                    row.Children.Add(new TextBlock { Text = milestone.TargetDate, Foreground = Brushes.Cyan, FontSize = 12 });
                    OverviewUpcomingMilestones.Children.Add(row);
                }
            }
        }

        private void ShowPlan(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "开发计划";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Visible;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("Plan");
            UpdateMilestoneView();
        }

        private void ShowTaskManagement(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "任务管理";
            TaskManagementView.Visibility = Visibility.Visible;
            TaskViewSwitcher.Visibility = Visibility.Visible;

            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;

            UpdateNavButtons("Tasks");

            // Default to Kanban view every time we enter
            KanbanViewButton.IsChecked = true;
            SwitchToKanbanView(this, new RoutedEventArgs());
        }

        private void SwitchToKanbanView(object sender, RoutedEventArgs e)
        {
            KanbanView.Visibility = Visibility.Visible;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            UpdateKanban();
        }

        private void SwitchToListView(object sender, RoutedEventArgs e)
        {
            KanbanView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Visible;
            UpdateListView();
        }

        private void ShowHeatmap(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "活跃热力图";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Visible;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("Heatmap");
            PopulateHeatmap();
        }



        private void ShowCalendar(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "日历视图";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Visible;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("Calendar");
            UpdateCalendarView();
        }

        private void ShowWeeklyReport(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "工时周报";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Visible;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("WeeklyReport");
            UpdateWeeklyReport();
        }

        private void ShowGitDiff(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "版本对比";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Visible;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("GitDiff");
            UpdateGitDiffView();
        }

        private void UpdateGitDiffView()
        {
            if (string.IsNullOrEmpty(_currentProjectPath)) return;
            
            // Load branches
            var branches = GitService.GetBranches(_currentProjectPath);
            var branchNames = branches.Select(b => b.Name).ToList();
            
            FromBranchCombo.ItemsSource = branchNames;
            ToBranchCombo.ItemsSource = branchNames;
            
            if (branchNames.Count > 0)
            {
                FromBranchCombo.SelectedIndex = 0;
                ToBranchCombo.SelectedIndex = branchNames.Count > 1 ? 1 : 0;
            }
            
            // Update branch list
            BranchListContainer.Children.Clear();
            foreach (var branch in branches)
            {
                var branchBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(10, 8, 10, 8),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                
                var branchStack = new StackPanel { Orientation = Orientation.Horizontal };
                branchStack.Children.Add(new TextBlock 
                { 
                    Text = branch.IsCurrent ? "✓ " : "  ", 
                    Foreground = branch.IsCurrent ? Brushes.Green : Brushes.Transparent, 
                    FontSize = 12 
                });
                branchStack.Children.Add(new TextBlock 
                { 
                    Text = branch.IsCurrent ? $"🌿 {branch.Name}" : branch.Name, 
                    Foreground = branch.IsCurrent ? Brushes.Cyan : Brushes.White, 
                    FontSize = 13 
                });
                branchBorder.Child = branchStack;
                
                // Click to select
                branchBorder.MouseLeftButtonDown += (s, ev) =>
                {
                    if (ToBranchCombo.SelectedItem?.ToString() != branch.Name)
                        ToBranchCombo.SelectedItem = branch.Name;
                    else
                        FromBranchCombo.SelectedItem = branch.Name;
                };
                
                BranchListContainer.Children.Add(branchBorder);
            }
            
            // Also update commit history
            UpdateCommitHistoryView();
        }
        
        private void UpdateCommitHistoryView()
        {
            CommitHistoryContainer.Children.Clear();
            
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                CommitHistoryContainer.Children.Add(new TextBlock 
                { 
                    Text = "请先选择项目目录", 
                    Foreground = Brushes.Gray, 
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }
            
            string searchText = CommitSearchBox?.Text?.ToLower() ?? "";
            var commits = GitService.GetCommitHistory(_currentProjectPath, 100);
            
            // Filter by search text
            if (!string.IsNullOrEmpty(searchText))
            {
                commits = commits.Where(c => 
                    c.Message.ToLower().Contains(searchText) ||
                    c.Author.ToLower().Contains(searchText) ||
                    c.Hash.ToLower().Contains(searchText)
                ).ToList();
            }
            
            if (commits.Count == 0)
            {
                CommitHistoryContainer.Children.Add(new TextBlock 
                { 
                    Text = string.IsNullOrEmpty(searchText) ? "暂无提交记录" : "没有找到匹配的提交", 
                    Foreground = Brushes.Gray, 
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }
            
            foreach (var commit in commits)
            {
                var commitBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                
                var commitStack = new StackPanel();
                
                // Hash and date row
                var headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
                headerRow.Children.Add(new TextBlock 
                { 
                    Text = commit.Hash, 
                    Foreground = Brushes.Cyan, 
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas")
                });
                headerRow.Children.Add(new TextBlock 
                { 
                    Text = commit.Date.ToString("yyyy-MM-dd HH:mm"), 
                    Foreground = Brushes.Gray, 
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Right
                });
                commitStack.Children.Add(headerRow);
                
                // Message
                commitStack.Children.Add(new TextBlock 
                { 
                    Text = commit.Message, 
                    Foreground = Brushes.White, 
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 5)
                });
                
                // Author
                commitStack.Children.Add(new TextBlock 
                { 
                    Text = $"👤 {commit.Author}", 
                    Foreground = Brushes.Gray, 
                    FontSize = 11
                });
                
                commitBorder.Child = commitStack;
                
                // Click to show detail
                commitBorder.MouseLeftButtonDown += (s, ev) => ShowCommitDetail(commit.Hash);
                
                CommitHistoryContainer.Children.Add(commitBorder);
            }
        }
        
        private void SearchCommits(object sender, RoutedEventArgs e)
        {
            UpdateCommitHistoryView();
        }
        
        private void ShowCommitDetail(string commitHash)
        {
            if (string.IsNullOrEmpty(_currentProjectPath)) return;
            
            // Hide diff panel, show commit detail panel
            DiffResultsPanel.Visibility = Visibility.Collapsed;
            CommitDetailPanel.Visibility = Visibility.Visible;
            
            var detail = GitService.GetCommitDetail(_currentProjectPath, commitHash);
            
            // Clear previous content
            CommitInfoContainer.Children.Clear();
            CommitFilesContainer.Children.Clear();
            
            // Commit info header
            var infoBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e3a5f")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var infoStack = new StackPanel();
            
            // Full hash
            infoStack.Children.Add(new TextBlock 
            { 
                Text = detail.FullHash, 
                Foreground = Brushes.Cyan, 
                FontSize = 12,
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            // Message
            infoStack.Children.Add(new TextBlock 
            { 
                Text = detail.Message, 
                Foreground = Brushes.White, 
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });
            
            // Author and date
            var metaStack = new StackPanel { Orientation = Orientation.Horizontal };
            metaStack.Children.Add(new TextBlock 
            { 
                Text = $"👤 {detail.Author}", 
                Foreground = Brushes.Gray, 
                FontSize = 12,
                Margin = new Thickness(0, 0, 20, 0)
            });
            metaStack.Children.Add(new TextBlock 
            { 
                Text = $"📅 {detail.Date:yyyy-MM-dd HH:mm:ss}", 
                Foreground = Brushes.Gray, 
                FontSize = 12
            });
            infoStack.Children.Add(metaStack);
            
            infoBorder.Child = infoStack;
            CommitInfoContainer.Children.Add(infoBorder);
            
            // Files changed
            if (detail.Changes.Count == 0)
            {
                CommitFilesContainer.Children.Add(new TextBlock 
                { 
                    Text = "此提交没有文件变更", 
                    Foreground = Brushes.Gray, 
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
            }
            else
            {
                foreach (var change in detail.Changes)
                {
                    var changeBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    
                    var changeGrid = new Grid();
                    changeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
                    changeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    
                    // Change type icon
                    string icon = change.ChangeType switch
                    {
                        "Added" => "➕",
                        "Modified" => "✏️",
                        "Deleted" => "🗑️",
                        "Renamed" => "📝",
                        _ => "📄"
                    };
                    var iconColor = change.ChangeType switch
                    {
                        "Added" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                        "Modified" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                        "Deleted" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                        _ => Brushes.Gray
                    };
                    
                    changeGrid.Children.Add(new TextBlock { Text = icon, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
                    
                    var fileText = new TextBlock 
                    { 
                        Text = change.FilePath, 
                        Foreground = Brushes.White, 
                        FontSize = 13,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    Grid.SetColumn(fileText, 1);
                    changeGrid.Children.Add(fileText);
                    
                    changeBorder.Child = changeGrid;
                    CommitFilesContainer.Children.Add(changeBorder);
                }
            }
        }
        
        private void BackToDiffList(object sender, RoutedEventArgs e)
        {
            CommitDetailPanel.Visibility = Visibility.Collapsed;
            DiffResultsPanel.Visibility = Visibility.Visible;
        }

        private void RefreshGitDiff(object sender, RoutedEventArgs e)
        {
            UpdateGitDiffView();
        }

        private void CompareCommits(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProjectPath)) return;
            
            string fromRef = FromBranchCombo.SelectedItem?.ToString() ?? "";
            string toRef = ToBranchCombo.SelectedItem?.ToString() ?? "";
            
            if (string.IsNullOrEmpty(fromRef) || string.IsNullOrEmpty(toRef)) return;
            
            var result = GitService.CompareRefs(_currentProjectPath, fromRef, toRef);
            
            // Update stats
            DiffStatsPanel.Visibility = Visibility.Visible;
            DiffAdditionsText.Text = $"+{result.TotalAdditions}";
            DiffDeletionsText.Text = $"-{result.TotalDeletions}";
            
            // Update results
            DiffResultsContainer.Children.Clear();
            
            if (result.Changes.Count == 0)
            {
                DiffResultsContainer.Children.Add(new TextBlock 
                { 
                    Text = "两个分支之间没有差异", 
                    Foreground = Brushes.Gray, 
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }
            
            foreach (var change in result.Changes)
            {
                var changeBorder = new Border
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(12, 8, 12, 8),
                    Margin = new Thickness(0, 0, 0, 5)
                };
                
                var changeGrid = new Grid();
                changeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
                changeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                changeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
                
                // Change type icon
                string icon = change.ChangeType switch
                {
                    "Added" => "➕",
                    "Modified" => "✏️",
                    "Deleted" => "🗑️",
                    "Renamed" => "📝",
                    _ => "📄"
                };
                var iconColor = change.ChangeType switch
                {
                    "Added" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                    "Modified" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                    "Deleted" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                    _ => Brushes.Gray
                };
                
                changeGrid.Children.Add(new TextBlock { Text = icon, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
                
                var fileText = new TextBlock 
                { 
                    Text = change.FilePath, 
                    Foreground = Brushes.White, 
                    FontSize = 13,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                Grid.SetColumn(fileText, 1);
                changeGrid.Children.Add(fileText);
                
                var statsText = new TextBlock
                {
                    Text = change.ChangeType == "Added" ? $"+{change.LinesAdded}" : 
                           change.ChangeType == "Deleted" ? $"-{change.LinesDeleted}" :
                           $"+{change.LinesAdded} -{change.LinesDeleted}",
                    Foreground = change.LinesAdded > 0 ? Brushes.Green : (change.LinesDeleted > 0 ? Brushes.Red : Brushes.Gray),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(statsText, 2);
                changeGrid.Children.Add(statsText);
                
                changeBorder.Child = changeGrid;
                DiffResultsContainer.Children.Add(changeBorder);
            }
        }

        private void ShowResources(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "资源浏览器";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Visible;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("Resources");
            UpdateResourceBrowser();
        }

        private void RefreshResourceTree(object sender, RoutedEventArgs e)
        {
            UpdateResourceBrowser();
        }

        private void UpdateResourceBrowser()
        {
            // Update project tree
            ProjectTreeContainer.Children.Clear();
            
            if (!string.IsNullOrEmpty(_currentProjectPath))
            {
                var rootNode = GodotProjectService.GetProjectTree(_currentProjectPath, 3);
                RenderProjectNode(rootNode, ProjectTreeContainer, 0);
            }
            else
            {
                var emptyText = new TextBlock { Text = "请先选择项目目录", Foreground = Brushes.Gray, FontSize = 14, Margin = new Thickness(10) };
                ProjectTreeContainer.Children.Add(emptyText);
            }
            
            // Update resource statistics
            UpdateResourceStats();
        }

        private void RenderProjectNode(ProjectNode node, StackPanel parent, int depth)
        {
            // Create toggle button for directory
            var nodeBorder = new Border
            {
                Margin = new Thickness(depth * 15, 2, 5, 2),
                Padding = new Thickness(5, 3, 5, 3),
                Background = Brushes.Transparent,
                CornerRadius = new CornerRadius(4),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            
            var nodeStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            // Icon based on type
            string icon = node.IsDirectory ? "📁" : GetFileTypeIcon(node.FileType);
            var iconText = new TextBlock { Text = icon, FontSize = 13, Margin = new Thickness(0, 0, 6, 0), VerticalAlignment = VerticalAlignment.Center };
            nodeStack.Children.Add(iconText);
            
            var nameText = new TextBlock { 
                Text = node.Name, 
                Foreground = node.IsDirectory ? Brushes.White : GetFileTypeBrush(node.FileType), 
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            nodeStack.Children.Add(nameText);
            
            nodeBorder.Child = nodeStack;
            
            // Add children container if directory
            StackPanel childrenContainer = null;
            if (node.IsDirectory && node.Children.Count > 0)
            {
                childrenContainer = new StackPanel { Margin = new Thickness(0, 0, 0, 0) };
                
                // Add expand/collapse functionality
                bool isExpanded = depth < 2; // Auto-expand first 2 levels
                nodeBorder.Tag = isExpanded;
                
                nodeBorder.MouseLeftButtonDown += (s, e) =>
                {
                    bool currentState = (bool)((Border)s).Tag;
                    ((Border)s).Tag = !currentState;
                    childrenContainer.Visibility = currentState ? Visibility.Collapsed : Visibility.Visible;
                };
                
                // Render children
                foreach (var child in node.Children)
                {
                    RenderProjectNode(child, childrenContainer, depth + 1);
                }
                
                nodeBorder.Tag = isExpanded;
                childrenContainer.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            }
            
            parent.Children.Add(nodeBorder);
            
            if (childrenContainer != null)
            {
                parent.Children.Add(childrenContainer);
            }
        }

        private string GetFileTypeIcon(string extension)
        {
            return extension.ToLower() switch
            {
                ".gd" => "📜",
                ".tscn" => "🎬",
                ".tres" => "📦",
                ".gdshader" or ".shader" => "🎨",
                ".png" or ".jpg" or ".jpeg" or ".webp" or ".svg" => "🖼️",
                ".ogg" or ".wav" or ".mp3" or ".flac" => "🎵",
                ".ttf" or ".otf" or ".woff" => "🔤",
                ".gdnlib" or ".gdext" => "🔌",
                ".txt" or ".md" => "📝",
                ".json" or ".yaml" or ".yml" => "📋",
                _ => "📄"
            };
        }

        private Brush GetFileTypeBrush(string extension)
        {
            return extension.ToLower() switch
            {
                ".gd" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06b6d4")),
                ".tscn" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                ".tres" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                ".gdshader" or ".shader" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ec4899")),
                ".png" or ".jpg" or ".jpeg" or ".webp" or ".svg" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                ".ogg" or ".wav" or ".mp3" or ".flac" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
                ".ttf" or ".otf" or ".woff" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f97316")),
                ".gdnlib" or ".gdext" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06b6d4")),
                _ => Brushes.LightGray
            };
        }

        private void UpdateResourceStats()
        {
            // Detect Godot project
            GodotProject godotProject = null;
            if (!string.IsNullOrEmpty(_currentProjectPath))
            {
                godotProject = GodotProjectService.DetectProject(_currentProjectPath);
            }
            
            // Update Godot project banner
            if (godotProject != null && godotProject.IsValid)
            {
                GodotProjectBanner.Visibility = Visibility.Visible;
                GodotProjectName.Text = $"🎮 {godotProject.Name}";
                GodotProjectPath.Text = godotProject.Path;
                GodotMainScene.Text = string.IsNullOrEmpty(godotProject.MainScene) ? "主场景: -" : $"主场景: {godotProject.MainScene}";
            }
            else
            {
                GodotProjectBanner.Visibility = Visibility.Collapsed;
            }
            
            // Get resource stats
            ResourceStats stats = new ResourceStats();
            if (!string.IsNullOrEmpty(_currentProjectPath))
            {
                stats = GodotProjectService.GetResourceStats(_currentProjectPath);
            }
            
            // Update stat cards
            StatScripts.Text = stats.Scripts.ToString();
            StatScenes.Text = stats.Scenes.ToString();
            StatTextures.Text = stats.Textures.ToString();
            StatAudio.Text = stats.Audio.ToString();
            
            // Update total resources in banner
            if (godotProject != null && godotProject.IsValid)
            {
                GodotTotalResources.Text = $"总资源: {stats.Total}";
            }
            
            // Update detailed resource list
            ResourceDetailsContainer.Children.Clear();
            
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                ResourceDetailsContainer.Children.Add(new TextBlock 
                { 
                    Text = "请先选择一个项目目录", 
                    Foreground = Brushes.Gray, 
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }
            
            // Create detailed stats
            var details = new List<(string Name, string Icon, int Count, string Color)>
            {
                ("GDScript 脚本 (.gd)", "📜", stats.Scripts, "#06b6d4"),
                ("场景文件 (.tscn)", "🎬", stats.Scenes, "#f59e0b"),
                ("资源文件 (.tres)", "📦", stats.Resources, "#10b981"),
                ("着色器 (.gdshader)", "🎨", stats.Shaders, "#ec4899"),
                ("扩展库 (.gdnlib)", "🔌", stats.Extensions, "#06b6d4"),
                ("纹理图片", "🖼️", stats.Textures, "#10b981"),
                ("音频文件", "🎵", stats.Audio, "#8b5cf6"),
                ("字体文件", "🔤", stats.Fonts, "#f97316"),
                ("其他文件", "📄", stats.Other, "#64748b")
            };
            
            foreach (var detail in details.Where(d => d.Count > 0))
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
                row.Children.Add(new TextBlock { Text = detail.Icon, FontSize = 16, Margin = new Thickness(0, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(new TextBlock { Text = detail.Name, Foreground = Brushes.White, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
                row.Children.Add(new TextBlock { 
                    Text = detail.Count.ToString(), 
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(detail.Color)), 
                    FontSize = 14, 
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                });
                ResourceDetailsContainer.Children.Add(row);
            }
            
            if (stats.Total == 0)
            {
                ResourceDetailsContainer.Children.Add(new TextBlock 
                { 
                    Text = "未找到资源文件", 
                    Foreground = Brushes.Gray, 
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
            }
        }

        private void PrevMonth(object sender, RoutedEventArgs e)
        {
            _calendarCurrentMonth = _calendarCurrentMonth.AddMonths(-1);
            UpdateCalendarView();
        }

        private void NextMonth(object sender, RoutedEventArgs e)
        {
            _calendarCurrentMonth = _calendarCurrentMonth.AddMonths(1);
            UpdateCalendarView();
        }

        // Task List View
        private void UpdateListView()
        {
            TaskListContainer.Children.Clear();

            // Header with proper Grid layout
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            headerGrid.Children.Add(new TextBlock { Text = "状态", Foreground = Brushes.Gray, FontSize = 11 });
            var titleHeader = new TextBlock { Text = "任务名称", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(titleHeader, 1);
            headerGrid.Children.Add(titleHeader);
            var catHeader = new TextBlock { Text = "分类", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(catHeader, 2);
            headerGrid.Children.Add(catHeader);
            var mileHeader = new TextBlock { Text = "里程碑", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(mileHeader, 3);
            headerGrid.Children.Add(mileHeader);
            var uiHeader = new TextBlock { Text = "紧急/重要", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(uiHeader, 4);
            headerGrid.Children.Add(uiHeader);
            var hourHeader = new TextBlock { Text = "工时", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(hourHeader, 5);
            headerGrid.Children.Add(hourHeader);
            var actionHeader = new TextBlock { Text = "操作", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(actionHeader, 6);
            headerGrid.Children.Add(actionHeader);

            TaskListContainer.Children.Add(headerGrid);

            string filter = SearchBox.Text.ToLower();
            string selectedMilestoneId = MilestoneFilter.SelectedValue?.ToString() ?? "";

            foreach (var task in _tasks)
            {
                if (!string.IsNullOrEmpty(selectedMilestoneId) && task.MilestoneId != selectedMilestoneId)
                    continue;

                if (!string.IsNullOrEmpty(filter) &&
                    !task.Title.ToLower().Contains(filter) &&
                    !task.Category.ToLower().Contains(filter))
                    continue;

                var row = CreateListViewRow(task);
                TaskListContainer.Children.Add(row);
            }
        }

        private UIElement CreateListViewRow(TaskItem task)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            // Status
            var statusColor = task.Status == "Completed" ? Brushes.Green : (task.Status == "In Progress" ? Brushes.Orange : Brushes.Gray);
            var statusBlock = new TextBlock { Text = task.Status switch { "Task Pool" => "📋 任务池", "In Progress" => "🔄 进行中", "Completed" => "✅ 完成", _ => task.Status }, Foreground = statusColor, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(statusBlock);

            // Title
            var titleBlock = new TextBlock { Text = task.Title, Foreground = Brushes.White, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(titleBlock, 1);
            grid.Children.Add(titleBlock);

            // Category
            var categoryBlock = new TextBlock { Text = task.Category, Foreground = GetCategoryBrush(task.Category), FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(categoryBlock, 2);
            grid.Children.Add(categoryBlock);

            // Milestone
            var milestone = _milestones.FirstOrDefault(m => m.Id == task.MilestoneId);
            var milestoneBlock = new TextBlock { Text = milestone?.Title ?? "-", Foreground = Brushes.LightGray, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(milestoneBlock, 3);
            grid.Children.Add(milestoneBlock);

            // Urgency/Importance
            var uiBlock = new TextBlock { Text = $"⚡{task.Urgency} ⭐{task.Importance}", Foreground = Brushes.LightGray, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(uiBlock, 4);
            grid.Children.Add(uiBlock);

            // Hours
            double totalHours = task.LoggedHours;
            if (task.LastTimerStart != null && DateTime.TryParse(task.LastTimerStart, out DateTime start))
                totalHours += (DateTime.Now - start).TotalHours;
            var hoursBlock = new TextBlock { Text = $"{totalHours:F1}h / {task.EstimatedHours}h", Foreground = Brushes.LightBlue, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            Grid.SetColumn(hoursBlock, 5);
            grid.Children.Add(hoursBlock);

            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 0, 0) };
            var btnEdit = new Button { Content = "编辑", Background = Brushes.Transparent, Foreground = Brushes.LightGray, BorderThickness = new Thickness(0), FontSize = 11, Padding = new Thickness(5, 2, 5, 2), Cursor = System.Windows.Input.Cursors.Hand };
            btnEdit.Click += (s, e) =>
            {
                var editDialog = new TaskEditDialog(task, _milestones) { Owner = this };
                if (editDialog.ShowDialog() == true)
                {
                    DataService.SaveTasks(_tasks);
                    UpdateListView();
                }
            };
            var btnDelete = new Button { Content = "删除", Background = Brushes.Transparent, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")), BorderThickness = new Thickness(0), FontSize = 11, Padding = new Thickness(5, 2, 5, 2), Cursor = System.Windows.Input.Cursors.Hand };
            btnDelete.Click += (s, e) =>
            {
                _tasks.Remove(task);
                DataService.SaveTasks(_tasks);
                UpdateListView();
            };
            btnPanel.Children.Add(btnEdit);
            btnPanel.Children.Add(btnDelete);
            Grid.SetColumn(btnPanel, 6);
            grid.Children.Add(btnPanel);

            border.Child = grid;
            return border;
        }

        // Calendar View
        private void UpdateCalendarView()
        {
            CalendarContainer.Children.Clear();
            CalendarMonthText.Text = _calendarCurrentMonth.ToString("yyyy年MM月");

            var firstDay = new DateTime(_calendarCurrentMonth.Year, _calendarCurrentMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_calendarCurrentMonth.Year, _calendarCurrentMonth.Month);
            // DayOfWeek: 0=Sunday, 1=Monday, ..., 6=Saturday
            int startDayOfWeek = (int)firstDay.DayOfWeek;

            // Week header - proper order starting from Sunday
            var weekHeader = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            weekHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string[] weekDays = { "日", "一", "二", "三", "四", "五", "六" };
            for (int i = 0; i < 7; i++)
            {
                var dayHeader = new TextBlock
                {
                    Text = weekDays[i],
                    Foreground = i == 0 ? Brushes.Red : (i == 6 ? Brushes.Red : Brushes.Gray),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    FontWeight = FontWeights.Bold
                };
                Grid.SetColumn(dayHeader, i);
                weekHeader.Children.Add(dayHeader);
            }
            CalendarContainer.Children.Add(weekHeader);

            // Calendar grid using Grid instead of WrapPanel for proper alignment
            var calendarGrid = new Grid();
            for (int i = 0; i < 7; i++)
            {
                calendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }

            // Calculate rows needed
            int totalCells = startDayOfWeek + daysInMonth;
            int rows = (int)Math.Ceiling(totalCells / 7.0);
            for (int i = 0; i < rows; i++)
            {
                calendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) });
            }

            // Empty cells before first day
            for (int i = 0; i < startDayOfWeek; i++)
            {
                var emptyCell = new Border { Margin = new Thickness(2) };
                Grid.SetRow(emptyCell, i / 7);
                Grid.SetColumn(emptyCell, i % 7);
                calendarGrid.Children.Add(emptyCell);
            }

            // Day cells
            for (int day = 1; day <= daysInMonth; day++)
            {
                var date = new DateTime(_calendarCurrentMonth.Year, _calendarCurrentMonth.Month, day);
                var tasksOnDay = _tasks.Where(t =>
                    (t.Status == "Completed" && !string.IsNullOrEmpty(t.DateCompleted) && DateTime.Parse(t.DateCompleted).Date == date) ||
                    (t.Status != "Completed" && !string.IsNullOrEmpty(t.DueDate) && DateTime.Parse(t.DueDate).Date == date)
                ).ToList();

                var cellBorder = new Border
                {
                    Margin = new Thickness(2),
                    Background = tasksOnDay.Count > 0 ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e3a5f")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(5)
                };

                var cellStack = new StackPanel();

                // Day number - highlight today
                bool isToday = date == DateTime.Today;
                var dayText = new TextBlock
                {
                    Text = day.ToString(),
                    Foreground = isToday ? Brushes.Orange : (date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Saturday ? Brushes.Red : Brushes.White),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold
                };
                cellStack.Children.Add(dayText);

                foreach (var t in tasksOnDay.Take(2))
                {
                    cellStack.Children.Add(new TextBlock { Text = t.Title, Foreground = Brushes.LightBlue, FontSize = 9, TextTrimming = TextTrimming.CharacterEllipsis });
                }
                if (tasksOnDay.Count > 2)
                {
                    cellStack.Children.Add(new TextBlock { Text = $"... +{tasksOnDay.Count - 2}", Foreground = Brushes.Gray, FontSize = 9 });
                }

                cellBorder.Child = cellStack;

                int cellIndex = startDayOfWeek + day - 1;
                Grid.SetRow(cellBorder, cellIndex / 7);
                Grid.SetColumn(cellBorder, cellIndex % 7);
                calendarGrid.Children.Add(cellBorder);
            }

            CalendarContainer.Children.Add(calendarGrid);
        }

        // Weekly Report
        private void UpdateWeeklyReport()
        {
            WeeklyReportContainer.Children.Clear();

            // Get week range (last 7 days)
            var endDate = DateTime.Today;
            var startDate = endDate.AddDays(-6);

            // Calculate total hours per day
            var dailyHours = new Dictionary<DateTime, double>();
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                dailyHours[d] = 0;
            }

            double totalWeekHours = 0;
            foreach (var task in _tasks)
            {
                double taskHours = 0;
                if (task.LastTimerStart != null && DateTime.TryParse(task.LastTimerStart, out DateTime timerStart))
                {
                    var timerDate = timerStart.Date;
                    if (timerDate >= startDate && timerDate <= endDate)
                    {
                        dailyHours[timerDate] += (DateTime.Now - timerStart).TotalHours;
                        taskHours += (DateTime.Now - timerStart).TotalHours;
                    }
                }
                // Only add logged hours if there are any recorded hours
                if (taskHours > 0 || task.LoggedHours > 0)
                    totalWeekHours += taskHours + task.LoggedHours;
            }

            // Summary
            var summaryBorder = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var summaryStack = new StackPanel();
            summaryStack.Children.Add(new TextBlock { Text = "本周工时汇总", Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold });
            summaryStack.Children.Add(new TextBlock { Text = $"总工时: {totalWeekHours:F1} 小时", Foreground = Brushes.White, FontSize = 24, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 10, 0, 0) });
            summaryStack.Children.Add(new TextBlock { Text = $"日期范围: {startDate:MM/dd} - {endDate:MM/dd}", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ddd6fe")), FontSize = 12, Margin = new Thickness(0, 5, 0, 0) });
            summaryBorder.Child = summaryStack;
            WeeklyReportContainer.Children.Add(summaryBorder);

            // Daily breakdown
            var chartBorder = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a")), CornerRadius = new CornerRadius(12), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 20) };
            var chartStack = new StackPanel();
            chartStack.Children.Add(new TextBlock { Text = "每日工时", Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15) });

            double maxHours = dailyHours.Values.Max() > 0 ? dailyHours.Values.Max() : 1;
            foreach (var kvp in dailyHours.OrderBy(k => k.Key))
            {
                var dayRow = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
                dayRow.Children.Add(new TextBlock { Text = $"{kvp.Key:ddd}", Foreground = Brushes.Gray, Width = 50 });

                var barBorder = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")), Height = 20, CornerRadius = new CornerRadius(4) };
                var barFill = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")), HorizontalAlignment = HorizontalAlignment.Left, Width = (kvp.Value / maxHours) * 200, CornerRadius = new CornerRadius(4) };
                barBorder.Child = barFill;
                dayRow.Children.Add(barBorder);

                dayRow.Children.Add(new TextBlock { Text = $"{kvp.Value:F1}h", Foreground = Brushes.White, Width = 50, TextAlignment = TextAlignment.Right, Margin = new Thickness(10, 0, 0, 0) });
                chartStack.Children.Add(dayRow);
            }
            chartBorder.Child = chartStack;
            WeeklyReportContainer.Children.Add(chartBorder);

            // Task breakdown
            var taskBorder = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a")), CornerRadius = new CornerRadius(12), Padding = new Thickness(20) };
            var taskStack = new StackPanel();
            taskStack.Children.Add(new TextBlock { Text = "任务工时明细", Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 15) });

            var tasksWithTime = _tasks.Where(t => t.LoggedHours > 0 || t.LastTimerStart != null).OrderByDescending(t => t.LoggedHours + (t.LastTimerStart != null && DateTime.TryParse(t.LastTimerStart, out var st) ? (DateTime.Now - st).TotalHours : 0)).ToList();

            if (tasksWithTime.Count == 0)
            {
                taskStack.Children.Add(new TextBlock { Text = "暂无工时记录", Foreground = Brushes.Gray, FontSize = 14 });
            }
            else
            {
                foreach (var task in tasksWithTime)
                {
                    double taskHours = task.LoggedHours;
                    if (task.LastTimerStart != null && DateTime.TryParse(task.LastTimerStart, out DateTime st))
                        taskHours += (DateTime.Now - st).TotalHours;

                    var taskRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                    taskRow.Children.Add(new TextBlock { Text = task.Title, Foreground = Brushes.White, Width = 250, TextTrimming = TextTrimming.CharacterEllipsis });
                    taskRow.Children.Add(new TextBlock { Text = $"{taskHours:F1}h", Foreground = Brushes.Cyan, TextAlignment = TextAlignment.Right, Width = 60 });
                    taskStack.Children.Add(taskRow);
                }
            }
            taskBorder.Child = taskStack;
            WeeklyReportContainer.Children.Add(taskBorder);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateKanban();
            UpdateListView();
        }

        private void MilestoneFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateKanban();
            UpdateListView();
        }

        private void UpdateMilestoneFilter()
        {
            var filterList = new List<Milestone> { new Milestone { Id = "", Title = "所有里程碑" } };
            filterList.AddRange(_milestones);
            MilestoneFilter.ItemsSource = filterList;
            MilestoneFilter.SelectedIndex = 0;
        }

        private void UpdateKanban()
        {
            TaskPoolList.Children.Clear();
            InProgressList.Children.Clear();
            CompletedList.Children.Clear();

            string filter = SearchBox.Text.ToLower();
            string selectedMilestoneId = MilestoneFilter.SelectedValue?.ToString() ?? "";

            foreach (var task in _tasks)
            {
                if (!string.IsNullOrEmpty(selectedMilestoneId) && task.MilestoneId != selectedMilestoneId)
                    continue;

                if (!string.IsNullOrEmpty(filter) &&
                    !task.Title.ToLower().Contains(filter) &&
                    !task.Category.ToLower().Contains(filter) &&
                    !(task.Tags?.Any(t => t.ToLower().Contains(filter)) ?? false))
                {
                    continue;
                }

                var card = CreateTaskCard(task);
                if (task.Status == "Task Pool") TaskPoolList.Children.Add(card);
                else if (task.Status == "In Progress") InProgressList.Children.Add(card);
                else if (task.Status == "Completed") CompletedList.Children.Add(card);
            }
        }

        private UIElement CreateTaskCard(TaskItem task)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 12),
            };

            var stack = new StackPanel();

            var categoryText = new TextBlock
            {
                Text = task.Category.ToUpper(),
                Foreground = GetCategoryBrush(task.Category),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var titleText = new TextBlock
            {
                Text = task.Title,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };

            stack.Children.Add(categoryText);
            stack.Children.Add(titleText);

            // Add Milestone Badge
            if (!string.IsNullOrEmpty(task.MilestoneId))
            {
                var milestone = _milestones.FirstOrDefault(m => m.Id == task.MilestoneId);
                if (milestone != null)
                {
                    var milestoneBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e3a5f")),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(6, 2, 6, 2),
                        Margin = new Thickness(0, 5, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    milestoneBorder.Child = new TextBlock
                    {
                        Text = $"🎯 {milestone.Title}",
                        FontSize = 9,
                        Foreground = Brushes.Cyan,
                        FontWeight = FontWeights.SemiBold
                    };
                    stack.Children.Add(milestoneBorder);
                }
            }

            // Add Description (Markdown)
            if (!string.IsNullOrEmpty(task.Description))
            {
                var descContainer = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
                MarkdownRenderer.Render(descContainer, task.Description, (lineIndex, isChecked) => HandleChecklistChange(task, lineIndex, isChecked));
                stack.Children.Add(descContainer);
            }

            // Add Tags
            if (task.Tags != null && task.Tags.Count > 0)
            {
                var tagsPanel = new WrapPanel { Margin = new Thickness(0, 5, 0, 0) };
                foreach (var tag in task.Tags)
                {
                    tagsPanel.Children.Add(new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 1, 5, 2),
                        Margin = new Thickness(0, 0, 5, 5),
                        Child = new TextBlock { Text = tag, FontSize = 9, Foreground = Brushes.LightGray }
                    });
                }
                stack.Children.Add(tagsPanel);
            }

            // Add Urgency/Importance indicators
            var metaStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            metaStack.Children.Add(new TextBlock { Text = $"⚡ {task.Urgency}", Foreground = Brushes.Orange, FontSize = 10, Margin = new Thickness(0, 0, 10, 0) });
            metaStack.Children.Add(new TextBlock { Text = $"⭐ {task.Importance}", Foreground = Brushes.Gold, FontSize = 10, Margin = new Thickness(0, 0, 10, 0) });

            // Time Tracking Display
            double totalLogged = task.LoggedHours;
            if (task.LastTimerStart != null && DateTime.TryParse(task.LastTimerStart, out DateTime liveStart))
            {
                totalLogged += (DateTime.Now - liveStart).TotalHours;
            }

            var timeText = new TextBlock
            {
                Text = $"🕒 {FormatTime(totalLogged)} / {task.EstimatedHours}h",
                Foreground = task.LastTimerStart != null ? Brushes.LightGreen : Brushes.LightBlue,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            metaStack.Children.Add(timeText);
            stack.Children.Add(metaStack);

            // Add Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };

            var btnTimer = new Button
            {
                Content = task.LastTimerStart == null ? "▶ 开始计时" : "⏹ 停止",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(8, 4, 8, 4),
                Background = task.LastTimerStart == null ? Brushes.Transparent : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                Foreground = task.LastTimerStart == null ? Brushes.LightGreen : Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 10
            };
            btnTimer.Click += (s, e) =>
            {
                ToggleTimer(task);
                UpdateKanban(); // Refresh to update button state and time
            };
            btnPanel.Children.Add(btnTimer);

            var btnDelete = new Button
            {
                Content = "删除",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 4, 10, 4),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            btnDelete.Click += (s, e) =>
            {
                _tasks.Remove(task);
                DataService.SaveTasks(_tasks);
                UpdateKanban();
            };

            var btnEdit = new Button
            {
                Content = "编辑",
                Margin = new Thickness(0, 0, 10, 0),
                Padding = new Thickness(10, 4, 10, 4),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11
            };
            btnEdit.Click += (s, e) =>
            {
                var editDialog = new TaskEditDialog(task, _milestones) { Owner = this };
                if (editDialog.ShowDialog() == true)
                {
                    DataService.SaveTasks(_tasks);
                    UpdateKanban();
                }
            };

            var btnMove = new Button
            {
                Content = task.Status == "Completed" ? "完成" : (task.Status == "In Progress" ? "完成" : "下一步"),
                Padding = new Thickness(15, 6, 15, 6),
                Style = (Style)FindResource("PremiumButtonStyle")
            };
            btnMove.Click += (s, e) => MoveTask(task);

            btnPanel.Children.Add(btnDelete);
            btnPanel.Children.Add(btnEdit);
            btnPanel.Children.Add(btnMove);
            stack.Children.Add(btnPanel);
            border.Child = stack;

            return border;
        }

        private void ToggleTimer(TaskItem task)
        {
            if (task.LastTimerStart == null)
            {
                // Start timer
                task.LastTimerStart = DateTime.Now.ToString("o"); // ISO 8601
            }
            else
            {
                // Stop timer
                if (DateTime.TryParse(task.LastTimerStart, out DateTime startTime))
                {
                    TimeSpan elapsed = DateTime.Now - startTime;
                    task.LoggedHours += elapsed.TotalHours;
                }
                task.LastTimerStart = null;
            }
            DataService.SaveTasks(_tasks);
        }

        private string FormatTime(double hours)
        {
            TimeSpan span = TimeSpan.FromHours(hours);
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours}h {span.Minutes}m";
            return $"{span.Minutes}m {span.Seconds}s";
        }

        private void HandleChecklistChange(TaskItem task, int lineIndex, bool isChecked)
        {
            string[] lines = task.Description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                var line = lines[lineIndex];
                var match = System.Text.RegularExpressions.Regex.Match(line, @"^(\s*-\s*\[)([ xX])(\]\s*.*)");
                if (match.Success)
                {
                    lines[lineIndex] = match.Groups[1].Value + (isChecked ? "x" : " ") + match.Groups[3].Value;
                    task.Description = string.Join("\n", lines);
                    DataService.SaveTasks(_tasks);
                    // No need to full update, the UI checkbox is already toggled
                }
            }
        }

        private void MoveTask(TaskItem task)
        {
            if (task.Status == "Task Pool")
            {
                task.Status = "In Progress";
            }
            else if (task.Status == "In Progress")
            {
                task.Status = "Completed";
                task.DateCompleted = DateTime.Now.ToString("yyyy-MM-dd");
            }
            // If task is already Completed, do nothing - completed tasks stay in Completed list

            DataService.SaveTasks(_tasks);
            UpdateKanban();
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new TaskItem
            {
                Title = "新开发任务",
                Category = "程序",
                Status = "Task Pool",
                Urgency = 3,
                Importance = 4,
                Tags = new List<string> { "待定" },
                MilestoneId = MilestoneFilter.SelectedValue?.ToString() ?? ""
            };
            _tasks.Add(newTask);
            DataService.SaveTasks(_tasks);
            
            // 根据当前视图类型刷新对应的视图
            if (TaskListViewGrid.Visibility == Visibility.Visible)
            {
                UpdateListView();
            }
            else
            {
                UpdateKanban();
            }
        }

        private Brush GetCategoryBrush(string category)
        {
            return category switch
            {
                "美术" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
                "程序" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#06b6d4")),
                "环境" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"))
            };
        }

        private void PopulateHeatmap()
        {
            HeatmapGrid.Children.Clear();
            var activity = string.IsNullOrEmpty(_currentProjectPath)
                ? new Dictionary<DateTime, int>()
                : GitService.GetActivityHeatmap(_currentProjectPath);

            DateTime endDate = DateTime.Now.Date;
            DateTime startDate = endDate.AddDays(-364);

            // Adjust to start on Sunday to match GitHub style (52 weeks)
            while (startDate.DayOfWeek != DayOfWeek.Sunday)
                startDate = startDate.AddDays(-1);

            // 正确的排列方式：列从左到右，每列7格
            // 第一格对应周日（行0），第七格对应周六（行6）
            // 使用 day 外层循环，week 内层循环，确保同一周的7天在同一列
            for (int day = 0; day < 7; day++)
            {
                for (int week = 0; week < 53; week++)
                {
                    DateTime current = startDate.AddDays(week * 7 + day);
                    // Skip future dates
                    if (current > endDate) continue;

                    int count = activity.ContainsKey(current) ? activity[current] : 0;
                    int level = count == 0 ? 0 : Math.Min(4, (count / 2) + 1);

                    var border = new Border
                    {
                        Width = 11,
                        Height = 11,
                        Margin = new Thickness(2),
                        CornerRadius = new CornerRadius(2),
                        Background = GetLevelBrush(level),
                        ToolTip = $"{current:yyyy-MM-dd}: {count} commits"
                    };
                    HeatmapGrid.Children.Add(border);
                }
            }
        }

        private Brush GetLevelBrush(int level)
        {
            return level switch
            {
                0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155")),
                1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4c1d95")),
                2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7c3aed")),
                3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a78bfa")),
                4 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ddd6fe")),
                _ => Brushes.Transparent
            };
        }

        private void UpdateMilestoneView()
        {
            MilestoneList.Children.Clear();

            // Add Header with Add Button
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 20) };
            var title = new TextBlock { Text = "里程碑列表", Foreground = Brushes.White, FontSize = 24, FontWeight = FontWeights.Bold };
            var btnAdd = new Button { Content = "+ 新建里程碑", Style = (Style)FindResource("PremiumButtonStyle"), HorizontalAlignment = HorizontalAlignment.Right };
            btnAdd.Click += (s, e) =>
            {
                var newMilestone = new Milestone { Title = "新里程碑", Version = "1.0.0" };
                var editDialog = new MilestoneEditDialog(newMilestone) { Owner = this };
                if (editDialog.ShowDialog() == true)
                {
                    _milestones.Add(newMilestone);
                    DataService.SaveMilestones(_milestones);
                    UpdateMilestoneView();
                    UpdateMilestoneFilter();
                }
            };

            header.Children.Add(title);
            header.Children.Add(btnAdd);
            MilestoneList.Children.Add(header);

            // Create a vertical StackPanel for list layout
            var verticalStack = new StackPanel();

            foreach (var milestone in _milestones)
            {
                verticalStack.Children.Add(CreateMilestoneCard(milestone));
            }

            MilestoneList.Children.Add(verticalStack);
        }

        private UIElement CreateMilestoneCard(Milestone milestone)
        {
            var border = new Border
            {
                Style = (Style)FindResource("GlassBorder"),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var outerStack = new StackPanel();

            // 1. 定义主 Grid 布局 (2行 x 3列)
            var mainGrid = new Grid();
            
            // 列定义：左(自适应拉伸) | 中(根据内容/固定宽度自适应) | 右(根据按钮自适应)
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 行定义：第一排内容 | 第二排图表
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // ================= 左侧：信息面板 =================
            var infoStack = new StackPanel { Margin = new Thickness(0, 0, 10, 0) }; // 添加少许右侧边距防止贴得太紧
            var titleText = new TextBlock { Text = milestone.Title, Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold };
            var versionText = new TextBlock { Text = $"版本: {milestone.Version}", Foreground = Brushes.Cyan, FontSize = 12, Margin = new Thickness(0, 5, 0, 5) };
            var descText = new TextBlock { Text = milestone.Description, Foreground = Brushes.Gray, FontSize = 14, TextWrapping = TextWrapping.Wrap };
            var dateText = new TextBlock { Text = $"时间: {milestone.StartDate} ~ {milestone.TargetDate}", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 5, 0, 0) };

            infoStack.Children.Add(titleText);
            infoStack.Children.Add(versionText);
            infoStack.Children.Add(descText);
            infoStack.Children.Add(dateText);

            Grid.SetRow(infoStack, 0);
            Grid.SetColumn(infoStack, 0);
            mainGrid.Children.Add(infoStack);

            // ================= 中间：进度面板 =================
            var tasksInMilestone = _tasks.Where(t => t.MilestoneId == milestone.Id).ToList();
            int total = tasksInMilestone.Count;
            int completed = tasksInMilestone.Count(t => t.Status == "Completed");
            int inProgress = tasksInMilestone.Count(t => t.Status == "In Progress");
            int taskPool = tasksInMilestone.Count(t => t.Status == "Task Pool");
            double progress = total == 0 ? 0 : (double)completed / total * 100;

            var progressStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 10, 20, 10), Width = 150 };

            var progressText = new TextBlock
            {
                Text = $"{progress:F0}%\n({completed}/{total})",
                Foreground = Brushes.White,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold
            };
            progressStack.Children.Add(progressText);

            // Consolidation of Multi-colored Progress Bar
            var distributionGrid = new Grid { Height = 10, Margin = new Thickness(0, 0, 0, 10) };
            var totalTasks = Math.Max(total, 1);

            if (completed > 0)
            {
                var completedBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3,0,0,3) };
                completedBar.Width = (completed * 150.0 / totalTasks);
                distributionGrid.Children.Add(completedBar);
            }
            if (inProgress > 0)
            {
                var inProgressBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")), HorizontalAlignment = HorizontalAlignment.Left };
                inProgressBar.Margin = new Thickness(completed > 0 ? completed * 150.0 / totalTasks : 0, 0, 0, 0);
                inProgressBar.Width = (inProgress * 150.0 / totalTasks);
                distributionGrid.Children.Add(inProgressBar);
            }
            if (taskPool > 0)
            {
                var taskPoolBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(0,3,3,0) };
                double offset = (completed + inProgress) * 150.0 / totalTasks;
                taskPoolBar.Margin = new Thickness(offset, 0, 0, 0);
                taskPoolBar.Width = (taskPool * 150.0 / totalTasks);
                distributionGrid.Children.Add(taskPoolBar);
            }
            progressStack.Children.Add(distributionGrid);

            // Legend inside progress stack
            var legend = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
            legend.Children.Add(new TextBlock { Text = $"■ 完成 {completed} ", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")), FontSize = 9 });
            legend.Children.Add(new TextBlock { Text = $"■ 进行 {inProgress} ", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")), FontSize = 9 });
            legend.Children.Add(new TextBlock { Text = $"■ 池 {taskPool}", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")), FontSize = 9 });
            progressStack.Children.Add(legend);

            Grid.SetRow(progressStack, 0);
            Grid.SetColumn(progressStack, 1);
            mainGrid.Children.Add(progressStack);

            // ================= 右侧：按钮面板 =================
            var btnPanel = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };

            var btnViewTasks = new Button
            {
                Content = "查看任务",
                Padding = new Thickness(5, 10, 5, 10),
                Background = Brushes.Transparent,
                Foreground = Brushes.LightGreen,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnViewTasks.Click += (s, e) =>
            {
                MilestoneFilter.SelectedValue = milestone.Id;
                ShowTaskManagement(this, new RoutedEventArgs());
            };

            var btnEdit = new Button
            {
                Content = "编辑",
                Padding = new Thickness(5, 10, 5, 10),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnEdit.Click += (s, e) =>
            {
                var editDialog = new MilestoneEditDialog(milestone) { Owner = this };
                if (editDialog.ShowDialog() == true)
                {
                    DataService.SaveMilestones(_milestones);
                    UpdateMilestoneView();
                }
            };

            var btnDelete = new Button
            {
                Content = "删除",
                Padding = new Thickness(5, 10, 5, 10),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            btnDelete.Click += (s, e) =>
            {
                var result = MessageBox.Show($"确定要删除里程碑 \"{milestone.Title}\" 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _milestones.Remove(milestone);
                    foreach (var t in _tasks.Where(t => t.MilestoneId == milestone.Id))
                        t.MilestoneId = "";
                    DataService.SaveMilestones(_milestones);
                    DataService.SaveTasks(_tasks);
                    UpdateMilestoneView();
                }
            };

            btnPanel.Children.Add(btnViewTasks);
            btnPanel.Children.Add(btnEdit);
            btnPanel.Children.Add(btnDelete);

            Grid.SetRow(btnPanel, 0);
            Grid.SetColumn(btnPanel, 2);
            mainGrid.Children.Add(btnPanel);

            // ================= 底部：燃尽图 =================
            var chart = CreateBurndownChart(milestone, tasksInMilestone);
            if (chart != null)
            {
                // 加一个 Grid 或 Border 作为容器，方便控制与上方内容的间距
                var bottomContainer = new Grid { Margin = new Thickness(0, 15, 0, 0) };
                bottomContainer.Children.Add(chart);

                Grid.SetRow(bottomContainer, 1);       // 放在第二排
                Grid.SetColumn(bottomContainer, 0);    // 从第一列开始
                Grid.SetColumnSpan(bottomContainer, 3); // 跨越全部三列，保证填满底部

                mainGrid.Children.Add(bottomContainer);
            }

            // 组合并返回
            border.Child = mainGrid;
            outerStack.Children.Add(border);

            return outerStack;
        }

        private UIElement CreateTaskSummaryChart(Milestone milestone, List<TaskItem> tasks)
        {
            if (tasks.Count == 0) return new Border();

            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = "任务分布", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 0, 0, 10) });

            int taskPool = tasks.Count(t => t.Status == "Task Pool");
            int inProgress = tasks.Count(t => t.Status == "In Progress");
            int completed = tasks.Count(t => t.Status == "Completed");

            // Simple progress visualization
            var grid = new Grid();
            grid.Height = 20;

            if (completed > 0)
            {
                var completedBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(4) };
                completedBar.Width = (completed * 200.0 / tasks.Count);
                grid.Children.Add(completedBar);
            }
            if (inProgress > 0)
            {
                var inProgressBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(4) };
                inProgressBar.Margin = new Thickness(completed > 0 ? completed * 200.0 / tasks.Count : 0, 0, 0, 0);
                inProgressBar.Width = (inProgress * 200.0 / tasks.Count);
                grid.Children.Add(inProgressBar);
            }
            if (taskPool > 0)
            {
                var taskPoolBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(4) };
                double offset = (completed + inProgress) * 200.0 / tasks.Count;
                taskPoolBar.Margin = new Thickness(offset, 0, 0, 0);
                taskPoolBar.Width = (taskPool * 200.0 / tasks.Count);
                grid.Children.Add(taskPoolBar);
            }

            stack.Children.Add(grid);

            var legend = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            legend.Children.Add(new TextBlock { Text = $"✅ 完成: {completed}", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")), FontSize = 11, Margin = new Thickness(0, 0, 15, 0) });
            legend.Children.Add(new TextBlock { Text = $"🔄 进行: {inProgress}", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")), FontSize = 11, Margin = new Thickness(0, 0, 15, 0) });
            legend.Children.Add(new TextBlock { Text = $"📋 任务池: {taskPool}", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")), FontSize = 11 });
            stack.Children.Add(legend);

            border.Child = stack;
            return border;
        }

        private UIElement? CreateBurndownChart(Milestone milestone, List<TaskItem> tasks)
        {
            if (tasks.Count == 0 || string.IsNullOrEmpty(milestone.StartDate) || string.IsNullOrEmpty(milestone.TargetDate))
                return null;

            if (!DateTime.TryParse(milestone.StartDate, out DateTime start) || !DateTime.TryParse(milestone.TargetDate, out DateTime end))
                return null;

            int totalDays = (int)(end - start).TotalDays;
            if (totalDays <= 0) totalDays = 1;

            var canvas = new Canvas { Height = 80, Margin = new Thickness(0, 10, 0, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a")), ClipToBounds = true };

            double width = 150;
            double height = 80;
            double padding = 10;
            double chartWidth = width - (padding * 2);
            double chartHeight = height - (padding * 2);

            int totalTasks = Math.Max(tasks.Count, 1);

            // Draw Ideal Line (Dashed)
            var idealLine = new System.Windows.Shapes.Polyline
            {
                Stroke = Brushes.DarkSlateGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            idealLine.Points.Add(new Point(padding, padding));
            idealLine.Points.Add(new Point(padding + chartWidth, padding + chartHeight));
            canvas.Children.Add(idealLine);

            // Draw Actual Line
            var actualLine = new System.Windows.Shapes.Polyline
            {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")),
                StrokeThickness = 1.5
            };

            DateTime today = DateTime.Now.Date;
            int daysElapsed = (int)(today - start).TotalDays;
            if (daysElapsed < 0) daysElapsed = 0;

            for (int d = 0; d <= totalDays; d++)
            {
                DateTime currentDay = start.AddDays(d);
                if (currentDay > today) break;

                int remaining = totalTasks - tasks.Count(t => t.Status == "Completed" && !string.IsNullOrEmpty(t.DateCompleted) && DateTime.Parse(t.DateCompleted) < currentDay);

                double x = padding + (d * (chartWidth / totalDays));
                double y = padding + (remaining * (chartHeight / totalTasks));
                actualLine.Points.Add(new Point(x, y));
            }
            canvas.Children.Add(actualLine);

            // Label
            var label = new TextBlock { Text = "燃尽趋势", Foreground = Brushes.Gray, FontSize = 8, Margin = new Thickness(padding, 2, 0, 0) };
            canvas.Children.Add(label);

            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(2),
                Child = canvas
            };
        }

        // ============== Release Management ==============
        private void ShowReleases(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "发布管理";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Visible;
            PluginsViewGrid.Visibility = Visibility.Collapsed;
            UpdateNavButtons("Releases");
            UpdateReleasesView();
        }

        private void ShowPlugins(object sender, RoutedEventArgs e)
        {
            TitleText.Text = "插件管理";
            KanbanView.Visibility = Visibility.Collapsed;
            ProjectOverviewView.Visibility = Visibility.Collapsed;
            HeatmapView.Visibility = Visibility.Collapsed;
            MilestoneView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            CalendarViewGrid.Visibility = Visibility.Collapsed;
            WeeklyReportViewGrid.Visibility = Visibility.Collapsed;
            TaskManagementView.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            ResourcesViewGrid.Visibility = Visibility.Collapsed;
            GitDiffViewGrid.Visibility = Visibility.Collapsed;
            ReleasesViewGrid.Visibility = Visibility.Collapsed;
            PluginsViewGrid.Visibility = Visibility.Visible;
            UpdateNavButtons("Plugins");
            UpdatePluginsView();
        }

        private void RefreshPlugins(object sender, RoutedEventArgs e)
        {
            UpdatePluginsView();
        }

        private void UpdatePluginsView()
        {
            // Clear plugin list
            PluginsContainer.Children.Clear();
            PluginDetailsContainer.Children.Clear();

            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                PluginsContainer.Children.Add(new TextBlock
                {
                    Text = "请先选择项目目录",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }

            // Get plugins
            var plugins = GodotProjectService.GetPlugins(_currentProjectPath);

            if (plugins.Count == 0)
            {
                PluginsContainer.Children.Add(new TextBlock
                {
                    Text = "未找到插件 (addons 目录为空)",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                
                // Show empty details
                PluginDetailsContainer.Children.Add(new TextBlock
                {
                    Text = "选择一个插件查看详情",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }

            // Add plugin cards
            foreach (var plugin in plugins)
            {
                var pluginCard = CreatePluginCard(plugin);
                PluginsContainer.Children.Add(pluginCard);
            }

            // Show first plugin details by default
            if (plugins.Count > 0)
            {
                ShowPluginDetails(plugins[0]);
            }
        }

        private UIElement CreatePluginCard(GodotPluginInfo plugin)
        {
            var border = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stack = new StackPanel();

            var titleStack = new StackPanel { Orientation = Orientation.Horizontal };
            titleStack.Children.Add(new TextBlock
            {
                Text = plugin.IsEnabled ? "✅" : "❌",
                FontSize = 14,
                Margin = new Thickness(0, 0, 8, 0)
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = plugin.Name,
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.Bold
            });
            stack.Children.Add(titleStack);

            if (!string.IsNullOrEmpty(plugin.Version))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"版本: {plugin.Version}",
                    Foreground = Brushes.Cyan,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 0)
                });
            }

            if (!string.IsNullOrEmpty(plugin.Author))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = $"作者: {plugin.Author}",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            border.Child = stack;

            // Click to show details
            border.MouseLeftButtonDown += (s, e) => ShowPluginDetails(plugin);

            return border;
        }

        private void ShowPluginDetails(GodotPluginInfo plugin)
        {
            PluginDetailsContainer.Children.Clear();

            // Plugin name
            var nameText = new TextBlock
            {
                Text = plugin.Name,
                Foreground = Brushes.White,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15)
            };
            PluginDetailsContainer.Children.Add(nameText);

            // Status
            var statusBorder = new Border
            {
                Background = plugin.IsEnabled ?
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")) :
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 15)
            };
            statusBorder.Child = new TextBlock
            {
                Text = plugin.IsEnabled ? "✅ 已启用" : "❌ 已禁用",
                Foreground = Brushes.White,
                FontSize = 12
            };
            PluginDetailsContainer.Children.Add(statusBorder);

            // Details grid
            var details = new List<(string Label, string Value)>
            {
                ("路径", plugin.Path),
                ("版本", string.IsNullOrEmpty(plugin.Version) ? "-" : plugin.Version),
                ("作者", string.IsNullOrEmpty(plugin.Author) ? "-" : plugin.Author),
                ("描述", string.IsNullOrEmpty(plugin.Description) ? "无描述" : plugin.Description)
            };

            foreach (var (label, value) in details)
            {
                var row = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
                row.Children.Add(new TextBlock
                {
                    Text = label + ":",
                    Foreground = Brushes.Gray,
                    FontSize = 13,
                    Width = 60
                });
                row.Children.Add(new TextBlock
                {
                    Text = value,
                    Foreground = Brushes.White,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap
                });
                PluginDetailsContainer.Children.Add(row);
            }
        }

        private void OpenGodotEditor(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                MessageBox.Show("请先选择一个项目目录", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool success = GodotProjectService.OpenInEditor(_currentProjectPath);
            if (!success)
            {
                MessageBox.Show("无法找到 Godot 编辑器。\n\n请确保 Godot 已安装并添加到系统 PATH，或将 godot.exe 放在项目目录中。", 
                    "启动失败", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateReleasesView()
        {
            ReleasesContainer.Children.Clear();
            
            if (_releases.Count == 0)
            {
                ReleasesContainer.Children.Add(new TextBlock
                {
                    Text = "暂无发布计划，点击\"新建版本\"创建第一个发布版本。",
                    Foreground = Brushes.Gray,
                    FontSize = 14,
                    Margin = new Thickness(10)
                });
                return;
            }
            
            foreach (var release in _releases)
            {
                ReleasesContainer.Children.Add(CreateReleaseCard(release));
            }
        }

        private UIElement CreateReleaseCard(ReleaseInfo release)
        {
            var border = new Border
            {
                Style = (Style)FindResource("GlassBorder"),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var stack = new StackPanel();
            
            // Header
            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 15) };
            
            var versionBadge = new Border
            {
                Background = release.Status == "Released" ? 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")) :
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 10, 0)
            };
            versionBadge.Child = new TextBlock
            {
                Text = release.Version,
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            
            var statusBadge = new Border
            {
                Background = release.Status switch
                {
                    "Released" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")),
                    "InProgress" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b"))
                },
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 10, 0)
            };
            statusBadge.Child = new TextBlock
            {
                Text = release.Status switch { "Released" => "✅ 已发布", "InProgress" => "🔄 进行中", _ => "📋 规划中" },
                Foreground = Brushes.White,
                FontSize = 11
            };
            
            header.Children.Add(statusBadge);
            header.Children.Add(versionBadge);
            
            // Title
            var titleText = new TextBlock
            {
                Text = release.Title,
                Foreground = Brushes.White,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(header);
            stack.Children.Add(titleText);
            
            // Description
            if (!string.IsNullOrEmpty(release.Description))
            {
                var descText = new TextBlock
                {
                    Text = release.Description,
                    Foreground = Brushes.Gray,
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stack.Children.Add(descText);
            }
            
            // Release date
            if (!string.IsNullOrEmpty(release.ReleaseDate))
            {
                var dateText = new TextBlock
                {
                    Text = $"发布日期: {release.ReleaseDate}",
                    Foreground = Brushes.Cyan,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                stack.Children.Add(dateText);
            }
            
            // Checklist
            if (release.Checklist.Count > 0)
            {
                var checklistLabel = new TextBlock
                {
                    Text = "发布检查清单",
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stack.Children.Add(checklistLabel);
                
                for (int i = 0; i < release.Checklist.Count; i++)
                {
                    var checkItem = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                    var checkBox = new CheckBox
                    {
                        IsChecked = i < release.ChecklistCompleted.Count && release.ChecklistCompleted[i],
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    int index = i;
                    checkBox.Checked += (s, ev) =>
                    {
                        while (release.ChecklistCompleted.Count <= index) release.ChecklistCompleted.Add(false);
                        release.ChecklistCompleted[index] = true;
                        DataService.SaveReleases(_releases);
                    };
                    checkBox.Unchecked += (s, ev) =>
                    {
                        while (release.ChecklistCompleted.Count <= index) release.ChecklistCompleted.Add(false);
                        release.ChecklistCompleted[index] = false;
                        DataService.SaveReleases(_releases);
                    };
                    
                    var itemText = new TextBlock
                    {
                        Text = release.Checklist[i],
                        Foreground = (i < release.ChecklistCompleted.Count && release.ChecklistCompleted[i]) ? 
                            Brushes.Gray : Brushes.White,
                        FontSize = 13,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextDecorations = (i < release.ChecklistCompleted.Count && release.ChecklistCompleted[i]) ? 
                            TextDecorations.Strikethrough : null
                    };
                    
                    checkItem.Children.Add(checkBox);
                    checkItem.Children.Add(itemText);
                    stack.Children.Add(checkItem);
                }
            }
            
            // Buttons
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };
            
            var btnDelete = new Button
            {
                Content = "删除",
                Padding = new Thickness(15, 8, 15, 8),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ef4444")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12
            };
            btnDelete.Click += (s, e) =>
            {
                var result = MessageBox.Show($"确定要删除版本 \"{release.Version}\" 吗？", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _releases.Remove(release);
                    DataService.SaveReleases(_releases);
                    UpdateReleasesView();
                }
            };
            
            var btnEdit = new Button
            {
                Content = "编辑",
                Padding = new Thickness(15, 8, 15, 8),
                Background = Brushes.Transparent,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8")),
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 12,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnEdit.Click += (s, e) => EditRelease(release);
            
            btnPanel.Children.Add(btnDelete);
            btnPanel.Children.Add(btnEdit);
            stack.Children.Add(btnPanel);
            
            border.Child = stack;
            return border;
        }

        private void AddRelease_Click(object sender, RoutedEventArgs e)
        {
            var newRelease = new ReleaseInfo
            {
                Version = "1.0.0",
                Title = "新版本发布",
                Status = "Planning",
                Checklist = new List<string> { "功能开发完成", "测试通过", "文档更新", "构建验证" },
                ChecklistCompleted = new List<bool> { false, false, false, false }
            };
            
            // Simple edit dialog using InputBox
            var versionDialog = new Window
            {
                Title = "新建发布版本",
                Width = 400,
                Height = 620,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a")),
                ResizeMode = ResizeMode.NoResize
            };
            
            var dialogStack = new StackPanel { Margin = new Thickness(20) };
            
            dialogStack.Children.Add(new TextBlock { Text = "版本号 (如 1.0.0)", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var versionInput = new TextBox { Text = newRelease.Version, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            dialogStack.Children.Add(versionInput);
            
            dialogStack.Children.Add(new TextBlock { Text = "标题", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var titleInput = new TextBox { Text = newRelease.Title, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            dialogStack.Children.Add(titleInput);
            
            dialogStack.Children.Add(new TextBlock { Text = "发布日期 (yyyy-MM-dd)", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var dateInput = new TextBox { Text = "", Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            dialogStack.Children.Add(dateInput);
            
            dialogStack.Children.Add(new TextBlock { Text = "状态", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var statusCombo = new ComboBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 0, 20) };
            statusCombo.Items.Add("Planning");
            statusCombo.Items.Add("InProgress");
            statusCombo.Items.Add("Released");
            statusCombo.SelectedIndex = 0;
            dialogStack.Children.Add(statusCombo);
            
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = new Button { Content = "取消", Padding = new Thickness(20, 8, 20, 8), Background = Brushes.Transparent, Foreground = Brushes.Gray, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 10, 0) };
            btnCancel.Click += (s, ev) => versionDialog.Close();
            var btnSave = new Button { Content = "保存", Padding = new Thickness(20, 8, 20, 8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            btnSave.Click += (s, ev) =>
            {
                newRelease.Version = versionInput.Text;
                newRelease.Title = titleInput.Text;
                newRelease.ReleaseDate = dateInput.Text;
                newRelease.Status = statusCombo.SelectedItem?.ToString() ?? "Planning";
                versionDialog.DialogResult = true;
            };
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            dialogStack.Children.Add(btnPanel);
            
            versionDialog.Content = dialogStack;
            versionDialog.Owner = this;
            
            if (versionDialog.ShowDialog() == true)
            {
                _releases.Add(newRelease);
                DataService.SaveReleases(_releases);
                UpdateReleasesView();
            }
        }

        private void EditRelease(ReleaseInfo release)
        {
            var editDialog = new Window
            {
                Title = $"编辑版本 {release.Version}",
                Width = 400,
                Height = 550,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0f172a")),
                ResizeMode = ResizeMode.NoResize
            };
            
            var dialogStack = new StackPanel { Margin = new Thickness(20) };
            
            dialogStack.Children.Add(new TextBlock { Text = "版本号", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var versionInput = new TextBox { Text = release.Version, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            dialogStack.Children.Add(versionInput);
            
            dialogStack.Children.Add(new TextBlock { Text = "标题", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var titleInput = new TextBox { Text = release.Title, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            dialogStack.Children.Add(titleInput);
            
            dialogStack.Children.Add(new TextBlock { Text = "发布日期", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var dateInput = new TextBox { Text = release.ReleaseDate, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 15) };
            dialogStack.Children.Add(dateInput);
            
            dialogStack.Children.Add(new TextBlock { Text = "状态", Foreground = Brushes.Gray, FontSize = 12, Margin = new Thickness(0, 0, 0, 5) });
            var statusCombo = new ComboBox { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")), Foreground = Brushes.White, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 0, 20) };
            statusCombo.Items.Add("Planning");
            statusCombo.Items.Add("InProgress");
            statusCombo.Items.Add("Released");
            statusCombo.SelectedItem = release.Status;
            dialogStack.Children.Add(statusCombo);
            
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnCancel = new Button { Content = "取消", Padding = new Thickness(20, 8, 20, 8), Background = Brushes.Transparent, Foreground = Brushes.Gray, BorderThickness = new Thickness(0), Margin = new Thickness(0, 0, 10, 0) };
            btnCancel.Click += (s, ev) => editDialog.Close();
            var btnSave = new Button { Content = "保存", Padding = new Thickness(20, 8, 20, 8), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8b5cf6")), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
            btnSave.Click += (s, ev) =>
            {
                release.Version = versionInput.Text;
                release.Title = titleInput.Text;
                release.ReleaseDate = dateInput.Text;
                release.Status = statusCombo.SelectedItem?.ToString() ?? "Planning";
                editDialog.DialogResult = true;
            };
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnSave);
            dialogStack.Children.Add(btnPanel);
            
            editDialog.Content = dialogStack;
            editDialog.Owner = this;
            
            if (editDialog.ShowDialog() == true)
            {
                DataService.SaveReleases(_releases);
                UpdateReleasesView();
            }
        }
    }
}
