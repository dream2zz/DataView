# DataView

DataView 是一个基于 Avalonia UI 的高性能数据表格控件演示项目，支持大规模数据的可视化展示与滚动。

适用于 .NET 9 桌面应用。

## 功能特性

- 支持百万级数据的高效渲染与滚动
- 可自定义列头、行头
- 支持多种数据源类型：二维数组、数据提供者接口、对象集合
- 具备横纵滚动条，表头与行头固定
- 采用 MVVM 架构，易于扩展

## 安装与依赖

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Avalonia UI](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)

## 运行方式

```sh
dotnet build
dotnet run --project DataView/DataView.csproj
```

## 示例

主窗口展示三种数据源：

1. **二维数组**：`DataSource1`，适合大规模数值型数据
2. **数据提供者接口**：`DataSource2`，通过 `BasicDataProvider` 封装
3. **对象集合**：`DataSource3`，如 `Person` 类型列表

## 代码片段

```csharp
// 绑定示例
<local:DataView ItemsSource="{Binding DataSource1}" Columns="{Binding Columns}" RowHeaders="{Binding RowHeaders}" />
<local:DataView ItemsSource="{Binding DataSource2}" />
<local:DataView ItemsSource="{Binding DataSource3}" Columns="{Binding Columns3}" />
```

## 贡献

欢迎提交 Issue 和 PR，完善功能或修复问题。

## License

MIT
