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
        private string _currentProjectPath = "";
        private System.Windows.Threading.DispatcherTimer _uiTimer;
        private DateTime _calendarCurrentMonth = DateTime.Now.Date;

        public MainWindow()
        {
            InitializeComponent();
            _tasks = DataService.LoadTasks();
            _milestones = DataService.LoadMilestones();
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
            var fileDist = GitService.GetFileTypeDistribution(_currentProjectPath);
            var branches = GitService.GetBranches(_currentProjectPath);
            int totalFiles = fileDist.Values.Sum();

            var topExtensions = fileDist.OrderByDescending(kv => kv.Value)
                .Take(3)
                .Select(kv => $"{kv.Key}: {kv.Value}");

            StatsText.Text = $"提交总数: {commits.Count} | 文件总数: {totalFiles} ({string.Join(", ", topExtensions)})";

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
            KanbanView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            UpdateNavButtons("ProjectOverview");
            UpdateProjectOverview();
        }

        private void UpdateNavButtons(string activeButton)
        {
            NavProjectOverview.Tag = activeButton == "ProjectOverview" ? "Active" : null;
            NavTasks.Tag = activeButton == "Tasks" ? "Active" : null;
            NavPlan.Tag = activeButton == "Plan" ? "Active" : null;
            NavHeatmap.Tag = activeButton == "Heatmap" ? "Active" : null;
            NavCalendar.Tag = activeButton == "Calendar" ? "Active" : null;
            NavWeeklyReport.Tag = activeButton == "WeeklyReport" ? "Active" : null;
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
                var commits = GitService.GetCommitHistory(_currentProjectPath);
                var branches = GitService.GetBranches(_currentProjectPath);
                var currentBranch = branches.FirstOrDefault(b => b.IsCurrent);
                OverviewGitStats.Text = $"Git提交: {commits.Count} | 分支: {currentBranch?.Name ?? "-"}";
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
            KanbanView.Visibility = Visibility.Collapsed;
            TaskListViewGrid.Visibility = Visibility.Collapsed;
            TaskViewSwitcher.Visibility = Visibility.Collapsed;
            UpdateNavButtons("WeeklyReport");
            UpdateWeeklyReport();
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
            UpdateKanban();
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

            // GitHub uses 53 weeks to cover a full year
            for (int week = 0; week < 53; week++)
            {
                for (int day = 0; day < 7; day++)
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
                0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e293b")),
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

            var dock = new DockPanel();

            var infoStack = new StackPanel();
            var titleText = new TextBlock { Text = milestone.Title, Foreground = Brushes.White, FontSize = 18, FontWeight = FontWeights.Bold };
            var versionText = new TextBlock { Text = $"版本: {milestone.Version}", Foreground = Brushes.Cyan, FontSize = 12, Margin = new Thickness(0, 5, 0, 5) };
            var descText = new TextBlock { Text = milestone.Description, Foreground = Brushes.Gray, FontSize = 14, TextWrapping = TextWrapping.Wrap };
            var dateText = new TextBlock { Text = $"时间: {milestone.StartDate} ~ {milestone.TargetDate}", Foreground = Brushes.Gray, FontSize = 11, Margin = new Thickness(0, 5, 0, 0) };

            infoStack.Children.Add(titleText);
            infoStack.Children.Add(versionText);
            infoStack.Children.Add(descText);
            infoStack.Children.Add(dateText);

            dock.Children.Add(infoStack);

            // Progress and Task Distribution (Right Side)
            var tasksInMilestone = _tasks.Where(t => t.MilestoneId == milestone.Id).ToList();
            int total = tasksInMilestone.Count;
            int completed = tasksInMilestone.Count(t => t.Status == "Completed");
            int inProgress = tasksInMilestone.Count(t => t.Status == "In Progress");
            int taskPool = tasksInMilestone.Count(t => t.Status == "Task Pool");
            double progress = total == 0 ? 0 : (double)completed / total * 100;

            var progressStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(20, 10, 0, 10), Width = 150 };

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
                var completedBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10b981")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3) };
                completedBar.Width = (completed * 150.0 / totalTasks);
                distributionGrid.Children.Add(completedBar);
            }
            if (inProgress > 0)
            {
                var inProgressBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f59e0b")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3) };
                inProgressBar.Margin = new Thickness(completed > 0 ? completed * 150.0 / totalTasks : 0, 0, 0, 0);
                inProgressBar.Width = (inProgress * 150.0 / totalTasks);
                distributionGrid.Children.Add(inProgressBar);
            }
            if (taskPool > 0)
            {
                var taskPoolBar = new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748b")), HorizontalAlignment = HorizontalAlignment.Left, CornerRadius = new CornerRadius(3) };
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

            // Buttons inside progress stack
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 15, 0, 0) };

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
            progressStack.Children.Add(btnPanel);

            // Add Burndown Chart into progress stack
            var chart = CreateBurndownChart(milestone, tasksInMilestone);
            if (chart != null)
            {
                progressStack.Children.Add(chart);
            }

            DockPanel.SetDock(progressStack, Dock.Right);
            dock.Children.Add(progressStack);

            border.Child = dock;
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
    }
}
