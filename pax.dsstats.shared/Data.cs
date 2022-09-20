using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pax.dsstats.shared;

public static class Data
{
    public static Commander GetCommander(string race)
    {
        return race switch
        {
            "Terran" => Commander.Terran,
            "Protoss" => Commander.Protoss,
            "Zerg" => Commander.Zerg,
            "Abathur" => Commander.Abathur,
            "Alarak" => Commander.Alarak,
            "Artanis" => Commander.Artanis,
            "Dehaka" => Commander.Dehaka,
            "Fenix" => Commander.Fenix,
            "Horner" => Commander.Horner,
            "Karax" => Commander.Karax,
            "Kerrigan" => Commander.Kerrigan,
            "Mengsk" => Commander.Mengsk,
            "Nova" => Commander.Nova,
            "Raynor" => Commander.Raynor,
            "Stetmann" => Commander.Stetmann,
            "Stukov" => Commander.Stukov,
            "Swann" => Commander.Swann,
            "Tychus" => Commander.Tychus,
            "Vorazun" => Commander.Vorazun,
            "Zagara" => Commander.Zagara,
            "Zeratul" => Commander.Zeratul,
            _ => Commander.Terran
        };
    }

    public static GameMode GetGameMode(string gameMode)
    {
        return gameMode switch
        {
            "GameModeBrawlCommanders" => GameMode.BrawlCommanders,
            "GameModeBrawlStandard" => GameMode.BrawlStandard,
            "GameModeBrawl" => GameMode.BrawlStandard,
            "GameModeCommanders" => GameMode.Commanders,
            "GameModeCommandersHeroic" => GameMode.CommandersHeroic,
            "GameModeGear" => GameMode.Gear,
            "GameModeSabotage" => GameMode.Sabotage,
            "GameModeStandard" => GameMode.Standard,
            "GameModeSwitch" => GameMode.Switch,
            "GameModeTutorial" => GameMode.Tutorial,
            _ => GameMode.None
        };
    }

    public static Dictionary<Commander, string> CmdrColor { get; } = new Dictionary<Commander, string>()
        {
            {     Commander.None, "#0000ff"        },
            {     Commander.Abathur, "#266a1b" },
            {     Commander.Alarak, "#ab0f0f" },
            {     Commander.Artanis, "#edae0c" },
            {     Commander.Dehaka, "#d52a38" },
            {     Commander.Fenix, "#fcf32c" },
            {     Commander.Horner, "#ba0d97" },
            {     Commander.Karax, "#1565c7" },
            {     Commander.Kerrigan, "#b021a1" },
            {     Commander.Mengsk, "#a46532" },
            {     Commander.Nova, "#f6f673" },
            {     Commander.Raynor, "#dd7336" },
            {     Commander.Stetmann, "#ebeae8" },
            {     Commander.Stukov, "#663b35" },
            {     Commander.Swann, "#ab4f21" },
            {     Commander.Tychus, "#342db5" },
            {     Commander.Vorazun, "#07c543" },
            {     Commander.Zagara, "#b01c48" },
            {     Commander.Zeratul, "#a1e7e7"  },
            {     Commander.Protoss, "#fcc828"   },
            {     Commander.Terran, "#4a4684"   },
            {     Commander.Zerg, "#6b1c92"   }
        };
}
