// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Demo;
using Friflo.EcGui;
using SDL;
using SDL3ImGui;
using static SDL.SDL3;

namespace SDL3.Lab;

public sealed unsafe class MyWindow : IDisposable
{
    private bool flash;
    private ObjectHandle<MyWindow> objectHandle { get; }
    internal SDL_Window* sdlWindowHandle;
    private SDL_Renderer* renderer;
    internal SDL_GPUDevice* gpuDevice;
    private readonly bool initSuccess;

    private const SDL_InitFlags init_flags = SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_GAMEPAD;

    public MyWindow()
    {
        if (!SDL_InitSubSystem(init_flags))
            throw new InvalidOperationException($"failed to initialise SDL. Error: {SDL_GetError()}");

        initSuccess = true;

        objectHandle = new ObjectHandle<MyWindow>(this, GCHandleType.Normal);
    }

    public void Setup()
    {
        SDL_SetGamepadEventsEnabled(true);
        SDL_SetEventFilter(&nativeFilter, objectHandle.Handle);

        if (OperatingSystem.IsWindows())
            SDL_SetWindowsMessageHook(&wndProc, objectHandle.Handle);
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static SDLBool wndProc(IntPtr userdata, MSG* message)
    {
        var handle = new ObjectHandle<MyWindow>(userdata);

        if (handle.GetTarget(out var window))
        {
            Console.WriteLine($"from {window}, message: {message->message}");
        }

        return true;
    }

    // ReSharper disable once UseCollectionExpression
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static SDLBool nativeFilter(IntPtr userdata, SDL_Event* e)
    {
        var handle = new ObjectHandle<MyWindow>(userdata);
        if (handle.GetTarget(out var window))
            return window.handleEventFromFilter(e);

        return true;
    }

    public Action<SDL_Event> EventFilter;

    private bool handleEventFromFilter(SDL_Event* e)
    {
        switch (e->Type)
        {
            case SDL_EventType.SDL_EVENT_KEY_UP:
            case SDL_EventType.SDL_EVENT_KEY_DOWN:
                handleKeyFromFilter(e->key);
                break;

            default:
                EventFilter?.Invoke(*e);
                break;
        }

        return true;
    }

    private void handleKeyFromFilter(SDL_KeyboardEvent e)
    {
        if (e.key == SDL_Keycode.SDLK_F)
        {
            flash = true;
        }
    }

    public void Create()
    {
        var appWidth  = 3200;
        var appHeight = 1600;
        sdlWindowHandle = SDL_CreateWindow("friflo EcGui - SDL3 GPU"u8, appWidth, appHeight, SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY);
        // renderer = SDL_CreateRenderer(sdlWindowHandle, (Utf8String)null);
    }
    
    public void CreateRenderer()
    {
        renderer = SDL_CreateRenderer(sdlWindowHandle, "main");
    }

    private void handleEvent(SDL_Event e)
    {
        if (SDL3ImGuiTools.ProcessEvent(e)) { // --- ImGui integration
            return;
        }
        switch (e.Type)
        {
            case SDL_EventType.SDL_EVENT_QUIT:
                run = false;
                break;

            case SDL_EventType.SDL_EVENT_KEY_DOWN:
                switch (e.key.key)
                {
                    case SDL_Keycode.SDLK_R:
                        bool old = SDL_GetWindowRelativeMouseMode(sdlWindowHandle);
                        SDL_SetWindowRelativeMouseMode(sdlWindowHandle, !old);
                        break;

                    case SDL_Keycode.SDLK_V:
                        string text = SDL_GetClipboardText();
                        Console.WriteLine($"clipboard: {text}");
                        break;

                    case SDL_Keycode.SDLK_F10:
                        SDL_SetWindowFullscreen(sdlWindowHandle, false);
                        break;

                    case SDL_Keycode.SDLK_F11:
                        SDL_SetWindowFullscreen(sdlWindowHandle, true);
                        break;

                    case SDL_Keycode.SDLK_J:
                    {
                        using var gamepads = SDL_GetGamepads();

                        if (gamepads == null || gamepads.Count == 0)
                            break;

                        var gamepad = SDL_OpenGamepad(gamepads[0]);

                        int count;
                        var bindings = SDL_GetGamepadBindings(gamepad, &count);

                        for (int i = 0; i < count; i++)
                        {
                            var binding = *bindings[i];
                            Console.WriteLine(binding.input_type);
                            Console.WriteLine(binding.output_type);
                            Console.WriteLine();
                        }

                        SDL_CloseGamepad(gamepad);
                        break;
                    }

                    case SDL_Keycode.SDLK_F1:
                        SDL_StartTextInput(sdlWindowHandle);
                        break;

                    case SDL_Keycode.SDLK_F2:
                        SDL_StopTextInput(sdlWindowHandle);
                        break;

                    case SDL_Keycode.SDLK_M:
                        SDL_Keymod mod = e.key.mod;
                        Console.WriteLine(mod);
                        break;
                }

                break;

            case SDL_EventType.SDL_EVENT_TEXT_INPUT:
                Console.WriteLine(e.text.GetText());
                break;

            case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                Console.WriteLine($"gamepad added: {e.gdevice.which}");
                break;

            case SDL_EventType.SDL_EVENT_PEN_PROXIMITY_IN:
                Console.WriteLine($"pen proximity in: {e.pproximity.which}");
                break;
        }
    }

    private bool run = true;

    private const int events_per_peep = 64;
    private readonly SDL_Event[] events = new SDL_Event[events_per_peep];

    private void pollEvents()
    {
        SDL_PumpEvents();

        int eventsRead;

        do
        {
            eventsRead = SDL_PeepEvents(events, SDL_EventAction.SDL_GETEVENT, SDL_EventType.SDL_EVENT_FIRST, SDL_EventType.SDL_EVENT_LAST);
            for (int i = 0; i < eventsRead; i++) {
                handleEvent(events[i]);
            }
        } while (eventsRead == events_per_peep);
    }

    private float frame;

    public void Run()
    {
        while (run)
        {
            if (flash)
            {
                flash = false;
                Console.WriteLine("flash!");
            }

            pollEvents();
            
            DemoECS.Update(); // call your ECS simulation here
            
			// --- ImGui integration (begin)
            SDL3ImGuiTools.NewFrame();
            
            EcGui.HistorySnapshot();     // optional - required to show histories
            EcGui.ExplorerWindow();
            EcGui.InspectorWindow();
            // ImGui.ShowDemoWindow();
            
            // Rendering
            SDL3ImGuiTools.Render(sdlWindowHandle, gpuDevice);
            // --- ImGui integration (end)

            // --- original rendering
            // SDL_SetRenderDrawColorFloat(renderer, SDL_sinf(frame) / 2 + 0.5f, SDL_cosf(frame) / 2 + 0.5f, 0.3f, 1.0f);
            // SDL_RenderClear(renderer);
            
            SDL_RenderPresent(renderer); 

            frame += 0.015f;

            // Thread.Sleep(10);
        }
    }

    public void Dispose()
    {
        if (initSuccess)
            SDL_QuitSubSystem(init_flags);

        objectHandle.Dispose();
    }
}

