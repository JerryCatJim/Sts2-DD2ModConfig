using DD2ModConfig.Code.ResoluteOrMeltdown;

namespace DD2ModConfig.Code.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class RMRegisterAttribute : Attribute
{
    private string? _characterId;
    public ResoluteOrMeltdownType RMType { get; }
    public bool IsUnique { get; }

    public RMRegisterAttribute(string characterId, ResoluteOrMeltdownType rmType, bool isUnique = false)
    {
        _characterId = characterId;
        RMType = rmType;
        IsUnique = isUnique;
    }

    public RMRegisterAttribute(Type characterType, ResoluteOrMeltdownType rmType, bool isUnique = false)
    {
        CharacterType = characterType;
        RMType = rmType;
        IsUnique = isUnique;
    }

    public Type? CharacterType { get; }

    public string CharacterId => _characterId ??= ResolveCharacterId();

    private string ResolveCharacterId()
    {
        if (CharacterType == null) return string.Empty;

        try
        {
            // 不能实例化 CharacterModel，AbstractModel 构造会向 ModelDb 重复注册。
            // 直接通过 ModelDb.GetId<T>() 拿到已注册的 ModelId，再取 Entry。
            var getIdMethod = typeof(MegaCrit.Sts2.Core.Models.ModelDb)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "GetId" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 0);

            if (getIdMethod == null)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error(
                    "[RMAttributes] ModelDb.GetId<T>() not found.");
                return string.Empty;
            }

            var closed = getIdMethod.MakeGenericMethod(CharacterType);
            var modelId = closed.Invoke(null, null);
            if (modelId == null)
            {
                MegaCrit.Sts2.Core.Logging.Log.Error(
                    $"[RMAttributes] ModelDb.GetId<{CharacterType.Name}>() returned null.");
                return string.Empty;
            }

            var entryProp = modelId.GetType().GetProperty("Entry");
            var entry = entryProp?.GetValue(modelId) as string;
            if (string.IsNullOrEmpty(entry))
            {
                MegaCrit.Sts2.Core.Logging.Log.Error(
                    $"[RMAttributes] ModelId.Entry for {CharacterType.Name} is empty.");
                return string.Empty;
            }

            return entry;
        }
        catch (System.Reflection.TargetInvocationException e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error(
                $"[RMAttributes] Failed to resolve CharacterId from {CharacterType}: {e.InnerException ?? e}");
            return string.Empty;
        }
        catch (Exception e)
        {
            MegaCrit.Sts2.Core.Logging.Log.Error(
                $"[RMAttributes] Failed to resolve CharacterId from {CharacterType}: {e}");
            return string.Empty;
        }
    }
}