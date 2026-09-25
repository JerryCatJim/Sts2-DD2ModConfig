using DD2ModConfig.Code.Monster.DeathMonster;

namespace DD2ModConfig.Code.Monster;

public static class MonsterAudioCfg
{
    public static string GetPathByMonsterName(string audioName, string monsterName)
    {
        switch (monsterName)
        {
            case "Death":
                return GetDeathPath(audioName);
            default:
                return "";
        }
    }
    public static string GetDeathPath(string audioName)
    {
        string FileName = "";
        string PathHead = "res://DD2ModConfig_Asset/Monster_Death/Sound_Death/";
        string FileEnd = ".wav";

        switch (audioName)
        {
            case "Spawn":
                FileName = "death_intro_v4";
                break;
            case "Hit":
                FileName = "vo_monster_death_hurt_0" + (DeathListenForRunStateSingleton.HitCount + 1);
                DeathListenForRunStateSingleton.HitCount++;
                break;
            case "Dead":
                FileName = "sfx_death_death";
                break;

            //Attack_XXX
            case "Attack_Point":
                FileName = "sfx_death_mm_antic";
                break;
            case "Attack_B":
                FileName = "sfx_death_reaver_antic";
                break;
            case "Attack_C":
                FileName = "sfx_death_waning_antic";
                break;
            case "Attack_Trample":
                FileName = "sfx_death_trample_antic";
                break;

            //XXX_Action
            case "Point_Action":
                FileName = "sfx_death_mm_use";
                break;
            case "B_Action":
                FileName = "sfx_death_reaver_use";
                break;
            case "C_Action":
                FileName = "sfx_death_waning_use";
                break;
            case "Trample_Action":
                FileName = "sfx_death_trample_use";
                break;

            //XXX_Recover
            case "Point_Recover":
                FileName = "sfx_death_mm_return";
                break;
            case "B_Recover":
                FileName = "sfx_death_shadow_return";
                break;
            case "C_Recover":
                FileName = "sfx_death_waning_return";
                break;
            case "Trample_Recover":
                FileName = "sfx_death_trample_return";
                break;

            //DoNothing
            default:
                break;
        }
        return FileName == "" ? FileName : PathHead + FileName + FileEnd;
    }
}
