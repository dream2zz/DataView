请用 C# 和 Avalonia，从零开始实现一个自定义的 DataGrid 控件。要求：
- 不使用 Avalonia 自带的 DataGrid 控件。
- 仅使用 Avalonia.Media.DrawingContext 进行所有绘制操作。
- 支持基本的数据展示（行、列、单元格）。
- 不需要行选中和单元格点击事件。
- 必须支持虚拟化（只渲染可见区域的数据行和单元格）。
- 结合 Copilot 智能体能力，在性能方面做多种优化，包括：内存占用、渲染效率、滚动流畅性、大数据量下的响应速度。可智能分析和推荐最佳实践，如异步加载、分块渲染、对象池等。
- 代码需包含控件类定义、绘制逻辑、数据绑定示例，以及如何在 XAML 中使用该控件。

帮我集成到当前项目中，并提供完整的代码示例和使用说明。

# 自定义虚拟化 DataGrid 控件集成说明

本项目集成了一个基于 Avalonia 的自定义虚拟化 DataGrid 控件，支持高性能大数据展示。

## 主要特性
- 仅使用 Avalonia.Media.DrawingContext 绘制，未用 Avalonia 自带 DataGrid。
- 支持行、列、单元格展示。
- 完全虚拟化，只渲染可见区域。
- 性能优化建议：对象池、异步加载、分块渲染等。

## 代码结构
- `Controls/VirtualDataGrid.cs`：自定义控件实现。
- `ViewModels/MainWindowViewModel.cs`：示例数据与列名。
- `Views/MainWindow.axaml`：控件 XAML 集成与数据绑定。

## 使用方法
1. 在 `MainWindowViewModel.cs` 中定义数据集合和列名：
   ```csharp
   public ObservableCollection<Person> People { get; }
   public IList<string> Columns { get; }
   ```
2. 在 `MainWindow.axaml` 中添加控件：
   ```xml
   <local:VirtualDataGrid ItemsSource="{Binding People}" Columns="{Binding Columns}" RowHeight="32" ColumnWidth="120" Height="350"/>
   ```
3. 运行项目，即可体验高性能虚拟化 DataGrid。

## 性能优化建议
- 仅渲染可见区域，减少内存和 CPU 占用。
- 可扩展对象池缓存、异步加载、分块渲染等高级优化。
- 支持大数据量流畅滚动。

如需进一步扩展功能（如分页、异步数据源等），可在控件基础上继续开发。