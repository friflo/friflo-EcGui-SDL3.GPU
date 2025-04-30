using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ImGuiNET;
using static SDL.SDL3;

using ImGuiContext = System.IntPtr;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace SDL3ImGui;

delegate void UserCallback(ImDrawListPtr cmd_list, ImDrawCmdPtr cmd);

/// <summary>
/// Used internally by imgui_impl*.* files
/// </summary>
internal static class ImGuiUtils
{
    // --- imgui_impl_sdl3.cpp
    internal static void IM_ASSERT(bool condition, string message) {
        if (!condition) {
            throw new InvalidOperationException(message);
        }
    }
    
    internal static unsafe T* IM_NEW<T>() where T : unmanaged {
        return (T*)NativeMemory.AllocZeroed((nuint)sizeof(T));
    }
    
    internal static unsafe void IM_DELETE(void* ptr) {
        NativeMemory.Free(ptr);
    }
    
    internal static void IMGUI_CHECKVERSION() {}
    
    internal static unsafe byte* SDL_strdup(string value) {
        var len = Encoding.UTF8.GetByteCount(value);
        var ptr = (byte*)SDL_calloc((uint)len + 1, 1);
        Encoding.UTF8.GetBytes(value.AsSpan(), new Span<byte>(ptr, len));
        ptr[len] = 0;
        return ptr;
    }
    
    [InlineArray((int)ImGuiMouseCursor.COUNT)]
    internal struct Cursors
    {
        // structs with the InlineArray attribute must contain EXACTLY one member.
        private IntPtr element;
        
        /* internal SDL_Cursor* this[int i] {
            get { return (SDL_Cursor*)element + i; }
            set { *((IntPtr*)element + i) = (IntPtr)value; }
        }*/
    }
    
    [InlineArray(10)]
    internal struct Gamepads
    {
        // structs with the InlineArray attribute must contain EXACTLY one member.
        private IntPtr element;
        
        // internal SDL_Gamepad* this[int i] => (SDL_Gamepad*)element + i;
    }
    
    // Called by TextLinkOpenURL() widget. E.g. ImGui.TextLinkOpenURL("https://www.google.de");
    internal static unsafe bool ImGui_ImplSDL3_OpenInShellFn(ImGuiContext ctx, byte* url) {
        return SDL_OpenURL(url);
    }

    // --- imgui_impl_wgpu.cpp
    // MEMALIGN(_SIZE,_ALIGN)        (((_SIZE) + ((_ALIGN) - 1)) & ~((_ALIGN) - 1))
    internal static ulong MEMALIGN(long size, int align)
    {
        return (ulong)((size + (align - 1)) & ~(align - 1));
    }
    
    internal  static unsafe void memcpy(void* dest, void* src, int size) {
        Buffer.MemoryCopy(src, dest, size, size); 
    }
    
    // ImDrawCallback_ResetRenderState is not defined in
    // - https://github.com/ocornut/imgui
    // - https://github.com/ImGuiNET/ImGui.NET
    internal static readonly IntPtr ImDrawCallback_ResetRenderState = -1;
    
    /* internal static WGPUTextureView AsTextureView(IntPtr intPtr) {
        return Unsafe.As<IntPtr, WGPUTextureView>(ref intPtr);
    } */
}