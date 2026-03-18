using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GamePrince
{
    public partial class TaskEditDialog : Window
    {
        public TaskItem Task { get; private set; }

        public TaskEditDialog(TaskItem task, List<Milestone> milestones)
        {
            InitializeComponent();
            Task = task;

            TitleInput.Text = task.Title;
            CategoryInput.Text = task.Category;
            UrgencyInput.Text = task.Urgency.ToString();
            ImportanceInput.Text = task.Importance.ToString();
            EstimateInput.Text = task.EstimatedHours.ToString();
            DueDateInput.Text = task.DueDate;
            TagsInput.Text = string.Join(", ", task.Tags);
            DescriptionInput.Text = task.Description;

            MilestoneInput.ItemsSource = milestones;
            MilestoneInput.SelectedValue = task.MilestoneId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 验证任务标题不能为空
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                MessageBox.Show("任务标题不能为空！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleInput.Focus();
                return;
            }

            // 验证紧急度范围 (1-10)
            if (!int.TryParse(UrgencyInput.Text, out int urgency) || urgency < 1 || urgency > 10)
            {
                MessageBox.Show("紧急度必须在 1-10 之间！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                UrgencyInput.Focus();
                return;
            }

            // 验证重要度范围 (1-10)
            if (!int.TryParse(ImportanceInput.Text, out int importance) || importance < 1 || importance > 10)
            {
                MessageBox.Show("重要度必须在 1-10 之间！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                ImportanceInput.Focus();
                return;
            }

            // 验证预计工时（非负数）
            if (!double.TryParse(EstimateInput.Text, out double estimate) || estimate < 0)
            {
                MessageBox.Show("预计工时必须是正数！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                EstimateInput.Focus();
                return;
            }

            // 验证日期格式（如果输入了日期）
            if (!string.IsNullOrWhiteSpace(DueDateInput.Text))
            {
                if (!DateTime.TryParse(DueDateInput.Text, out _))
                {
                    MessageBox.Show("日期格式不正确，请使用 YYYY-MM-DD 格式！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DueDateInput.Focus();
                    return;
                }
            }

            // 所有验证通过，保存数据
            Task.Title = TitleInput.Text.Trim();
            Task.Description = DescriptionInput.Text;
            Task.Category = CategoryInput.Text;
            Task.MilestoneId = MilestoneInput.SelectedValue?.ToString() ?? "";
            Task.Urgency = urgency;
            Task.Importance = importance;
            Task.EstimatedHours = estimate;
            Task.DueDate = DueDateInput.Text;

            Task.Tags = TagsInput.Text.Split(',')
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .ToList();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
