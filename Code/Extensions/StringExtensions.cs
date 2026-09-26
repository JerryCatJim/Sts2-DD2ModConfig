using MyMainFile = DD2ModConfig.Code.MainFile;
using MegaCrit.Sts2.Core.Logging;

namespace DD2ModConfig.Code.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", path);
    }

    public static string CardImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Cards", path);
    }
    public static string RemadeCardImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Cards_Remake", path);
    }

    public static string PowerImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Powers", path);
    }

    public static string RemadePowerImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Powers_Remake", path);
    }

    public static string RelicImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Relics", path);
    }

    public static string PotionImagePath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Potions", path);
    }

    public static string CharacterUiPath(this string path, string modId)
    {
        return Path.Join(modId, "Images", "Charui", path);
    }
}
