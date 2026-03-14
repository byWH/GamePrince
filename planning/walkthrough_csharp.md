# C# WPF 版开发助手 - 项目总结

按照你的要求，我们放弃了复杂的前端+后端模式，改用了最纯粹、性能最高的 **Windows 原生 C# WPF** 方案。

## 方案优势
1.  **纯粹**：只有 C# (逻辑) 和 XAML (界面)。没有 Node.js，没有 Rust，没有 Localhost。
2.  **本地化**：数据直接读写本地的 `tasks.json`。
3.  **高性能**：直接调用 Windows 系统组件，内存占用极低。
4.  **打包简单**：可以一键生成单个 `.exe` 文件。

## 已实现功能
- **现代 UI**：使用了深蓝配色、圆角设计和阴影效果，保留了高级感。
- **任务看板**：支持“待处理”和“进行中”状态展示。
- **活跃热力图**：全自动生成的 365 天开发热力图组件。
- **本地存储**：已集成 `DataService.cs`，你的任务数据会自动存入同目录下的 JSON 文件。

---

## 如何运行与打包

### 1. 直接运行 (开发预览)
在终端输入：
```powershell
dotnet run
```

### 2. 生成单个 EXE (打包直接用)
运行以下命令，它会把所有内容打包进一个独立、绿色、单文件的 `.exe` 中：
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:IncludeNativeLibrariesForSelfExtract=true
```
**打包后的文件位置**：
`D:\Data\EXE\bin\Release\net10.0-windows\win-x64\publish\GamePrince.exe`
你可以直接把这个 `.exe` 传给任何人使用，无需安装任何环境。

## 文件清单
- [GamePrince.csproj](file:///d:/Data/EXE/GamePrince.csproj): 项目配置文件。
- [MainWindow.xaml](file:///d:/Data/EXE/MainWindow.xaml): 视觉界面定义。
- [MainWindow.xaml.cs](file:///d:/Data/EXE/MainWindow.xaml.cs): 交互逻辑。
- [DataService.cs](file:///d:/Data/EXE/DataService.cs): 本地数据保存逻辑。
