using ImGuiNET;
using SDL;
using static SDL.SDL3;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace SDL3ImGui;

// port from:  https://github.com/ocornut/imgui/blob/master/examples/example_sdl3_sdlgpu3/main.cpp
/// <summary>
/// Helper methods to simplify integration of ImGui into an SDL3 based application
/// </summary>
public static unsafe class SDL3ImGuiTools
{
    public static SDL_GPUDevice* CreateGpuDevice(SDL_Window* window)
    {
        var gpu_device = SDL_CreateGPUDevice(SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL | SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_METALLIB, true, new Utf8String());
        // Claim window for GPU Device
        if (!SDL_ClaimWindowForGPUDevice(gpu_device, window)) {
            // printf("Error: SDL_ClaimWindowForGPUDevice(): %s\n", SDL_GetError());
            return null;
        }
        SDL_SetGPUSwapchainParameters(gpu_device, window, SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR, SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX);
        return gpu_device;
    }
    
    public static void InitGpuDevice(SDL_Window* window,  SDL_GPUDevice* gpuDevice)
    {
        ImGui_ImplSDL3.ImGui_ImplSDL3_InitForSDLGPU(window);
        ImGui_ImplSDLGPU3.ImGui_ImplSDLGPU3_InitInfo init_info = default;
        init_info.Device = gpuDevice;
        init_info.ColorTargetFormat = SDL_GetGPUSwapchainTextureFormat(gpuDevice, window);
        init_info.MSAASamples = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1;
        ImGui_ImplSDLGPU3.ImGui_ImplSDLGPU3_Init(&init_info);
    }
    
    /// <summary>
    /// Must be called before <see cref="Render"/>
    /// </summary>
    public static void NewFrame()
    {
        ImGui_ImplSDLGPU3.ImGui_ImplSDLGPU3_NewFrame();
        ImGui_ImplSDL3.NewFrame();
        ImGui.NewFrame();
    }
    
    public static bool ProcessEvent(SDL_Event e) {
        return ImGui_ImplSDL3.ProcessEvent(&e);
    }
    
    public static void Render(SDL_Window* window, SDL_GPUDevice* gpuDevice)
    {
        ImGui.Render();
        
        ImDrawData* draw_data = ImGui.GetDrawData();
        bool is_minimized = (draw_data->DisplaySize.X <= 0.0f || draw_data->DisplaySize.Y <= 0.0f);

        SDL_GPUCommandBuffer* command_buffer = SDL_AcquireGPUCommandBuffer(gpuDevice); // Acquire a GPU command buffer

        SDL_GPUTexture* swapchain_texture;
        SDL_AcquireGPUSwapchainTexture(command_buffer, window, &swapchain_texture, null, null); // Acquire a swapchain texture

        if (swapchain_texture != null && !is_minimized)
        {
            // This is mandatory: call ImGui_ImplSDLGPU3_PrepareDrawData() to upload the vertex/index buffer!
            ImGui_ImplSDLGPU3.ImGui_ImplSDLGPU3_PrepareDrawData(draw_data, command_buffer);

            // Setup and start a render pass
            SDL_GPUColorTargetInfo target_info = default;
            target_info.texture = swapchain_texture;
            target_info.clear_color = new SDL_FColor { r = 0.0f, g = 0.3f, b = 0.3f, a = 1 };  // { clear_color.x, clear_color.y, clear_color.z, clear_color.w };
            target_info.load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR;
            target_info.store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE;
            target_info.mip_level = 0;
            target_info.layer_or_depth_plane = 0;
            target_info.cycle = false;
            SDL_GPURenderPass* render_pass = SDL_BeginGPURenderPass(command_buffer, &target_info, 1, null);

            // Render ImGui
            ImGui_ImplSDLGPU3.ImGui_ImplSDLGPU3_RenderDrawData(draw_data, command_buffer, render_pass, null); // TODO last param null?

            SDL_EndGPURenderPass(render_pass);
        }
        SDL_SubmitGPUCommandBuffer(command_buffer);
    }
}