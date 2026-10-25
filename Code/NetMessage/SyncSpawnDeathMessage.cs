using BaseLib.Abstracts;
using DD2ModConfig.Code.Config;
using DD2ModConfig.Code.Monster.DeathMonster;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace DD2ModConfig.Code.NetMessage;

public class SyncSpawnDeathMessage : ICustomMessage
{
    public SyncSpawnDeathMessage() { }

    /*// 带参构造（方便你发送时 new）
    public SyncSpawnDeathMessage(bool whatEverBool)
    {
        //......
    }*/
    public bool ShouldBroadcast => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteBool(ShouldPredictWhetherDeathWillAppear);
        writer.WriteBool(ShouldDeathOnlyHuntFlagellant);
        writer.WriteInt(DeathAppearMaxTime);
        writer.WriteInt(DeathEncounterChance);
        writer.WriteBool(ShouldDeathAppearInMonsterRoom);
        writer.WriteBool(ShouldDeathAppearInEliteRoom);
        writer.WriteBool(ShouldDeathAppearInBossRoom);
        writer.WriteBool(ShouldEnhanceDeathAfterDefeat);
    }
    public void Deserialize(PacketReader reader)
    {
        ShouldPredictWhetherDeathWillAppear = reader.ReadBool();
        ShouldDeathOnlyHuntFlagellant = reader.ReadBool();
        DeathAppearMaxTime = reader.ReadInt();
        DeathEncounterChance = reader.ReadInt();
        ShouldDeathAppearInMonsterRoom = reader.ReadBool();
        ShouldDeathAppearInEliteRoom = reader.ReadBool();
        ShouldDeathAppearInBossRoom = reader.ReadBool();
        ShouldEnhanceDeathAfterDefeat = reader.ReadBool();
    }

    public void HandleMessage(ulong senderId)
    {
        DeathListenForRunStateSingleton.ShouldPredictWhetherDeathWillAppear = ShouldPredictWhetherDeathWillAppear;
        DeathListenForRunStateSingleton.ShouldDeathOnlyHuntFlagellant = ShouldDeathOnlyHuntFlagellant;
        DeathListenForRunStateSingleton.DeathAppearMaxTime = DeathAppearMaxTime;
        DeathListenForRunStateSingleton.DeathEncounterChance = DeathEncounterChance;
        DeathListenForRunStateSingleton.ShouldDeathAppearInMonsterRoom = ShouldDeathAppearInMonsterRoom;
        DeathListenForRunStateSingleton.ShouldDeathAppearInEliteRoom = ShouldDeathAppearInEliteRoom;
        DeathListenForRunStateSingleton.ShouldDeathAppearInBossRoom = ShouldDeathAppearInBossRoom;
        DeathListenForRunStateSingleton.ShouldEnhanceDeathAfterDefeat = ShouldEnhanceDeathAfterDefeat;
    }

    public bool ShouldPredictWhetherDeathWillAppear { get; set; } = DD2ModConfigCfg.PredictWhetherDeathWillAppear;
    public bool ShouldDeathOnlyHuntFlagellant { get; set; } = DD2ModConfigCfg.ShouldDeathOnlyHuntFlagellant;
    public int DeathAppearMaxTime { get; set; } = DD2ModConfigCfg.DeathAppearMaxTime;
    public int DeathEncounterChance { get; set; } = DD2ModConfigCfg.DeathEncounterChance;
    public bool ShouldDeathAppearInMonsterRoom { get; set; } = DD2ModConfigCfg.ShouldDeathAppearInMonsterRoom;
    public bool ShouldDeathAppearInEliteRoom { get; set; } = DD2ModConfigCfg.ShouldDeathAppearInEliteRoom;
    public bool ShouldDeathAppearInBossRoom { get; set; } = DD2ModConfigCfg.ShouldDeathAppearInBossRoom;
    public bool ShouldEnhanceDeathAfterDefeat { get; set; } = DD2ModConfigCfg.ShouldEnhanceDeathAfterDefeat;
}
