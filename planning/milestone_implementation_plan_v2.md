# Implementation Plan - Milestone Management

This plan outlines the steps to add Milestone support to GamePrince, allowing users to group tasks and track progress by version.

## Proposed Changes

### [Component] Data Layer

#### [MODIFY] [DataService.cs](file:///d:/Data/EXE/DataService.cs)
- Add `Milestone` class:
  ```csharp
  public class Milestone {
      public string Id { get; set; } = Guid.NewGuid().ToString();
      public string Title { get; set; } = "";
      public string Description { get; set; } = "";
      public string Version { get; set; } = "";
      public string StartDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
      public string TargetDate { get; set; } = "";
      public bool IsCompleted { get; set; } = false;
  }
  ```
- Update `TaskItem`: Add `public string MilestoneId { get; set; } = "";`
- Add methods:
  - `public static List<Milestone> LoadMilestones()`
  - `public static void SaveMilestones(List<Milestone> milestones)`

### [Component] UI - Main Window

#### [MODIFY] [MainWindow.xaml](file:///d:/Data/EXE/MainWindow.xaml)
- Add a hidden/collapsed view for the Milestone List. 
- Update sidebar buttons to toggle between Kanban, Heatmap, and the new Milestone view.

#### [MODIFY] [MainWindow.xaml.cs](file:///d:/Data/EXE/MainWindow.xaml.cs)
- Add `_milestones` list.
- Implement UI generation for milestone cards.
- Update `ShowPlan` to actually show the milestone view.

### [Component] UI - Task Edit

#### [MODIFY] [TaskEditDialog.xaml](file:///d:/Data/EXE/TaskEditDialog.xaml)
- Add a `ComboBox` for milestone selection.

#### [MODIFY] [TaskEditDialog.xaml.cs](file:///d:/Data/EXE/TaskEditDialog.xaml.cs)
- Populate the ComboBox with available milestones.
- Update the `Task` object with the selected `MilestoneId`.

## Verification Plan

### Manual Verification
1. **Milestone Creation**: Open "开发计划", create a milestone "v1.0".
2. **Task Linking**: Edit a task, select "v1.0" from the dropdown, save.
3. **Persistence**: Restart the app, ensure the task is still linked to "v1.0".
4. **Filtering**: (Optional) Filter Kanban tasks by milestone.
