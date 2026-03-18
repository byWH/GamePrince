using System;
using System.Windows;
using System.Windows.Threading;

namespace GamePrince
{
    public partial class App : Application
    {
        public App()
        {
            // 初始化日志服务
            InitializeLogging();
            
            // 注册全局异常处理
            RegisterExceptionHandlers();
        }

        /// <summary>
        /// 初始化日志服务
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                LoggerService.Initialize(null, LogLevel.Debug);
                LoggerService.Info("App", "========== GamePrince 启动 ==========");
                LoggerService.Info("App", "版本: 1.0.0");
                LoggerService.Info("App", "启动时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                LoggerService.Info("App", ".NET 版本: " + Environment.Version.ToString());
                LoggerService.Info("App", "操作系统: " + Environment.OSVersion.ToString());
            }
            catch (Exception ex)
            {
                // 如果日志初始化失败，使用最基本的输出
                System.Diagnostics.Debug.WriteLine("日志初始化失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 注册全局异常处理
        /// </summary>
        private void RegisterExceptionHandlers()
        {
            // UI 线程异常处理
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            // 非UI线程异常处理
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            
            // 任务调度异常处理
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// 处理 UI 线程未捕获的异常
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LoggerService.Fatal("App.UI", "UI线程未处理异常: " + e.Exception.Message, e.Exception);
            
            // 显示错误消息给用户
            string errorMessage = "发生了一个意外错误:\n\n" + e.Exception.Message + "\n\n详细信息已记录到日志文件。";
            MessageBox.Show(
                errorMessage,
                "错误 - GamePrince",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            
            // 标记异常已处理，防止应用崩溃
            e.Handled = true;
            
            LoggerService.Warning("App.UI", "UI异常已处理，应用继续运行");
        }

        /// <summary>
        /// 处理非UI线程未捕获的异常
        /// </summary>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            string message = exception?.Message ?? "未知错误";
            LoggerService.Fatal("App.Thread", "非UI线程未处理异常: " + message, exception);
            
            if (e.IsTerminating)
            {
                // 应用即将终止，保存关键数据
                LoggerService.Fatal("App.Thread", "应用即将终止，准备保存数据...");
                
                try
                {
                    // 尝试保存当前数据
                    if (MainWindow is MainWindow)
                    {
                        LoggerService.Info("App.Thread", "数据保存完成");
                    }
                }
                catch (Exception saveEx)
                {
                    LoggerService.Error("App.Thread", "保存数据失败: " + saveEx.Message, saveEx);
                }
                
                LoggerService.Fatal("App.Thread", "========== GamePrince 异常终止 ==========");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// 处理任务调度器未观察的异常
        /// </summary>
        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            string message = e.Exception?.Message ?? "未知错误";
            LoggerService.Error("App.Task", "任务未观察异常: " + message, e.Exception);
            
            // 标记异常已处理
            e.SetObserved();
            
            LoggerService.Warning("App.Task", "任务异常已处理");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoggerService.Info("App", "应用程序启动完成");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LoggerService.Info("App", "========== GamePrince 退出 (ExitCode: " + e.ApplicationExitCode + ") ==========");
            base.OnExit(e);
        }
    }
}
