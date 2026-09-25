using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace DD2ModConfig.Code.Monster;

public static class MonsterAudioManager
{
    private static AudioStreamPlayer? _monsterAudioPlayer;
    public static AudioStreamPlayer MonsterAudioPlayer
    {
        get
        {
            // IsInstanceValid 能同时检测 null 和已被 QueueFree 的对象
            if (!GodotObject.IsInstanceValid(_monsterAudioPlayer))
            {
                _monsterAudioPlayer = new AudioStreamPlayer();
            }
            return _monsterAudioPlayer;
        }
    }

    public static void PlayDeathSfx(string audioName, bool bIsTemp = false, bool bIsFullPathName = false, float VolumeDb = -4f)
    {
        PlayMonsterSfx(audioName, bIsTemp, bIsFullPathName, VolumeDb, "Death");
    }
    private static void PlayMonsterSfx(string audioName, bool bIsTemp, bool bIsFullPathName, float VolumeDb, string monsterName)
    {
        AudioStream stream;
        string path = bIsFullPathName ? audioName : MonsterAudioCfg.GetPathByMonsterName(audioName, monsterName);

        if (path == null || path == "") return;
        try
        {
            stream = PreloadManager.Cache.GetAsset<AudioStream>(path);
        }
        catch
        {
            Log.Error($"[MonsterAudioManager] Could not load audio: {path}");
            return;
        }

        //单例模式的audioPlayer，不要手动释放
        var audioPlayer = bIsTemp ? new AudioStreamPlayer() : MonsterAudioPlayer;

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
}