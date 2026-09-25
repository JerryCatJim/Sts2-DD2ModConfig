using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Saves;

namespace DD2ModConfig.Code.Audio;

public class DD2AudioManager
{
    private static AudioStreamPlayer? _stressAudioPlayer;
    public static AudioStreamPlayer StressAudioPlayer
    {
        get
        {
            // IsInstanceValid 能同时检测 null 和已被 QueueFree 的对象
            if (!GodotObject.IsInstanceValid(_stressAudioPlayer))
            {
                _stressAudioPlayer = new AudioStreamPlayer();
            }
            return _stressAudioPlayer;
        }
    }

    private static AudioStreamPlayer? _deathDoorAudioPlayer;
    public static AudioStreamPlayer DeathDoorAudioPlayer
    {
        get
        {
            // IsInstanceValid 能同时检测 null 和已被 QueueFree 的对象
            if (!GodotObject.IsInstanceValid(_deathDoorAudioPlayer))
            {
                _deathDoorAudioPlayer = new AudioStreamPlayer();
            }
            return _deathDoorAudioPlayer;
        }
    }
    private static AudioStreamPlayer? _deathBlowAudioPlayer;
    public static AudioStreamPlayer DeathBlowAudioPlayer
    {
        get
        {
            // IsInstanceValid 能同时检测 null 和已被 QueueFree 的对象
            if (!GodotObject.IsInstanceValid(_deathBlowAudioPlayer))
            {
                _deathBlowAudioPlayer = new AudioStreamPlayer();
            }
            return _deathBlowAudioPlayer;
        }
    }
    private static AudioStreamPlayer? _dd2BgmPlayer;
    public static AudioStreamPlayer DD2BgmPlayer
    {
        get
        {
            // IsInstanceValid 能同时检测 null 和已被 QueueFree 的对象
            if (!GodotObject.IsInstanceValid(_dd2BgmPlayer))
            {
                _dd2BgmPlayer = new AudioStreamPlayer();
            }
            return _dd2BgmPlayer;
        }
    }
    public static float ModifiedDD2BgmLinear { get; set; } = 0.0f;
    public static void PlayDD2Sfx(string AudioName, bool bIsTemp = false, bool bIsFullPathName = false, float VolumeDb = 0.0f)
    {
        if (NonInteractiveMode.IsActive) return;

        AudioStream stream;
        string path = bIsFullPathName ? AudioName : GetDD2SoundPath(AudioName);

        if (path == null || path == "") return;
        try
        {
            stream = PreloadManager.Cache.GetAsset<AudioStream>(path);
        }
        catch
        {
            Log.Error($"[DD2AudioManager] Could not load audio: {path}");
            return;
        }

        //单例模式的audioPlayer，不要手动释放
        var audioPlayer = bIsTemp ? new AudioStreamPlayer() : GetDD2AudioPlayer(AudioName);

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

    public static void PlayDD2Bgm(string BgmName, float VolumeDb = 0f)
    {
        AudioStream stream;
        string path = GetDD2BgmPath(BgmName);
        try
        {
            stream = PreloadManager.Cache.GetAsset<AudioStream>(path);
        }
        catch
        {
            GD.PrintErr($"[DD2AudioManager] Could not load audio: {path}");
            return;
        }

        //单例模式的audioPlayer，不要手动释放
        var audioPlayer = DD2BgmPlayer;

        audioPlayer.VolumeDb = 0.0f;
        audioPlayer.VolumeDb += VolumeDb;
        audioPlayer.Stream = stream;
        audioPlayer.Bus = "SFX";

        ModifiedDD2BgmLinear = audioPlayer.VolumeLinear;

        SetDD2BgmPlayerVolumeByPercent(SaveManager.Instance.SettingsSave.VolumeBgm);

        var combatRoom = NCombatRoom.Instance;
        if (combatRoom != null)
        {
            if (audioPlayer.GetParent() == null)
            {
                combatRoom.AddChild(audioPlayer);
                audioPlayer.Play();
                NAudioManager.Instance?.SetBgmVol(0);
            }
        }
    }
    public static void StopDD2Bgm()
    {
        var audioPlayer = DD2BgmPlayer;
        audioPlayer.Stop();
        audioPlayer.QueueFreeSafely();
        NAudioManager.Instance?.SetBgmVol(SaveManager.Instance.SettingsSave.VolumeBgm);
    }

    private static AudioStreamPlayer GetDD2AudioPlayer(string audioName)
    {
        if (audioName.Contains("Stress"))
        {
            return StressAudioPlayer;
        }
        else if (audioName.Contains("DeathDoor"))
        {
            return DeathDoorAudioPlayer;
        }
        else if (audioName.Contains("DeathBlow"))
        {
            return DeathBlowAudioPlayer;
        }
        return new AudioStreamPlayer();
    }

    private static string GetDD2SoundPath(string audioName)
    {
        switch (audioName)
        {
            case "GainStress":
                return "res://DD2ModConfig/Sounds/Stress/sfx_battle_status_stressup.wav";
            case "LoseStress":
                return "res://DD2ModConfig/Sounds/Stress/sfx_battle_status_stressdown.wav";
            case "DeathDoor":
                return "res://DD2ModConfig/Sounds/DeathDoor/sfx_battle_deathsdoor.wav";
            case "DeathArmor":
                return "res://DD2ModConfig/Sounds/DeathDoor/sfx_battle_token_deathsdoorarmor.wav";
            case "DeathBlowDoom":
                return "res://DD2ModConfig/Sounds/DeathDoor/sfx_battle_deathsdoor_deathblow.wav";
            case "DeathBlow":
                return "res://DD2ModConfig/Sounds/DeathDoor/sfx_battle_deathsdoor_deathblow_v2.wav";
            default:
                return "";
        }
    }
    private static string GetDD2BgmPath(string bgmName)
    {
        switch (bgmName)
        {
            case "Death":
            case "Mountain":
                return "res://DD2ModConfig/Sounds/Bgm/DD2_TheMountain_BGM.mp3";
            default:
                return "";
        }
    }

    public static void SetDD2BgmPlayerVolumeByPercent(float percent)
    {
        //SaveManager.Instance.SettingsSave.VolumeBgm在 [NBgmVolumeSlider] 类 的 "OnValueChanged"函数里接收的值已经/100.0了
        //VolumeLinear会把VolumeDb覆盖，所以要把修改过的Volume先记录再应用百分比
        DD2BgmPlayer.VolumeLinear = ModifiedDD2BgmLinear * Math.Clamp(percent, 0, 1);
    }
}
