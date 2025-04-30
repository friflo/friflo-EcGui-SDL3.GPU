// port from:  https://github.com/ocornut/imgui/blob/master/backends/imgui_impl_sdl3.cpp

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using SDL;

using static SDL.SDL3;
using static SDL.SDL_EventType;
using static SDL.SDL_SystemCursor;
using static SDL.SDL_Scancode;
using static SDL.SDL_GamepadButton;
using static SDL.SDL_GamepadAxis;
using static SDL3ImGui.ImGuiUtils;


using Uint64 = System.UInt64;
using ImGuiContext = System.IntPtr;
// ImGuiContext - function signatures at: https://github.com/ocornut/imgui
// void        (*Platform_SetImeDataFn)(ImGuiContext* ctx, ImGuiViewport* viewport, ImGuiPlatformImeData* data);
// const char* (*Platform_GetClipboardTextFn)(ImGuiContext* ctx);
// void        (*Platform_SetClipboardTextFn)(ImGuiContext* ctx, const char* text);
// bool        (*Platform_OpenInShellFn)(ImGuiContext* ctx, const char* path);

// dear imgui: Platform Backend for SDL3
// This needs to be used along with a Renderer (e.g. SDL_GPU, DirectX11, OpenGL3, Vulkan..)
// (Info: SDL3 is a cross-platform general purpose library for handling windows, inputs, graphics context creation, etc.)

// Implemented features:
//  [X] Platform: Clipboard support.
//  [X] Platform: Mouse support. Can discriminate Mouse/TouchScreen.
//  [X] Platform: Keyboard support. Since 1.87 we are using the io.AddKeyEvent() function. Pass ImGuiKey values to all key functions e.g. ImGui::IsKeyPressed(ImGuiKey_Space). [Legacy SDL_SCANCODE_* values are obsolete since 1.87 and not supported since 1.91.5]
//  [X] Platform: Gamepad support.
//  [X] Platform: Mouse cursor shape and visibility (ImGuiBackendFlags_HasMouseCursors). Disable with 'io.ConfigFlags |= ImGuiConfigFlags_NoMouseCursorChange'.
//  [X] Platform: IME support.

// You can use unmodified imgui_impl_* files in your project. See examples/ folder for examples of using this.
// Prefer including the entire imgui/ repository into your project (either as a copy or as a submodule), and only build the backends you need.
// Learn about Dear ImGui:
// - FAQ                  https://dearimgui.com/faq
// - Getting Started      https://dearimgui.com/getting-started
// - Documentation        https://dearimgui.com/docs (same as your local docs/ folder).
// - Introduction, links and more at the top of imgui.cpp

// CHANGELOG
// (minor and older changes stripped away, please see git history for details)
//  2025-04-09: Don't attempt to call SDL_CaptureMouse() on drivers where we don't call SDL_GetGlobalMouseState(). (#8561)
//  2025-03-30: Update for SDL3 api changes: Revert SDL_GetClipboardText() memory ownership change. (#8530, #7801)
//  2025-03-21: Fill gamepad inputs and set ImGuiBackendFlags_HasGamepad regardless of ImGuiConfigFlags_NavEnableGamepad being set.
//  2025-03-10: When dealing with OEM keys, use scancodes instead of translated keycodes to choose ImGuiKey values. (#7136, #7201, #7206, #7306, #7670, #7672, #8468)
//  2025-02-26: Only start SDL_CaptureMouse() when mouse is being dragged, to mitigate issues with e.g.Linux debuggers not claiming capture back. (#6410, #3650)
//  2025-02-24: Avoid calling SDL_GetGlobalMouseState() when mouse is in relative mode.
//  2025-02-18: Added ImGuiMouseCursor_Wait and ImGuiMouseCursor_Progress mouse cursor support.
//  2025-02-10: Using SDL_OpenURL() in platform_io.Platform_OpenInShellFn handler.
//  2025-01-20: Made ImGui_ImplSDL3_SetGamepadMode(ImGui_ImplSDL3_GamepadMode_Manual) accept an empty array.
//  2024-10-24: Emscripten: SDL_EVENT_MOUSE_WHEEL event doesn't require dividing by 100.0f on Emscripten.
//  2024-09-03: Update for SDL3 api changes: SDL_GetGamepads() memory ownership revert. (#7918, #7898, #7807)
//  2024-08-22: moved some OS/backend related function pointers from ImGuiIO to ImGuiPlatformIO:
//               - io.GetClipboardTextFn    -> platform_io.Platform_GetClipboardTextFn
//               - io.SetClipboardTextFn    -> platform_io.Platform_SetClipboardTextFn
//               - io.PlatformSetImeDataFn  -> platform_io.Platform_SetImeDataFn
//  2024-08-19: Storing SDL_WindowID inside ImGuiViewport::PlatformHandle instead of SDL_Window*.
//  2024-08-19: ImGui_ImplSDL3_ProcessEvent() now ignores events intended for other SDL windows. (#7853)
//  2024-07-22: Update for SDL3 api changes: SDL_GetGamepads() memory ownership change. (#7807)
//  2024-07-18: Update for SDL3 api changes: SDL_GetClipboardText() memory ownership change. (#7801)
//  2024-07-15: Update for SDL3 api changes: SDL_GetProperty() change to SDL_GetPointerProperty(). (#7794)
//  2024-07-02: Update for SDL3 api changes: SDLK_x renames and SDLK_KP_x removals (#7761, #7762).
//  2024-07-01: Update for SDL3 api changes: SDL_SetTextInputRect() changed to SDL_SetTextInputArea().
//  2024-06-26: Update for SDL3 api changes: SDL_StartTextInput()/SDL_StopTextInput()/SDL_SetTextInputRect() functions signatures.
//  2024-06-24: Update for SDL3 api changes: SDL_EVENT_KEY_DOWN/SDL_EVENT_KEY_UP contents.
//  2024-06-03; Update for SDL3 api changes: SDL_SYSTEM_CURSOR_ renames.
//  2024-05-15: Update for SDL3 api changes: SDLK_ renames.
//  2024-04-15: Inputs: Re-enable calling SDL_StartTextInput()/SDL_StopTextInput() as SDL3 no longer enables it by default and should play nicer with IME.
//  2024-02-13: Inputs: Fixed gamepad support. Handle gamepad disconnection. Added ImGui_ImplSDL3_SetGamepadMode().
//  2023-11-13: Updated for recent SDL3 API changes.
//  2023-10-05: Inputs: Added support for extra ImGuiKey values: F13 to F24 function keys, app back/forward keys.
//  2023-05-04: Fixed build on Emscripten/iOS/Android. (#6391)
//  2023-04-06: Inputs: Avoid calling SDL_StartTextInput()/SDL_StopTextInput() as they don't only pertain to IME. It's unclear exactly what their relation is to IME. (#6306)
//  2023-04-04: Inputs: Added support for io.AddMouseSourceEvent() to discriminate ImGuiMouseSource_Mouse/ImGuiMouseSource_TouchScreen. (#2702)
//  2023-02-23: Accept SDL_GetPerformanceCounter() not returning a monotonically increasing value. (#6189, #6114, #3644)
//  2023-02-07: Forked "imgui_impl_sdl2" into "imgui_impl_sdl3". Removed version checks for old feature. Refer to imgui_impl_sdl2.cpp for older changelog.

// ReSharper disable RedundantEmptySwitchSection
// ReSharper disable RedundantCast
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace SDL3ImGui;

public static unsafe class ImGui_ImplSDL3 {
/*
#include "imgui.h"
#ifndef IMGUI_DISABLE
#include "imgui_impl_sdl3.h"

// Clang warnings with -Weverything
#if defined(__clang__)
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wimplicit-int-float-conversion"  // warning: implicit conversion from 'xxx' to 'float' may lose precision
#endif

// SDL
#include <SDL3/SDL.h>
#if defined(__APPLE__)
#include <TargetConditionals.h>
#endif
#ifdef _WIN32
#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <windows.h>
#endif
*/
private static readonly bool SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE =
    !OperatingSystem.IsAndroid() && !(OperatingSystem.IsMacOS() && OperatingSystem.IsIOS());
/*
#if !defined(__EMSCRIPTEN__) && !defined(__ANDROID__) && !(defined(__APPLE__) && TARGET_OS_IOS) && !defined(__amigaos4__)
#define SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE    1
#else
#define SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE    0
#endif

// FIXME-LEGACY: remove when SDL 3.1.3 preview is released.
#ifndef SDLK_APOSTROPHE
#define SDLK_APOSTROPHE SDLK_QUOTE
#endif
#ifndef SDLK_GRAVE
#define SDLK_GRAVE SDLK_BACKQUOTE
#endif
*/
    
enum ImGui_ImplSDL3_GamepadMode { AutoFirst, AutoAll, Manual };

// SDL Data
private struct ImGui_ImplSDL3_Data
{
    internal SDL_Window*                Window;
    internal SDL_WindowID               WindowID;
    internal SDL_Renderer*              Renderer;
    internal Uint64                     Time;
    internal byte*                      ClipboardTextData;

    // IME handling
    internal SDL_Window*                ImeWindow;

    // Mouse handling
    internal SDL_WindowID               MouseWindowID;
    internal int                        MouseButtonsDown;
    internal Cursors                    MouseCursors; // fixed [] - element type: SDL_Cursor*
    internal SDL_Cursor*                MouseLastCursor;
    internal int                        MousePendingLeaveFrame;
    internal bool                       MouseCanUseGlobalState;
    internal bool                       MouseCanUseCapture;

    // Gamepad handling
    internal Gamepads                   Gamepads; // // fixed [10] - element type: SDL_Gamepad*
    internal int                        GamepadCount;
    internal ImGui_ImplSDL3_GamepadMode GamepadMode;
    internal bool                       WantUpdateGamepadsList;

//  ImGui_ImplSDL3_Data()   { memset((void*)this, 0, sizeof(*this)); } - used AllocZeroed() at allocation
};

// Backend data stored in io.BackendPlatformUserData to allow support for multiple Dear ImGui contexts
// It is STRONGLY preferred that you use docking branch with multi-viewports (== single Dear ImGui context + multiple windows) instead of multiple Dear ImGui contexts.
// FIXME: multi-context support is not well tested and probably dysfunctional in this backend.
// FIXME: some shared resources (mouse cursor shape, gamepad) are mishandled when using multi-context.
static ImGui_ImplSDL3_Data* ImGui_ImplSDL3_GetBackendData()
{
    return ImGui.GetCurrentContext() != 0 ? (ImGui_ImplSDL3_Data*)ImGui.GetIO().BackendPlatformUserData : null;
}

// Functions
static byte* ImGui_ImplSDL3_GetClipboardText(ImGuiContext ctx)
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    if (bd->ClipboardTextData != null)
        SDL_free(bd->ClipboardTextData);
    var sdl_clipboard_text = SDL_GetClipboardText();
    bd->ClipboardTextData = sdl_clipboard_text != null ? SDL_strdup(sdl_clipboard_text) : null;
    return bd->ClipboardTextData;
}

static void ImGui_ImplSDL3_SetClipboardText(ImGuiContext ctx, byte* text)
{
    SDL_SetClipboardText(text);
}

static void ImGui_ImplSDL3_PlatformSetImeData(ImGuiContext ctx, ImGuiViewport* viewport, ImGuiPlatformImeData* data)
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    SDL_WindowID window_id = (SDL_WindowID)(IntPtr)viewport->PlatformHandle;
    SDL_Window* window = SDL_GetWindowFromID(window_id);
    if ((data->WantVisible == 0 || bd->ImeWindow != window) && bd->ImeWindow != null)
    {
        SDL_StopTextInput(bd->ImeWindow);
        bd->ImeWindow = null;
    }
    if (data->WantVisible != 0)
    {
        SDL_Rect r;
        r.x = (int)data->InputPos.X;
        r.y = (int)data->InputPos.Y;
        r.w = 1;
        r.h = (int)data->InputLineHeight;
        SDL_SetTextInputArea(window, &r, 0);
        SDL_StartTextInput(window);
        bd->ImeWindow = window;
    }
}

// Not static to allow third-party code to use that if they want to (but undocumented)
// ImGuiKey ImGui_ImplSDL3_KeyEventToImGuiKey(SDL_Keycode keycode, SDL_Scancode scancode);
static ImGuiKey ImGui_ImplSDL3_KeyEventToImGuiKey(SDL_Keycode keycode, SDL_Scancode scancode)
{
    // Keypad doesn't have individual key values in SDL3
    switch (scancode)
    {
        case SDL_SCANCODE_KP_0: return ImGuiKey.Keypad0;
        case SDL_SCANCODE_KP_1: return ImGuiKey.Keypad1;
        case SDL_SCANCODE_KP_2: return ImGuiKey.Keypad2;
        case SDL_SCANCODE_KP_3: return ImGuiKey.Keypad3;
        case SDL_SCANCODE_KP_4: return ImGuiKey.Keypad4;
        case SDL_SCANCODE_KP_5: return ImGuiKey.Keypad5;
        case SDL_SCANCODE_KP_6: return ImGuiKey.Keypad6;
        case SDL_SCANCODE_KP_7: return ImGuiKey.Keypad7;
        case SDL_SCANCODE_KP_8: return ImGuiKey.Keypad8;
        case SDL_SCANCODE_KP_9: return ImGuiKey.Keypad9;
        case SDL_SCANCODE_KP_PERIOD: return ImGuiKey.KeypadDecimal;
        case SDL_SCANCODE_KP_DIVIDE: return ImGuiKey.KeypadDivide;
        case SDL_SCANCODE_KP_MULTIPLY: return ImGuiKey.KeypadMultiply;
        case SDL_SCANCODE_KP_MINUS: return ImGuiKey.KeypadSubtract;
        case SDL_SCANCODE_KP_PLUS: return ImGuiKey.KeypadAdd;
        case SDL_SCANCODE_KP_ENTER: return ImGuiKey.KeypadEnter;
        case SDL_SCANCODE_KP_EQUALS: return ImGuiKey.KeypadEqual;
        default: break;
    }
    switch (keycode)
    {
        case SDL_Keycode.SDLK_TAB: return ImGuiKey.Tab;
        case SDL_Keycode.SDLK_LEFT: return ImGuiKey.LeftArrow;
        case SDL_Keycode.SDLK_RIGHT: return ImGuiKey.RightArrow;
        case SDL_Keycode.SDLK_UP: return ImGuiKey.UpArrow;
        case SDL_Keycode.SDLK_DOWN: return ImGuiKey.DownArrow;
        case SDL_Keycode.SDLK_PAGEUP: return ImGuiKey.PageUp;
        case SDL_Keycode.SDLK_PAGEDOWN: return ImGuiKey.PageDown;
        case SDL_Keycode.SDLK_HOME: return ImGuiKey.Home;
        case SDL_Keycode.SDLK_END: return ImGuiKey.End;
        case SDL_Keycode.SDLK_INSERT: return ImGuiKey.Insert;
        case SDL_Keycode.SDLK_DELETE: return ImGuiKey.Delete;
        case SDL_Keycode.SDLK_BACKSPACE: return ImGuiKey.Backspace;
        case SDL_Keycode.SDLK_SPACE: return ImGuiKey.Space;
        case SDL_Keycode.SDLK_RETURN: return ImGuiKey.Enter;
        case SDL_Keycode.SDLK_ESCAPE: return ImGuiKey.Escape;
        //case SDL_Keycode.SDLK_APOSTROPHE: return ImGuiKey.Apostrophe;
        case SDL_Keycode.SDLK_COMMA: return ImGuiKey.Comma;
        //case SDL_Keycode.SDLK_MINUS: return ImGuiKey.Minus;
        case SDL_Keycode.SDLK_PERIOD: return ImGuiKey.Period;
        //case SDL_Keycode.SDLK_SLASH: return ImGuiKey.Slash;
        case SDL_Keycode.SDLK_SEMICOLON: return ImGuiKey.Semicolon;
        //case SDL_Keycode.SDLK_EQUALS: return ImGuiKey.Equal;
        //case SDL_Keycode.SDLK_LEFTBRACKET: return ImGuiKey.LeftBracket;
        //case SDL_Keycode.SDLK_BACKSLASH: return ImGuiKey.Backslash;
        //case SDL_Keycode.SDLK_RIGHTBRACKET: return ImGuiKey.RightBracket;
        //case SDL_Keycode.SDLK_GRAVE: return ImGuiKey.GraveAccent;
        case SDL_Keycode.SDLK_CAPSLOCK: return ImGuiKey.CapsLock;
        case SDL_Keycode.SDLK_SCROLLLOCK: return ImGuiKey.ScrollLock;
        case SDL_Keycode.SDLK_NUMLOCKCLEAR: return ImGuiKey.NumLock;
        case SDL_Keycode.SDLK_PRINTSCREEN: return ImGuiKey.PrintScreen;
        case SDL_Keycode.SDLK_PAUSE: return ImGuiKey.Pause;
        case SDL_Keycode.SDLK_LCTRL: return ImGuiKey.LeftCtrl;
        case SDL_Keycode.SDLK_LSHIFT: return ImGuiKey.LeftShift;
        case SDL_Keycode.SDLK_LALT: return ImGuiKey.LeftAlt;
        case SDL_Keycode.SDLK_LGUI: return ImGuiKey.LeftSuper;
        case SDL_Keycode.SDLK_RCTRL: return ImGuiKey.RightCtrl;
        case SDL_Keycode.SDLK_RSHIFT: return ImGuiKey.RightShift;
        case SDL_Keycode.SDLK_RALT: return ImGuiKey.RightAlt;
        case SDL_Keycode.SDLK_RGUI: return ImGuiKey.RightSuper;
        case SDL_Keycode.SDLK_APPLICATION: return ImGuiKey.Menu;
        case SDL_Keycode.SDLK_0: return ImGuiKey._0;
        case SDL_Keycode.SDLK_1: return ImGuiKey._1;
        case SDL_Keycode.SDLK_2: return ImGuiKey._2;
        case SDL_Keycode.SDLK_3: return ImGuiKey._3;
        case SDL_Keycode.SDLK_4: return ImGuiKey._4;
        case SDL_Keycode.SDLK_5: return ImGuiKey._5;
        case SDL_Keycode.SDLK_6: return ImGuiKey._6;
        case SDL_Keycode.SDLK_7: return ImGuiKey._7;
        case SDL_Keycode.SDLK_8: return ImGuiKey._8;
        case SDL_Keycode.SDLK_9: return ImGuiKey._9;
        case SDL_Keycode.SDLK_A: return ImGuiKey.A;
        case SDL_Keycode.SDLK_B: return ImGuiKey.B;
        case SDL_Keycode.SDLK_C: return ImGuiKey.C;
        case SDL_Keycode.SDLK_D: return ImGuiKey.D;
        case SDL_Keycode.SDLK_E: return ImGuiKey.E;
        case SDL_Keycode.SDLK_F: return ImGuiKey.F;
        case SDL_Keycode.SDLK_G: return ImGuiKey.G;
        case SDL_Keycode.SDLK_H: return ImGuiKey.H;
        case SDL_Keycode.SDLK_I: return ImGuiKey.I;
        case SDL_Keycode.SDLK_J: return ImGuiKey.J;
        case SDL_Keycode.SDLK_K: return ImGuiKey.K;
        case SDL_Keycode.SDLK_L: return ImGuiKey.L;
        case SDL_Keycode.SDLK_M: return ImGuiKey.M;
        case SDL_Keycode.SDLK_N: return ImGuiKey.N;
        case SDL_Keycode.SDLK_O: return ImGuiKey.O;
        case SDL_Keycode.SDLK_P: return ImGuiKey.P;
        case SDL_Keycode.SDLK_Q: return ImGuiKey.Q;
        case SDL_Keycode.SDLK_R: return ImGuiKey.R;
        case SDL_Keycode.SDLK_S: return ImGuiKey.S;
        case SDL_Keycode.SDLK_T: return ImGuiKey.T;
        case SDL_Keycode.SDLK_U: return ImGuiKey.U;
        case SDL_Keycode.SDLK_V: return ImGuiKey.V;
        case SDL_Keycode.SDLK_W: return ImGuiKey.W;
        case SDL_Keycode.SDLK_X: return ImGuiKey.X;
        case SDL_Keycode.SDLK_Y: return ImGuiKey.Y;
        case SDL_Keycode.SDLK_Z: return ImGuiKey.Z;
        case SDL_Keycode.SDLK_F1: return ImGuiKey.F1;
        case SDL_Keycode.SDLK_F2: return ImGuiKey.F2;
        case SDL_Keycode.SDLK_F3: return ImGuiKey.F3;
        case SDL_Keycode.SDLK_F4: return ImGuiKey.F4;
        case SDL_Keycode.SDLK_F5: return ImGuiKey.F5;
        case SDL_Keycode.SDLK_F6: return ImGuiKey.F6;
        case SDL_Keycode.SDLK_F7: return ImGuiKey.F7;
        case SDL_Keycode.SDLK_F8: return ImGuiKey.F8;
        case SDL_Keycode.SDLK_F9: return ImGuiKey.F9;
        case SDL_Keycode.SDLK_F10: return ImGuiKey.F10;
        case SDL_Keycode.SDLK_F11: return ImGuiKey.F11;
        case SDL_Keycode.SDLK_F12: return ImGuiKey.F12;
        case SDL_Keycode.SDLK_F13: return ImGuiKey.F13;
        case SDL_Keycode.SDLK_F14: return ImGuiKey.F14;
        case SDL_Keycode.SDLK_F15: return ImGuiKey.F15;
        case SDL_Keycode.SDLK_F16: return ImGuiKey.F16;
        case SDL_Keycode.SDLK_F17: return ImGuiKey.F17;
        case SDL_Keycode.SDLK_F18: return ImGuiKey.F18;
        case SDL_Keycode.SDLK_F19: return ImGuiKey.F19;
        case SDL_Keycode.SDLK_F20: return ImGuiKey.F20;
        case SDL_Keycode.SDLK_F21: return ImGuiKey.F21;
        case SDL_Keycode.SDLK_F22: return ImGuiKey.F22;
        case SDL_Keycode.SDLK_F23: return ImGuiKey.F23;
        case SDL_Keycode.SDLK_F24: return ImGuiKey.F24;
        case SDL_Keycode.SDLK_AC_BACK: return ImGuiKey.AppBack;
        case SDL_Keycode.SDLK_AC_FORWARD: return ImGuiKey.AppForward;
        default: break;
    }
    
    // Fallback to scancode
    switch (scancode)
    {
        case SDL_SCANCODE_GRAVE: return ImGuiKey.GraveAccent;
        case SDL_SCANCODE_MINUS: return ImGuiKey.Minus;
        case SDL_SCANCODE_EQUALS: return ImGuiKey.Equal;
        case SDL_SCANCODE_LEFTBRACKET: return ImGuiKey.LeftBracket;
        case SDL_SCANCODE_RIGHTBRACKET: return ImGuiKey.RightBracket;
//      case SDL_SCANCODE_NONUSBACKSLASH: return ImGuiKey.Oem102;
        case SDL_SCANCODE_BACKSLASH: return ImGuiKey.Backslash;
        case SDL_SCANCODE_SEMICOLON: return ImGuiKey.Semicolon;
        case SDL_SCANCODE_APOSTROPHE: return ImGuiKey.Apostrophe;
        case SDL_SCANCODE_COMMA: return ImGuiKey.Comma;
        case SDL_SCANCODE_PERIOD: return ImGuiKey.Period;
        case SDL_SCANCODE_SLASH: return ImGuiKey.Slash;
        default: break;
    }
    return ImGuiKey.None;
}

static void ImGui_ImplSDL3_UpdateKeyModifiers(SDL_Keymod sdl_key_mods)
{
    var io = ImGui.GetIO();
    io.AddKeyEvent(ImGuiKey.ModCtrl, ((uint)sdl_key_mods & SDL_KMOD_CTRL) != 0);
    io.AddKeyEvent(ImGuiKey.ModShift, ((uint)sdl_key_mods & SDL_KMOD_SHIFT) != 0);
    io.AddKeyEvent(ImGuiKey.ModAlt, ((uint)sdl_key_mods & SDL_KMOD_ALT) != 0);
    io.AddKeyEvent(ImGuiKey.ModSuper, ((uint)sdl_key_mods & SDL_KMOD_GUI) != 0);
}


static ImGuiViewport* ImGui_ImplSDL3_GetViewportForWindowID(SDL_WindowID window_id)
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    return (window_id == bd->WindowID) ? ImGui.GetMainViewport() : null;
}

// You can read the io.WantCaptureMouse, io.WantCaptureKeyboard flags to tell if dear imgui wants to use your inputs.
// - When io.WantCaptureMouse is true, do not dispatch mouse input data to your main application, or clear/overwrite your copy of the mouse data.
// - When io.WantCaptureKeyboard is true, do not dispatch keyboard input data to your main application, or clear/overwrite your copy of the keyboard data.
// Generally you may always pass all inputs to dear imgui, and hide them from your application based on those two flags.
// If you have multiple SDL events and some of them are not meant to be used by dear imgui, you may need to filter events based on their windowID field.
public static bool ProcessEvent(SDL_Event* ev)
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    IM_ASSERT(bd != null, "Context or backend not initialized! Did you call ImGui_ImplSDL3_Init()?");
    var io = ImGui.GetIO();

    var eventType = (SDL_EventType)ev->type;
    switch (eventType)
    {
        case SDL_EVENT_MOUSE_MOTION:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->motion.windowID) == null)
                return false;
            Vector2 mouse_pos = new ((float)ev->motion.x, (float)ev->motion.y);
            io.AddMouseSourceEvent(ev->motion.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
            io.AddMousePosEvent(mouse_pos.X, mouse_pos.Y);
            return true;
        }
        case SDL_EVENT_MOUSE_WHEEL:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->wheel.windowID) == null)
                return false;
            //IMGUI_DEBUG_LOG("wheel %.2f %.2f, precise %.2f %.2f\n", (float)ev->wheel.x, (float)ev->wheel.y, ev->wheel.preciseX, ev->wheel.preciseY);
            float wheel_x = -ev->wheel.x;
            float wheel_y = ev->wheel.y;
            io.AddMouseSourceEvent(ev->wheel.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
            io.AddMouseWheelEvent(wheel_x, wheel_y);
            return true;
        }
        case SDL_EVENT_MOUSE_BUTTON_DOWN:
        case SDL_EVENT_MOUSE_BUTTON_UP:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->button.windowID) == null)
                return false;
            int mouse_button = -1;
            if (ev->button.button == SDL_BUTTON_LEFT) { mouse_button = 0; }
            if (ev->button.button == SDL_BUTTON_RIGHT) { mouse_button = 1; }
            if (ev->button.button == SDL_BUTTON_MIDDLE) { mouse_button = 2; }
            if (ev->button.button == SDL_BUTTON_X1) { mouse_button = 3; }
            if (ev->button.button == SDL_BUTTON_X2) { mouse_button = 4; }
            if (mouse_button == -1)
                break;
            io.AddMouseSourceEvent(ev->button.which == SDL_TOUCH_MOUSEID ? ImGuiMouseSource.TouchScreen : ImGuiMouseSource.Mouse);
            io.AddMouseButtonEvent(mouse_button, (eventType == SDL_EVENT_MOUSE_BUTTON_DOWN));
            bd->MouseButtonsDown = (eventType == SDL_EVENT_MOUSE_BUTTON_DOWN) ? (bd->MouseButtonsDown | (1 << mouse_button)) : (bd->MouseButtonsDown & ~(1 << mouse_button));
            return true;
        }
        case SDL_EVENT_TEXT_INPUT:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->text.windowID) == null)
                return false;
            io.AddInputCharactersUTF8(ev->text.GetText());
            return true;
        }
        case SDL_EVENT_KEY_DOWN:
        case SDL_EVENT_KEY_UP:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->key.windowID) == null)
                return false;
            ImGui_ImplSDL3_UpdateKeyModifiers((SDL_Keymod)ev->key.mod);
            //IMGUI_DEBUG_LOG("SDL_EVENT_KEY_%s : key=%d ('%s'), scancode=%d ('%s'), mod=%X\n",
            //    (ev->type == SDL_EVENT_KEY_DOWN) ? "DOWN" : "UP  ", ev->key.key, SDL_GetKeyName(ev->key.key), ev->key.scancode, SDL_GetScancodeName(ev->key.scancode), ev->key.mod);
            ImGuiKey key = ImGui_ImplSDL3_KeyEventToImGuiKey(ev->key.key, ev->key.scancode);
            io.AddKeyEvent(key, (eventType == SDL_EVENT_KEY_DOWN));
            io.SetKeyEventNativeData(key, (int)ev->key.key, (int)ev->key.scancode, (int)ev->key.scancode); // To support legacy indexing (<1.87 user code). Legacy backend uses SDLK_*** as indices to IsKeyXXX() functions.
            return true;
        }
        case SDL_EVENT_WINDOW_MOUSE_ENTER:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->window.windowID) == null)
                return false;
            bd->MouseWindowID = ev->window.windowID;
            bd->MousePendingLeaveFrame = 0;
            return true;
        }
        // - In some cases, when detaching a window from main viewport SDL may send SDL_WINDOWEVENT_ENTER one frame too late,
        //   causing SDL_WINDOWEVENT_LEAVE on previous frame to interrupt drag operation by clear mouse position. This is why
        //   we delay process the SDL_WINDOWEVENT_LEAVE events by one frame. See issue #5012 for details.
        // FIXME: Unconfirmed whether this is still needed with SDL3.
        case SDL_EVENT_WINDOW_MOUSE_LEAVE:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->window.windowID) == null)
                return false;
            bd->MousePendingLeaveFrame = ImGui.GetFrameCount() + 1;
            return true;
        }
        case SDL_EVENT_WINDOW_FOCUS_GAINED:
        case SDL_EVENT_WINDOW_FOCUS_LOST:
        {
            if (ImGui_ImplSDL3_GetViewportForWindowID(ev->window.windowID) == null)
                return false;
            io.AddFocusEvent(eventType == SDL_EVENT_WINDOW_FOCUS_GAINED);
            return true;
        }
        case SDL_EVENT_GAMEPAD_ADDED:
        case SDL_EVENT_GAMEPAD_REMOVED:
        {
            bd->WantUpdateGamepadsList = true;
            return true;
        }
    }
    return false;
}

static void ImGui_ImplSDL3_SetupPlatformHandles(ImGuiViewport* viewport, SDL_Window* window)
{
    viewport->PlatformHandle = (void*)(IntPtr)SDL_GetWindowID(window);
    viewport->PlatformHandleRaw = null;
if (OperatingSystem.IsWindows()) {
    viewport->PlatformHandleRaw = (void*)SDL_GetPointerProperty(SDL_GetWindowProperties(window), SDL_PROP_WINDOW_WIN32_HWND_POINTER, 0);
} else if (OperatingSystem.IsMacOS()) {
    viewport->PlatformHandleRaw = (void*)SDL_GetPointerProperty(SDL_GetWindowProperties(window), SDL_PROP_WINDOW_COCOA_WINDOW_POINTER, 0);
}
}

public static bool Init(SDL_Window* window, SDL_Renderer* renderer, void* sdl_gl_context)
{
    var io = ImGui.GetIO();
    IMGUI_CHECKVERSION();
    IM_ASSERT(io.BackendPlatformUserData == 0, "Already initialized a platform backend!");
//  IM_UNUSED(sdl_gl_context); // Unused in this branch

    // Setup backend capabilities flags
    var bd = (ImGui_ImplSDL3_Data*)NativeMemory.AllocZeroed((nuint)sizeof(ImGui_ImplSDL3_Data));
    io.BackendPlatformUserData = (IntPtr)bd;
//  io.BackendPlatformName = "imgui_impl_sdl3";
    io.BackendFlags |= ImGuiBackendFlags.HasMouseCursors;           // We can honor GetMouseCursor() values (optional)
    io.BackendFlags |= ImGuiBackendFlags.HasSetMousePos;            // We can honor io.WantSetMousePos requests (optional, rarely used)

    bd->Window = window;
    bd->WindowID = SDL_GetWindowID(window);
    bd->Renderer = renderer;

    // Check and store if we are on a SDL backend that supports SDL_GetGlobalMouseState() and SDL_CaptureMouse()
    // ("wayland" and "rpi" don't support it, but we chose to use a white-list instead of a black-list)
    bd->MouseCanUseGlobalState = false;
    bd->MouseCanUseCapture = false;
if (SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE) {
    var sdl_backend = SDL_GetCurrentVideoDriver();
    var capture_and_global_state_whitelist = new [] { "windows", "cocoa", "x11", "DIVE", "VMAN" };
    foreach (var item in capture_and_global_state_whitelist)
        if (item.StartsWith(sdl_backend))
            bd->MouseCanUseGlobalState = bd->MouseCanUseCapture = true;
}

    var platform_io = ImGui.GetPlatformIO();
    platform_io.Platform_SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(ImGui_ImplSDL3_SetClipboardText);
    platform_io.Platform_GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(ImGui_ImplSDL3_GetClipboardText);
    platform_io.Platform_SetImeDataFn = Marshal.GetFunctionPointerForDelegate(ImGui_ImplSDL3_PlatformSetImeData);
    platform_io.Platform_OpenInShellFn = Marshal.GetFunctionPointerForDelegate(ImGui_ImplSDL3_OpenInShellFn);

    // Gamepad handling
    bd->GamepadMode = ImGui_ImplSDL3_GamepadMode.AutoFirst;
    bd->WantUpdateGamepadsList = true;

    // Load mouse cursors
    bd->MouseCursors[(int)ImGuiMouseCursor.Arrow] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_DEFAULT);
    bd->MouseCursors[(int)ImGuiMouseCursor.TextInput] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_TEXT);
    bd->MouseCursors[(int)ImGuiMouseCursor.ResizeAll] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_MOVE);
    bd->MouseCursors[(int)ImGuiMouseCursor.ResizeNS] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_NS_RESIZE);
    bd->MouseCursors[(int)ImGuiMouseCursor.ResizeEW] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_EW_RESIZE);
    bd->MouseCursors[(int)ImGuiMouseCursor.ResizeNESW] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_NESW_RESIZE);
    bd->MouseCursors[(int)ImGuiMouseCursor.ResizeNWSE] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_NWSE_RESIZE);
    bd->MouseCursors[(int)ImGuiMouseCursor.Hand] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_POINTER);
//  bd->MouseCursors[(int)ImGuiMouseCursor.Wait] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_WAIT);
//  bd->MouseCursors[(int)ImGuiMouseCursor.Progress] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_PROGRESS);
    bd->MouseCursors[(int)ImGuiMouseCursor.NotAllowed] = (nint)SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_NOT_ALLOWED);

    // Set platform dependent data in viewport
    // Our mouse update function expect PlatformHandle to be filled for the main viewport
    ImGuiViewport* main_viewport = ImGui.GetMainViewport();
    ImGui_ImplSDL3_SetupPlatformHandles(main_viewport, window);

    // From 2.0.5: Set SDL hint to receive mouse click events on window focus, otherwise SDL doesn't emit the event.
    // Without this, when clicking to gain focus, our widgets wouldn't activate even though they showed as hovered.
    // (This is unfortunately a global SDL setting, so enabling it might have a side-effect on your application.
    // It is unlikely to make a difference, but if your app absolutely needs to ignore the initial on-focus click:
    // you can ignore SDL_EVENT_MOUSE_BUTTON_DOWN events coming right after a SDL_WINDOWEVENT_FOCUS_GAINED)
#if SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH
    SDL_SetHint(SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
#endif

    // From 2.0.22: Disable auto-capture, this is preventing drag and drop across multiple windows (see #5710)
#if SDL_HINT_MOUSE_AUTO_CAPTURE
    SDL_SetHint(SDL_HINT_MOUSE_AUTO_CAPTURE, "0");
#endif

    return true;
}

static bool ImGui_ImplSDL3_InitForOpenGL(SDL_Window* window, void* sdl_gl_context)
{
//  IM_UNUSED(sdl_gl_context); // Viewport branch will need this.
    return Init(window, null, sdl_gl_context);
}

static bool ImGui_ImplSDL3_InitForVulkan(SDL_Window* window)
{
    return Init(window, null, null);
}

static bool ImGui_ImplSDL3_InitForD3D(SDL_Window* window)
{
if (!OperatingSystem.IsWindows()) {
    throw new InvalidOperationException("Unsupported");
}
    return Init(window, null, null);
}

static bool ImGui_ImplSDL3_InitForMetal(SDL_Window* window)
{
    return Init(window, null, null);
}

static bool ImGui_ImplSDL3_InitForSDLRenderer(SDL_Window* window, SDL_Renderer* renderer)
{
    return Init(window, renderer, null);
}

internal static bool ImGui_ImplSDL3_InitForSDLGPU(SDL_Window* window)
{
    return Init(window, null, null);
}

static bool ImGui_ImplSDL3_InitForOther(SDL_Window* window)
{
    return Init(window, null, null);
}

// static void ImGui_ImplSDL3_CloseGamepads();

static void ImGui_ImplSDL3_Shutdown()
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    IM_ASSERT(bd != null, "No platform backend to shutdown, or already shutdown?");
    var io = ImGui.GetIO();

    if (bd->ClipboardTextData != null)
        SDL_free(bd->ClipboardTextData);
    for (ImGuiMouseCursor cursor_n = 0; cursor_n < ImGuiMouseCursor.COUNT; cursor_n++)
        SDL_DestroyCursor((SDL_Cursor*)bd->MouseCursors[(int)cursor_n]);
    ImGui_ImplSDL3_CloseGamepads();

//  io.BackendPlatformName = null;
    io.BackendPlatformUserData = (IntPtr)null;
    io.BackendFlags &= ~(ImGuiBackendFlags.HasMouseCursors | ImGuiBackendFlags.HasSetMousePos | ImGuiBackendFlags.HasGamepad);
    NativeMemory.Free(bd);   // IM_DELETE(bd);
}

static void ImGui_ImplSDL3_UpdateMouseData()
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    var io = ImGui.GetIO();

    // We forward mouse input when hovered or captured (via SDL_EVENT_MOUSE_MOTION) or when focused (below)
    bool is_app_focused;
    SDL_Window* focused_window;
if (SDL_HAS_CAPTURE_AND_GLOBAL_MOUSE) {
    // - SDL_CaptureMouse() let the OS know e.g. that our drags can extend outside of parent boundaries (we want updated position) and shouldn't trigger other operations outside.
    // - Debuggers under Linux tends to leave captured mouse on break, which may be very inconvenient, so to mitigate the issue we wait until mouse has moved to begin capture.
    if (bd->MouseCanUseCapture)
    {
        bool want_capture = false;
        for (int button_n = 0; button_n < (int)ImGuiMouseButton.COUNT && !want_capture; button_n++)
            if (ImGui.IsMouseDragging((ImGuiMouseButton)button_n, 1.0f))
                want_capture = true;
        SDL_CaptureMouse(want_capture);
    }

    focused_window = SDL_GetKeyboardFocus();
    is_app_focused = (bd->Window == focused_window);
} else {
    focused_window = bd->Window;
    is_app_focused = ((ulong)SDL_GetWindowFlags(bd->Window) & SDL_WINDOW_INPUT_FOCUS) != 0L; // SDL 2.0.3 and non-windowed systems: single-viewport only
}
    if (is_app_focused)
    {
        // (Optional) Set OS mouse position from Dear ImGui if requested (rarely used, only when io.ConfigNavMoveSetMousePos is enabled by user)
        if (io.WantSetMousePos)
            SDL_WarpMouseInWindow(bd->Window, io.MousePos.X, io.MousePos.Y);

        // (Optional) Fallback to provide mouse position when focused (SDL_EVENT_MOUSE_MOTION already provides this when hovered or captured)
        bool is_relative_mouse_mode = SDL_GetWindowRelativeMouseMode(bd->Window);
        if (bd->MouseCanUseGlobalState && bd->MouseButtonsDown == 0 && !is_relative_mouse_mode)
        {
            // Single-viewport mode: mouse position in client window coordinates (io.MousePos is (0,0) when the mouse is on the upper-left corner of the app window)
            float mouse_x_global, mouse_y_global;
            int window_x, window_y;
            SDL_GetGlobalMouseState(&mouse_x_global, &mouse_y_global);
            SDL_GetWindowPosition(focused_window, &window_x, &window_y);
            io.AddMousePosEvent(mouse_x_global - window_x, mouse_y_global - window_y);
        }
    }
}

static void ImGui_ImplSDL3_UpdateMouseCursor()
{
    var io = ImGui.GetIO();
    if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) != 0)
        return;
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();

    var imgui_cursor = ImGui.GetMouseCursor();
    if (io.MouseDrawCursor || imgui_cursor == ImGuiMouseCursor.None)
    {
        // Hide OS mouse cursor if imgui is drawing it or if it wants no cursor
        SDL_HideCursor();
    }
    else
    {
        // Show OS mouse cursor
        SDL_Cursor* expected_cursor = (SDL_Cursor*)(bd->MouseCursors[(int)imgui_cursor] != 0 ? bd->MouseCursors[(int)imgui_cursor] : bd->MouseCursors[(int)ImGuiMouseCursor.Arrow]);
        if (bd->MouseLastCursor != expected_cursor)
        {
            SDL_SetCursor(expected_cursor); // SDL function doesn't have an early out (see #6113)
            bd->MouseLastCursor = expected_cursor;
        }
        SDL_ShowCursor();
    }
}

static void ImGui_ImplSDL3_CloseGamepads()
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    if (bd->GamepadMode != ImGui_ImplSDL3_GamepadMode.Manual)
    for (int n = 0; n < bd->GamepadCount; n++)
        SDL_CloseGamepad((SDL_Gamepad*)bd->Gamepads[n]);
    bd->GamepadCount = 0;
}

static void ImGui_ImplSDL3_SetGamepadMode(ImGui_ImplSDL3_GamepadMode mode, SDL_Gamepad** manual_gamepads_array, int manual_gamepads_count)
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    ImGui_ImplSDL3_CloseGamepads();
    if (mode == ImGui_ImplSDL3_GamepadMode.Manual)
    {
        IM_ASSERT(manual_gamepads_array != null || manual_gamepads_count <= 0, "manual_gamepads_array != null || manual_gamepads_count <= 0");
        for (int n = 0; n < manual_gamepads_count; n++)
            bd->Gamepads[n] = (nint)manual_gamepads_array[n]; // bd->Gamepads.push_back(manual_gamepads_array[n]);
        bd->GamepadCount = manual_gamepads_count;
    }
    else
    {
        IM_ASSERT(manual_gamepads_array == null && manual_gamepads_count <= 0, "manual_gamepads_array == null && manual_gamepads_count <= 0");
        bd->WantUpdateGamepadsList = true;
    }
    bd->GamepadMode = mode;
}

static void ImGui_ImplSDL3_UpdateGamepadButton(ImGui_ImplSDL3_Data* bd, ImGuiIOPtr io, ImGuiKey key, SDL_GamepadButton button_no)
{
    bool merged_value = false;
    for (int n = 0; n < bd->GamepadCount; n++)
        merged_value |= SDL_GetGamepadButton((SDL_Gamepad*)bd->Gamepads[n], button_no) != false;
    io.AddKeyEvent(key, merged_value);
}

static float Saturate(float v) { return v < 0.0f ? 0.0f : v  > 1.0f ? 1.0f : v; }
static void ImGui_ImplSDL3_UpdateGamepadAnalog(ImGui_ImplSDL3_Data* bd, ImGuiIOPtr io, ImGuiKey key, SDL_GamepadAxis axis_no, float v0, float v1)
{
    float merged_value = 0.0f;
    for (int n = 0; n < bd->GamepadCount; n++)
    {
        float vn = Saturate((float)(SDL_GetGamepadAxis((SDL_Gamepad*)bd->Gamepads[n], axis_no) - v0) / (float)(v1 - v0));
        if (merged_value < vn)
            merged_value = vn;
    }
    io.AddKeyAnalogEvent(key, merged_value > 0.1f, merged_value);
}

static void ImGui_ImplSDL3_UpdateGamepads()
{
    var io = ImGui.GetIO();
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();

    // Update list of gamepads to use
    if (bd->WantUpdateGamepadsList && bd->GamepadMode != ImGui_ImplSDL3_GamepadMode.Manual)
    {
        ImGui_ImplSDL3_CloseGamepads();
        int sdl_gamepads_count = 0;
        SDL_JoystickID* sdl_gamepads = SDL_GetGamepads(&sdl_gamepads_count);
        for (int n = 0; n < sdl_gamepads_count; n++) {
            SDL_Gamepad* gamepad; if ((gamepad = SDL_OpenGamepad(sdl_gamepads[n])) != null)
            {
                bd->Gamepads[bd->GamepadCount++] = (nint)gamepad;
                if (bd->GamepadMode == ImGui_ImplSDL3_GamepadMode.AutoFirst)
                    break;
            }
        }
        bd->WantUpdateGamepadsList = false;
        SDL_free(sdl_gamepads);
    }

    io.BackendFlags &= ~ImGuiBackendFlags.HasGamepad;
    if (bd->GamepadCount == 0)
        return;
    io.BackendFlags |= ImGuiBackendFlags.HasGamepad;

    // Update gamepad inputs
    const int thumb_dead_zone = 8000;           // SDL_gamepad.h suggests using this value.
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadStart,       SDL_GAMEPAD_BUTTON_START);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadBack,        SDL_GAMEPAD_BUTTON_BACK);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadFaceLeft,    SDL_GAMEPAD_BUTTON_WEST);           // Xbox X, PS Square
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadFaceRight,   SDL_GAMEPAD_BUTTON_EAST);           // Xbox B, PS Circle
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadFaceUp,      SDL_GAMEPAD_BUTTON_NORTH);          // Xbox Y, PS Triangle
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadFaceDown,    SDL_GAMEPAD_BUTTON_SOUTH);          // Xbox A, PS Cross
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadDpadLeft,    SDL_GAMEPAD_BUTTON_DPAD_LEFT);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadDpadRight,   SDL_GAMEPAD_BUTTON_DPAD_RIGHT);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadDpadUp,      SDL_GAMEPAD_BUTTON_DPAD_UP);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadDpadDown,    SDL_GAMEPAD_BUTTON_DPAD_DOWN);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadL1,          SDL_GAMEPAD_BUTTON_LEFT_SHOULDER);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadR1,          SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadL2,          SDL_GAMEPAD_AXIS_LEFT_TRIGGER,  0.0f, 32767);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadR2,          SDL_GAMEPAD_AXIS_RIGHT_TRIGGER, 0.0f, 32767);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadL3,          SDL_GAMEPAD_BUTTON_LEFT_STICK);
    ImGui_ImplSDL3_UpdateGamepadButton(bd, io, ImGuiKey.GamepadR3,          SDL_GAMEPAD_BUTTON_RIGHT_STICK);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadLStickLeft,  SDL_GAMEPAD_AXIS_LEFTX,  -thumb_dead_zone, -32768);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadLStickRight, SDL_GAMEPAD_AXIS_LEFTX,  +thumb_dead_zone, +32767);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadLStickUp,    SDL_GAMEPAD_AXIS_LEFTY,  -thumb_dead_zone, -32768);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadLStickDown,  SDL_GAMEPAD_AXIS_LEFTY,  +thumb_dead_zone, +32767);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadRStickLeft,  SDL_GAMEPAD_AXIS_RIGHTX, -thumb_dead_zone, -32768);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadRStickRight, SDL_GAMEPAD_AXIS_RIGHTX, +thumb_dead_zone, +32767);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadRStickUp,    SDL_GAMEPAD_AXIS_RIGHTY, -thumb_dead_zone, -32768);
    ImGui_ImplSDL3_UpdateGamepadAnalog(bd, io, ImGuiKey.GamepadRStickDown,  SDL_GAMEPAD_AXIS_RIGHTY, +thumb_dead_zone, +32767);
}

static Uint64 frequency;

public static void NewFrame()
{
    ImGui_ImplSDL3_Data* bd = ImGui_ImplSDL3_GetBackendData();
    IM_ASSERT(bd != null, "Context or backend not initialized! Did you call ImGui_ImplSDL3_Init()?");
    var io = ImGui.GetIO();

    // Setup display size (every frame to accommodate for window resizing)
    int w, h;
    int display_w, display_h;
    SDL_GetWindowSize(bd->Window, &w, &h);
    if (((ulong)SDL_GetWindowFlags(bd->Window) & SDL_WINDOW_MINIMIZED) != 0)
        w = h = 0;
    SDL_GetWindowSizeInPixels(bd->Window, &display_w, &display_h);
    io.DisplaySize = new((float)w, (float)h);
    if (w > 0 && h > 0)
        io.DisplayFramebufferScale = new((float)display_w / w, (float)display_h / h);

    // Setup time step (we don't use SDL_GetTicks() because it is using millisecond resolution)
    // (Accept SDL_GetPerformanceCounter() not returning a monotonically increasing value. Happens in VMs and Emscripten, see #6189, #6114, #3644)
    frequency = SDL_GetPerformanceFrequency();
    Uint64 current_time = SDL_GetPerformanceCounter();
    if (current_time <= bd->Time)
        current_time = bd->Time + 1;
    io.DeltaTime = bd->Time > 0 ? (float)((double)(current_time - bd->Time) / frequency) : (float)(1.0f / 60.0f);
    bd->Time = current_time;

    if (bd->MousePendingLeaveFrame != 0 && bd->MousePendingLeaveFrame >= ImGui.GetFrameCount() && bd->MouseButtonsDown == 0)
    {
        bd->MouseWindowID = 0;
        bd->MousePendingLeaveFrame = 0;
        io.AddMousePosEvent(-float.MaxValue,-float.MaxValue);
    }

    ImGui_ImplSDL3_UpdateMouseData();
    ImGui_ImplSDL3_UpdateMouseCursor();

    // Update game controllers (if enabled and available)
    ImGui_ImplSDL3_UpdateGamepads();
}

//-----------------------------------------------------------------------------

// #if defined(__clang__)
// #pragma clang diagnostic pop
// #endif
// 
// #endif // #ifndef IMGUI_DISABLE

}

