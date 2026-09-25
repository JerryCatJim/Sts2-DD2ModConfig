using System;

namespace DD2ModConfig.Code.ResoluteOrMeltdown;

public sealed class RMRegistration
{
    public Type ModelType { get; }
    public string CharacterId { get; }
    public ResoluteOrMeltdownType RMType { get; }
    public bool IsUnique { get; }
    public Func<ResoluteOrMeltdownModel> Factory { get; }

    public RMRegistration(Type modelType, string characterId, ResoluteOrMeltdownType rmType, bool isUnique, Func<ResoluteOrMeltdownModel> factory)
    {
        ModelType = modelType;
        CharacterId = characterId;
        RMType = rmType;
        IsUnique = isUnique;
        Factory = factory;
    }

    public override string ToString()
        => $"{ModelType.FullName} -> {CharacterId} (unique={IsUnique})";
}