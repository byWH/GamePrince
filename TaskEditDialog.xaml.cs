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
            TagsInput.Text = string.Join(", ", task.Tags);
            DescriptionInput.Text = task.Description;

            MilestoneInput.DisplayMemberPath = "Title";
            MilestoneInput.SelectedValuePath = "Id";
            MilestoneInput.ItemsSource = milestones;
            MilestoneInput.SelectedValue = task.MilestoneId;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Task.Title = TitleInput.Text;
            Task.Description = DescriptionInput.Text;
            Task.Category = CategoryInput.Text;
            Task.MilestoneId = MilestoneInput.SelectedValue?.ToString() ?? "";
            
            if (int.TryParse(UrgencyInput.Text, out int urgency))
                Task.Urgency = urgency;
            
            if (int.TryParse(ImportanceInput.Text, out int importance))
                Task.Importance = importance;

            if (double.TryParse(EstimateInput.Text, out double estimate))
                Task.EstimatedHours = estimate;

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
