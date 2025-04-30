
using System;
using System.Diagnostics;
using System.IO;
using Demo;
using Friflo.EcGui;
using ImGuiNET;
using SDL;
using SDL3ImGui;
using static SDL.SDL3;

namespace SDL3.Lab;

static class Program
{
    public static unsafe void Main()
    {
        if (OperatingSystem.IsWindows()) {
            // Console.OutputEncoding = Encoding.UTF8;      // requires: <OutputType>Exe</OutputType>  in *.csproj
        }
        Console.WriteLine($"false is represented as {SDL_OutOfMemory()} (expected 0x{0:x2})");
        Console.WriteLine($"true  is represented as {SDL_ClearError()} (expected 0x{1:x2})");
        _ = SDL_PROP_WINDOW_WIN32_HWND_POINTER;
        SDL_SetHint(SDL_HINT_WINDOWS_CLOSE_ON_ALT_F4, "null byte \0 in string"u8);
        Debug.Assert(SDL_GetHint(SDL_HINT_WINDOWS_CLOSE_ON_ALT_F4) == "null byte ");

        SDL_SetHint(SDL_HINT_WINDOWS_CLOSE_ON_ALT_F4, "1"u8);
        SDL_SetHint(SDL_HINT_WINDOWS_CLOSE_ON_ALT_F4, "1");

        using (var window = new MyWindow())
        {
            Console.WriteLine($"SDL revision: {SDL_GetRevision()}");

            printDisplays();

            window.Setup();
            window.Create();
            
            // --- ImGui integration (begin)
            ImGui.CreateContext();
            var io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;       // Enable navigation with arrow keys
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;        // Enable navigation GamePad controller
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            // io.WantCaptureMouse = true;                              // Enables dragging values
            // io.FontGlobalScale = 3;
            
            io.Fonts.AddFontFromFileTTF(Path.Combine(AppContext.BaseDirectory, "Content", "Inter-Regular.ttf"), 40);
            io.Fonts.AddFontDefault(); // is called later in: ImGui/examples/example_sdl3_sdlgpu3/main.cpp 
            io.Fonts.Build();
            
            ImGui.StyleColorsLight();
            EcGui.Setup.SetDefaultStyles(); // optional
            
            var gpuDevice = SDL3ImGuiTools.CreateGpuDevice(window.sdlWindowHandle);
            SDL3ImGuiTools.InitGpuDevice(window.sdlWindowHandle, gpuDevice);
            window.gpuDevice = gpuDevice;
            
            // io.Fonts.AddFontDefault();   fails if calling here
            // io.Fonts.Build();
            
            window.CreateRenderer(); // was originally called in window.Create()
            // --- ImGui integration (end)
            DemoECS.CreateEntityStore();    // set up your ECS here
            DemoECS.CustomizeEcGui();       // customize UI

            printWindows();

            const SDL_Keymod state = SDL_Keymod.SDL_KMOD_CAPS | SDL_Keymod.SDL_KMOD_ALT;
            SDL_SetModState(state);
            Debug.Assert(SDL_GetModState() == state);

            window.Run();
        }

        SDL_Quit();
    }

    private static void printDisplays()
    {
        using var displays = SDL_GetDisplays();
        if (displays == null)
            return;

        for (int i = 0; i < displays.Count; i++)
        {
            SDL_DisplayID id = displays[i];
            Console.WriteLine(id);

            using var modes = SDL_GetFullscreenDisplayModes(id);
            if (modes == null)
                continue;

            for (int j = 0; j < modes.Count; j++)
            {
                SDL_DisplayMode mode = modes[j];
                Console.WriteLine($"{mode.w}x{mode.h}@{mode.refresh_rate}");
            }
        }
    }

    private static unsafe void printWindows()
    {
        using var windows = SDL_GetWindows();
        if (windows == null)
            return;

        for (int i = 0; i < windows.Count; i++)
        {
            Console.WriteLine($"Window {i} title: {SDL_GetWindowTitle(windows[i])}");
        }
    }
}
