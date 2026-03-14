#:package Sdl3Sharp@0.0.1-test9

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Sdl3Sharp;
using Sdl3Sharp.Events;
using Sdl3Sharp.IO;
using Sdl3Sharp.Video;
using Sdl3Sharp.Video.Rendering;

using var sdl = new Sdl(builder => builder
    .SetAppName("")
    .InitializeSubSystems(SubSystems.Video)
);

file sealed class App : AppBase
{
    protected override AppResult OnInitialize(Sdl sdl, string[] args)
    {
        // Do your initialization here (e.g. create 'Window's and 'Renderer's)
        // Fail early with 'Failure' if something goes wrong

        return Continue;
    }

    protected override AppResult OnIterate(Sdl sdl)
    {
        // Do your per-frame logic here
        // If you use a 'Renderer', don't forget to call 'TryRenderPresent' at the end of the frame

        return Continue;
    }

    protected override AppResult OnEvent(Sdl sdl, ref Event @event)
    {
        // Handle your event here
        // You might want to use the '@event.TryAsReadOnly<>' to check and convert to specific event types and/or use '@event.Type'
        // Do forget to handle 'EventType.WindowCloseRequested' and return 'Success' to quit out of the app

        return Continue;
    }

    protected override void OnQuit(Sdl sdl, AppResult result)
    {
        // Do your cleanup here (e.g. dispose of 'Window's and 'Renderer's)
    }
}

file static class Extensions
{
    private static readonly Assembly mProgramAssembly = Assembly.GetAssembly(typeof(Program))!;
    private static readonly string mProgramAssemblyName = mProgramAssembly.GetName().Name!;

    extension(Renderer renderer)
    {
        public bool TryLoadEmbeddedTexture(string resourceName, [NotNullWhen(true)] out Texture? texture)
        {
            texture = null;

            var resourceNameSpan = resourceName.AsSpan().Trim();

            bool isBmp;
            if (resourceNameSpan.IsWhiteSpace()
                || !((isBmp = resourceNameSpan.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                    || resourceNameSpan.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
            
            if (mProgramAssembly.GetManifestResourceStream($"{mProgramAssemblyName}.{resourceNameSpan}") is not {} resourceStream)
            {
                return false;
            }

            using var resourceSdlStream = resourceStream.ToSdlStream(leaveOpen: false);
            
            Surface surface;
            if (isBmp)
            {
                if (!Surface.TryLoadBmp(resourceSdlStream, out surface!))
                {
                    return false;
                }
            }
            else
            {
                if (!Surface.TryLoadPng(resourceSdlStream, out surface!))
                {
                    return false;
                }
            }

            using (surface)
            {
                if (!renderer.TryCreateTextureFromSurface(surface, out texture!))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
