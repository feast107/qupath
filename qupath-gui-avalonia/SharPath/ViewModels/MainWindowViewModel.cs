using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace SharPath.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "QuPath (Avalonia Shell)";

    [ObservableProperty]
    private string _backendStatus = "IKVM: ???";

    [ObservableProperty]
    private string _backendVersion = "";

    [ObservableProperty]
    private string _imagePath = string.Empty;

    public MainWindowViewModel()
    {
        try
        {
            // 保持原有 IKVM 检查逻辑（若存在）
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "qupath.bridge", StringComparison.OrdinalIgnoreCase))
                      ?? Assembly.Load("qupath.bridge");

            var types = asm.GetTypes().OrderBy(x => x.FullName);
            var type = asm
                .GetTypes()
                .FirstOrDefault(t => t.Name == "QuPathBridgeApi" || t.FullName?.EndsWith(".QuPathBridgeApi") == true);

            if (type != null)
            {
                var method = type.GetMethod("getQuPathVersion", BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    var v = method.Invoke(null, null) as string ?? "unknown";
                    BackendStatus = "IKVM: ???";
                    BackendVersion = $"QuPath {v}";
                }
            }
        }
        catch (Exception e)
        {
            BackendStatus = "IKVM: ????";
            BackendVersion = e.Message;
        }
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        // 打开本地图像文件（用于演示切片/瓦片渲染）
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var window = lifetime?.MainWindow;
            if (window == null)
                return;
            
            var ofd = new OpenFileDialog();
            ofd.AllowMultiple = false;
            ofd.Filters.Add(new FileDialogFilter { Name = "Images", Extensions = { "png", "jpg", "jpeg", "tif", "tiff", "bmp" } });
            var res = await ofd.ShowAsync(window);
            if (res?.Length > 0)
            {
                var selected = res[0];
                // Try to upload to gRPC backend if available
                try
                {
                    // Backend address - adjust as needed or make configurable
                    var backend = "http://localhost:50051";
                    using var client = new SharPath.Services.GrpcImageClient(backend);
                    var slideId = await client.ImportSlideAsync(selected);
                    // Request first tile (0,0) and write to temp file for viewer
                    // Request a small grid of tiles (2x2) and stitch them into a single image
                    int tilesX = 2, tilesY = 2;
                    var images = new List<SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>>();
                    for (int iy = 0; iy < tilesY; iy++)
                    {
                        for (int ix = 0; ix < tilesX; ix++)
                        {
                            await foreach (var chunk in client.StreamTileAsync(slideId, 0, ix, iy))
                            {
                                var bytes = chunk.Data.ToByteArray();
                                using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes);
                                images.Add(img.Clone());
                                break; // only one chunk expected per tile
                            }
                        }
                    }
                    if (images.Count > 0)
                    {
                        int tileW = images[0].Width;
                        int tileH = images[0].Height;
                        int outW = tileW * tilesX;
                        int outH = tileH * tilesY;
                        using var outImg = new SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(outW, outH);
                        for (int iy = 0; iy < tilesY; iy++)
                        {
                            for (int ix = 0; ix < tilesX; ix++)
                            {
                                int idx = iy * tilesX + ix;
                                if (idx >= images.Count) break;
                                outImg.Mutate(ctx => ctx.DrawImage(images[idx], new SixLabors.ImageSharp.Point(ix * tileW, iy * tileH), 1f));
                            }
                        }
                        var tmp = Path.Combine(Path.GetTempPath(), $"qupath_tiles_{Guid.NewGuid()}.png");
                        outImg.Save(tmp);
                        ImagePath = tmp;
                    }
                }
                catch
                {
                    // Fallback: show local file directly
                    ImagePath = selected;
                }
            }
        }
        catch (Exception)
        {
            // ignore for now
        }
    }

    [RelayCommand]
    private void Exit()
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            lifetime?.Shutdown();
        }
        catch { }
    }
}

