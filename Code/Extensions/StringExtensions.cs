using MyMainFile = DD2ModConfig.Code.MainFile;
using MegaCrit.Sts2.Core.Logging;

namespace DD2ModConfig.Code.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", path);
    }

    public static string CardImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Cards", path);
    }
    public static string RemadeCardImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Cards_Remake", path);
    }

    public static string PowerImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Powers", path);
    }

    public static string RemadePowerImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Powers_Remake", path);
    }

    public static string RelicImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Relics", path);
    }

    public static string PotionImagePath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Potions", path);
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(MyMainFile.ModId, "Images", "Charui", path);
    }
}
