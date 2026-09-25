using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DD2ModConfig.Code.Audio;

public class RMAudioManager
{
    private static AudioStreamPlayer? _rmAudioPlayer;
    public static AudioStreamPlayer RMAudioPlayer
    {
        get
        {
            // IsInstanceValid 能同时检测 null 和已被 QueueFree 的对象
            if (!GodotObject.IsInstanceValid(_rmAudioPlayer))
            {
                _rmAudioPlayer = new AudioStreamPlayer();
            }
            return _rmAudioPlayer;
        }
    }

    public static void PlayRMSfx(string AudioName, bool bIsTemp = false, bool bIsFullPathName = false, float VolumeDb = -10.0f) //声音素材音量太大了，减小一点
    {
        if (NonInteractiveMode.IsActive) return;

        AudioStream stream;
        string path = bIsFullPathName ? AudioName : GetEnterSfxPath(AudioName);

        if (path == null || path == "") return;
        try
        {
            stream = PreloadManager.Cache.GetAsset<AudioStream>(path);
        }
        catch
        {
            Log.Error($"[RMAudioManager] Could not load audio: {path}");
            return;
        }

        //单例模式的audioPlayer，不要手动释放
        var audioPlayer = bIsTemp ? new AudioStreamPlayer() : RMAudioPlayer;

        audioPlayer.VolumeDb = 0.0f;
        audioPlayer.VolumeDb += VolumeDb;
        audioPlayer.Stream = stream;
        audioPlayer.Bus = "SFX";
        if (bIsTemp)
        {
            audioPlayer.Finished += () => audioPlayer.QueueFreeSafely();
        }

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom != null)
        {
            if (audioPlayer.GetParent() == null)
            {
                combatRoom.AddChild(audioPlayer);
            }
            audioPlayer.Play();
        }
        else if (bIsTemp)
        {
            audioPlayer.QueueFreeSafely();
        }
    }
    private static string GetEnterSfxPath(string RMType)
    {
        string Path = "";
        switch (RMType)
        {
            case "Resolute":
                Path = "res://DD2ModConfig/Sounds/Resolute/sfx_battle_status_resolute.wav";
                break;
            case "Meltdown":
                Path = "res://DD2ModConfig/Sounds/Meltdown/sfx_battle_status_meltdown.wav";
                break;
            case "Toxic":
                Path = "res://DD2ModConfig/Sounds/Toxic/sfx_battle_status_toxic.wav";
                break;
            default:
                break;
        }
        return Path;
    }
}
