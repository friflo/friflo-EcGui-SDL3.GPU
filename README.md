# friflo EcGui - SDL3 GPU

Minimal **SDL3 GPU** setup to showcase use and integration of [**friflo EcGui**](https://github.com/friflo/Friflo.Engine.ECS#ecgui).

**friflo EcGui** can be integrated in every environment that support [**ImGui.NET**](https://github.com/ImGuiNET/ImGui.NET).

Ready-to-run Demos are available for: MonoGame, Godot, SDL3 GPU and Silk.NET.OpenGL.

![friflo-EcGui-SDL3](https://github.com/user-attachments/assets/eb6d5319-0e3d-41a1-a89e-719586e0093f)


## Goals

- Provide instant access to entities their components, tags and relations at runtime via **Explorer** and **Inspector** window.

- Provides a good understanding of how ECS query filters work by adding or removing components or tags in the **Explorer**.  
  The demo allows you to immediately dive deep into ECS without coding.

- **EcGui** accelerates development speed of your ECS significant.  
  It makes the need of a debugger (Watch & Variables window) or logging in big parts obsolete.  
  Typical weak points using a debugger or logging:  
  * _Debugger:_ Requires to stop the process and finding all required data is time consuming.
  * _Logging:_  Over logging impacts performance, disk space and bandwidth. Logging less data carries the risk of missing important data.  
                Logging clutters the code with log statements and may introduce exceptions.  

- Integration requires only 3 simple method calls:
  ```cs
  // on startup
  EcGui.AddExplorerStore("Store", store);
  // in render loop
  EcGui.ExplorerWindow();
  EcGui.InspectorWindow();
  ```
  Additional queries and systems can be added to the **Explorer** at any time with:
  ```cs
  EcGui.AddExplorerQuery("myQuery", myQuery);
  EcGui.AddExplorerSystems(root);
  ```

- ECS data rendering, editing and interaction with the **EcGui** is instant.

- The impact on game loop performance and rendering is negligible.  
  The entire execution time (ECS data access, layout and rendering) is ~0.1 - 0.5 ms per frame on a modern dev system.  
  The execution requires no heap allocation in common cases to prevent impacting performance by GC collections.


## Folder structure

```
ECS 
- DemoECS.cs      Creates an example store with some entities. Show how to customize EcGui.
- DemoTypes.cs    Contains various example types: components, relations, tags, custom types and enums
- TypeDrawers.cs  Custom TypeDrawer's are optional.
                  They enable to render custom types in a single line. E.g. Point, Color, Sprite, ...

ImGui             Contains the source to connect SDL3 GPU Input and Rendering to ImGui.NET.
                  This folder should be copied to your project as is.
```

## License

This project is licensed under MIT.  

Copyright © 2025   Ullrich Praetz - https://github.com/friflo