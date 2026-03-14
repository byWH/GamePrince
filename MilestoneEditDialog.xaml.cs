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
            Milestone.Title = TitleInput.Text;
            Milestone.Version = VersionInput.Text;
            Milestone.Description = DescriptionInput.Text;
            Milestone.StartDate = StartDateInput.Text;
            Milestone.TargetDate = TargetDateInput.Text;
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
