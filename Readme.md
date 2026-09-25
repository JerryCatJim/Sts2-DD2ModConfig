Create a new file with full name "Directory.Build.props" and fill it with the following content.
Remember to change the following paths to your own!

<Project>
  <PropertyGroup>
    <!-- // NOTE: Megadot is version 4.5.1, and the game won't load your .pck if the Godot version used is newer. -->
    <!-- // NOTE: Change your Godot's export name to "BasicExport" if you want to package the .pck automatically when publishing in C# project! -->
    <!-- // NOTE: Change the following path to your own! -->
    <!-- ========== Windows default ========== -->
    <GodotWinPath>E:\GodotEngine_4.5.1_stable\Godot_v4.5.1-stable_mono_win64.exe</GodotWinPath>
    <SteamLibraryWinPath>E:/Steam/steamapps</SteamLibraryWinPath>
    <BaseLibPath>E:\Steam\steamapps\common\Slay the Spire 2\BaseLib</BaseLibPath>

    <!-- ========== Linux default ========== -->
    <!-- 注意：$(HOME) 会读取环境变量，如果没定义可改用 /home/$(USER) 等 -->
    <GodotLinuxPath>$(HOME)/.local/share/Steam/steamapps/common/Godot Engine/godot.x11.opt.tools.64</GodotLinuxPath>
    <SteamLibraryLinuxPath>$(HOME)/.local/share/Steam/steamapps</SteamLibraryLinuxPath>

    <!-- ========== macOS default ========== -->
    <GodotMacPath>$(HOME)/Applications/Godot_mono.app/Contents/MacOS/Godot</GodotMacPath>
    <SteamLibraryMacPath>$(HOME)/Library/Application Support/Steam/steamapps</SteamLibraryMacPath>
  </PropertyGroup>
</Project>