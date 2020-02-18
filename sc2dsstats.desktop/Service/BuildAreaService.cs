using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using paxgamelib.Models;
using paxgamelib.Data;
using System.Numerics;
using paxgamelib;
using paxgamelib.Service;

namespace sc2dsstats.desktop.Service
{


    public static class BuildAreaService
    {
        public static Unit ClickHandler(Player _player, double mouseX, double mouseY, string areaPos, Unit container, List<Unit> units)
        {
            Unit unit = null;
            if (areaPos == "") return unit;

            string[] areaCoord = areaPos.Split('|');
            if (areaCoord.Count() != 4) return unit;

            float top = 0;
            float right = 0;
            float bottom = 0;
            float left = 0;
            float.TryParse(areaCoord[0], out top);
            float.TryParse(areaCoord[1], out right);
            float.TryParse(areaCoord[2], out bottom);
            float.TryParse(areaCoord[3], out left);

            Vector2 newpos = new Vector2((float)mouseX - left, (float)mouseY - top);
            Vector2 newintpos = new Vector2((float)Math.Round((newpos.X / 1000) * paxgame.Buildareasize.Y, MidpointRounding.AwayFromZero) / 2, (float)Math.Round((newpos.Y / 400) * paxgame.Buildareasize.X, MidpointRounding.AwayFromZero) / 2);
            unit = units.SingleOrDefault(x => x.PlacePos == newintpos && x.Status != UnitStatuses.Available);

            if (container != null && container.Status == UnitStatuses.Available && unit == null)
            {
                unit = UnitPool.GetCopy(container.Name);
                unit.PlacePos = newintpos;
                return unit;
            }
            else if (unit != null)
                return unit;
            else if (container != null && container.Status != UnitStatuses.Available && unit == null)
            {
                container.PlacePos = newintpos;
                UnitService.NewUnitPos(_player, container);
                return null;
            }
            else
                return null;
        }

        public static void Swap(Player _player, Player _opp)
        {
            string pl = _player.GetString();
            string opp = _opp.GetString();
            _player.SetString(opp);
            _opp.SetString(pl);
        }

        public static async Task ResetBuildAreaClass(BACSSClass cssclass)
        {
            lock (cssclass)
                cssclass.delay += BACSSClass.dspan;

            await Task.Delay(cssclass.delay);

            lock (cssclass)
            {
                cssclass.delay -= BACSSClass.dspan; ;
                if (cssclass.delay <= 0)
                {
                    cssclass.delay = 0;
                    cssclass.buildareaclass = "buildarea";
                }
            }
        }

        public static async Task BotTerranAI(Player _opp)
        {
            while (_opp.MineralsCurrent >= 0)
                await GetMove(_opp);
        }

        public static async Task GetMove(Player _opp)
        {
            string action = await GetMovesService.GetMoveFaster(String.Join('X', _opp.GetAIMoves()));
            int move = int.Parse(action);
            Object obj = GetMovesService.ActionToMove(move);
            if (obj is Unit)
            {
                Unit unit = obj as Unit;
                UnitService.NewUnit(_opp, unit);
                _opp.MineralsCurrent -= unit.Cost;
                _opp.Units.Add(unit);

            }
            else if (obj is UnitAbility)
            {
                UnitAbility ab = obj as UnitAbility;
                _opp.MineralsCurrent -= UnitService.AbilityUpgradeUnit(ab, _opp);
            }
        }
    }

    public class BACSSClass
    {
        private string _buildareaclass = "buildarea";
        public const int dspan = 500;
        public string cursorclass { get; set; } = "";
        public string rotateclass { get; set; } = "";
        public string buildareaclass
        {
            get { return _buildareaclass; }
            set
            {
                lock (this)
                    _buildareaclass = value;
                BuildAreaService.ResetBuildAreaClass(this);
            }
        }
        public int delay { get; set; } = 0;
    }
}

