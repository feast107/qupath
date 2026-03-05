using Avalonia;
using SharPath;

BuildAvaloniaApp()
    .StartWithClassicDesktopLifetime(args);

static AppBuilder BuildAvaloniaApp()
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .LogToTrace();