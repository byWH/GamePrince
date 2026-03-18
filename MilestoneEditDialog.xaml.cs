using System;
using System.Windows;

namespace GamePrince
{
    public partial class MilestoneEditDialog : Window
    {
        public Milestone Milestone { get; private set; }
        public bool IsDeleted { get; private set; } = false;

        public MilestoneEditDialog(Milestone milestone)
        {
            InitializeComponent();
            Milestone = milestone;

            TitleInput.Text = milestone.Title;
            VersionInput.Text = milestone.Version;
            DescriptionInput.Text = milestone.Description;
            StartDateInput.Text = milestone.StartDate;
            TargetDateInput.Text = milestone.TargetDate;
            IsCompletedInput.IsChecked = milestone.IsCompleted;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 验证标题不能为空
            if (string.IsNullOrWhiteSpace(TitleInput.Text))
            {
                MessageBox.Show("里程碑标题不能为空！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleInput.Focus();
                return;
            }

            // 验证版本号格式（可选，但建议符合语义化版本）
            string version = VersionInput.Text.Trim();
            if (!string.IsNullOrEmpty(version))
            {
                // 简单的版本号验证：支持 v1.0.0, 1.0.0, 1.0 等格式
                if (!System.Text.RegularExpressions.Regex.IsMatch(version, @"^v?\d+(\.\d+)*$"))
                {
                    MessageBox.Show("版本号格式不正确，建议使用 v1.0.0 或 1.0.0 格式！", "验证警告", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            // 验证日期格式
            string startDateStr = StartDateInput.Text.Trim();
            string targetDateStr = TargetDateInput.Text.Trim();

            if (!string.IsNullOrWhiteSpace(startDateStr))
            {
                if (!DateTime.TryParse(startDateStr, out DateTime startDate))
                {
                    MessageBox.Show("开始日期格式不正确，请使用 YYYY-MM-DD 格式！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartDateInput.Focus();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(targetDateStr))
            {
                if (!DateTime.TryParse(targetDateStr, out DateTime targetDate))
                {
                    MessageBox.Show("目标日期格式不正确，请使用 YYYY-MM-DD 格式！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TargetDateInput.Focus();
                    return;
                }

                // 如果两个日期都填写了，验证目标日期是否晚于开始日期
                if (!string.IsNullOrWhiteSpace(startDateStr) && DateTime.TryParse(startDateStr, out DateTime startDateVal) &&
                    DateTime.TryParse(targetDateStr, out DateTime targetDateVal))
                {
                    if (targetDateVal < startDateVal)
                    {
                        MessageBox.Show("目标日期必须晚于或等于开始日期！", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TargetDateInput.Focus();
                        return;
                    }
                }
            }

            // 所有验证通过，保存数据
            Milestone.Title = TitleInput.Text.Trim();
            Milestone.Version = version;
            Milestone.Description = DescriptionInput.Text;
            Milestone.StartDate = startDateStr;
            Milestone.TargetDate = targetDateStr;
            Milestone.IsCompleted = IsCompletedInput.IsChecked ?? false;

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
