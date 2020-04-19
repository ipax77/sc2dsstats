using paxgamelib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgamelib.Service
{
    public static class GameService
    {
        public static async Task<ConcurrentDictionary<int, AddUnit>> GenFight(GameHistory _game, bool shuffle = true)
        {

            if (_game.battlefield == null)
                _game.battlefield = new Battlefield();

            _game.battlefield.Computing = true;

            if (shuffle == true)
            {
                _game.battlefield.Units = new List<Unit>();
                _game.battlefield.Units.AddRange(GameService.ShuffleUnits(_game.Players));
            }

            List<Unit> Units = new List<Unit>(_game.battlefield.Units);


            List<Vector2> pos = new List<Vector2>(UnitService.ResetUnits(_game.battlefield.Units));
            _game.battlefield.UnitPostions = new ConcurrentDictionary<Vector2, bool>();
            foreach (var v in pos)
                _game.battlefield.UnitPostions.TryAdd(v, true);

            _game.battlefield.Def1.Path = new List<KeyValuePair<float, float>>();
            _game.battlefield.Def2.Path = new List<KeyValuePair<float, float>>();
            _game.battlefield.Units.Add(_game.battlefield.Def1);
            _game.battlefield.Units.Add(_game.battlefield.Def2);

            _game.Health = new List<KeyValuePair<float, float>>();
            _game.battlefield.Health = new List<KeyValuePair<KeyValuePair<float, float>, KeyValuePair<float, float>>>();

            ConcurrentDictionary<int, AddUnit> AddUnits = new ConcurrentDictionary<int, AddUnit>();

            int i = 0;
            while (true)
            {
                _game.battlefield.Done = 0;
                _game.battlefield.KilledUnits.Clear();
                _game.battlefield.Units = _game.battlefield.Units.Where(x => x.Healthbar > 0).ToList();

                if (_game.battlefield.Def1.Healthbar == 0 || _game.battlefield.Def2.Healthbar == 0)
                {
                    foreach (var pl in _game.Players)
                        pl.inGame = false;
                    break;
                }

                if (_game.battlefield.Units.Count() <= 2)
                    break;

                List<Unit> enemies1 = new List<Unit>();
                enemies1.AddRange(_game.battlefield.Units.Where(x => x.Owner > 3 && x.Race != UnitRace.Neutral));
                List<Unit> enemies2 = new List<Unit>();
                enemies2.AddRange(_game.battlefield.Units.Where(x => x.Owner <= 3 && x.Race != UnitRace.Neutral));

                foreach (Unit unit in _game.battlefield.Units.Where(x => x.Race == UnitRace.Neutral || x.Race == UnitRace.Decoy))
                {
                    if (!AddUnits.ContainsKey(unit.ID))
                    {
                        AddUnit addunit = new AddUnit();
                        addunit.Delay = i;
                        addunit.Unit = unit;
                        AddUnits[unit.ID] = addunit;
                    }
                }

                int Todo = _game.battlefield.Units.Count();

                foreach (Unit unit in _game.battlefield.Units.ToArray())
                    UnitService.Act(unit, _game.battlefield, enemies1, enemies2);

                int j = 0;
                while (true)
                {
                    if (j > 200 || i > 1200)
                    {
                        Console.WriteLine("fs break");
                        foreach (Unit unit in _game.battlefield.Units)
                        {
                            unit.Shieldbar = 0;
                            unit.Healthbar = 0;
                        }
                        break;
                    }

                    if (_game.battlefield.Done >= Todo)
                    {
                        float hpteam1 = _game.battlefield.Units.Where(x => x.Owner <= 3 && x.Race != UnitRace.Decoy && x.Race != UnitRace.Defence && x.Race != UnitRace.Neutral).Sum(s => s.Healthpoints + s.Shieldpoints);
                        float hpteam2 = _game.battlefield.Units.Where(x => x.Owner > 3 && x.Race != UnitRace.Decoy && x.Race != UnitRace.Defence && x.Race != UnitRace.Neutral).Sum(s => s.Healthpoints + s.Shieldpoints);
                        _game.Health.Add(new KeyValuePair<float, float>(hpteam1, hpteam2));

                        _game.battlefield.Health.Add(new KeyValuePair<KeyValuePair<float, float>, KeyValuePair<float, float>>(new KeyValuePair<float, float>(_game.battlefield.Def1.Healthbar, _game.battlefield.Def1.Shieldbar), new KeyValuePair<float, float>(_game.battlefield.Def2.Healthbar, _game.battlefield.Def2.Shieldbar)));

                        break;
                    }
                    else
                        await Task.Delay(25);

                    // fail safe

                    j++;
                }
                i++;
            }
            _game.battlefield.Computing = false;
            _game.battlefield.Units = new List<Unit>(Units);
            return AddUnits;
        }

        public static async Task<string> GenStyle(GameHistory _game, ConcurrentDictionary<int, AddUnit> AddUnits)
        {
            string style = "";
            int total = _game.battlefield.Def1.Path.Count();

            // def1 and def2 hp/shield animation
            List<KeyValuePair<float, float>> life = new List<KeyValuePair<float, float>>();
            foreach (var ent in _game.battlefield.Health)
                life.Add(ent.Key);
            style += GetDefHpAnimation("HPDefOne", "HPTeamOneAnimation", _game.battlefield.Def1.Healthpoints, life);
            style += GetDefShieldAnimation("SPDefOne", "SPTeamOneAnimation", _game.battlefield.Def1.Shieldpoints, life);
            life.Clear();
            foreach (var ent in _game.battlefield.Health)
                life.Add(ent.Value);
            style += GetDefHpAnimation("HPDefTwo", "HPTeamTwoAnimation", _game.battlefield.Def2.Healthpoints, life);
            style += GetDefShieldAnimation("SPDefTwo", "SPTeamTwoAnimation", _game.battlefield.Def2.Shieldpoints, life);
            life.Clear();

            // army value animation
            style += GetDefHpAnimation("ArmyTeamOne", "ArmyTeamOneAnimation", _game.Health.First().Key, _game.Health, "darkmagenta");
            style += GetDefShieldAnimation("ArmyTeamTwo", "ArmyTeamTwoAnimation", _game.Health.First().Value, _game.Health, "darkmagenta");


            // unit animation
            foreach (Unit unit in _game.battlefield.Units)
            {
                style += GetStyleClass(unit);
                style += GetStyleKeyframes(unit);
            }

            // addunits animation (created during battle)
            foreach (AddUnit addunit in AddUnits.Values)
            {
                if (addunit.Unit.Path.Count() < 4)
                    while (addunit.Unit.Path.Count() < 4)
                        addunit.Unit.Path.Add(addunit.Unit.Path.Last());

                style += GetStyleClass(addunit.Unit, (addunit.Delay * (float)Battlefield.Ticks.TotalSeconds) - (float)Battlefield.Ticks.TotalSeconds);
                style += GetStyleKeyframes(addunit.Unit);

                _game.Units.Add(addunit.Unit);
            }
            return style;
        }

        public static string GetDefShieldAnimation(string mc, string ma, float hpmax, List<KeyValuePair<float, float>> Health, string color = "darkblue")
        {
            string myclass = string.Format(@"
.{0} {{
height: 20vh;
width: 2vw;
opacity: 0.6;
background-color: {1};
animation-name: {2};
animation-duration: {3}s;
animation-timing-function: linear;
}}
", mc, color, ma, Health.Count() * Battlefield.Ticks.TotalSeconds);

            string mykeyframe = string.Format(@"
@keyframes {0} {{
", ma);
            
            bool skip = false;
            for (int i = 0; i < Health.Count(); i++)
            {
                
                int per = 0;
                if (i == Health.Count - 1)
                    per = 100;
                // skip animation if unit did not move
                else if (i > 2 && Health[i].Value == Health[i - 1].Value)
                {
                    // next move animation at propper speed
                    if (skip == true && Health[i + 1].Value == Health[i - 1].Value)
                    {
                        continue;
                    }
                    else if (skip == false)
                    {
                        skip = true;
                        continue;
                    }
                    per = (int)(i * 100 / Health.Count());
                }
                else
                    per = (int)(i * 100 / Health.Count());

                skip = false;
                float shield = MathF.Round(Health[i].Value / hpmax, 2);



                mykeyframe += string.Format(@"{0}% {{
transform: scaleY({1});
transform-origin: bottom;
}}
", per, shield);
                
            }
            mykeyframe += @"}
";
            return myclass + mykeyframe;
        }

        public static string GetDefHpAnimation(string mc, string ma, float hpmax, List<KeyValuePair<float, float>> Health, string color = "darkred")
        {
            string myclass = string.Format(@"
.{0} {{
height: 20vh;
width: 2vw;
opacity: 0.6;
background-color: {1};
animation-name: {2};
animation-duration: {3}s;
animation-timing-function: linear;
}}
", mc, color, ma, Health.Count() * Battlefield.Ticks.TotalSeconds);

            string mykeyframe = string.Format(@"
@keyframes {0} {{
", ma);

            bool skip = false;
            for (int i = 0; i < Health.Count(); i++)
            {

                int per = 0;
                if (i == Health.Count - 1)
                    per = 100;
                // skip animation if unit did not move
                else if (i > 2 && Health[i].Key == Health[i - 1].Key)
                {
                    // next move animation at propper speed
                    if (skip == true && (Health[i + 1].Key == Health[i - 1].Key))
                    {
                        continue;
                    }
                    else if (skip == false)
                    {
                        skip = true;
                        continue;
                    }
                    per = (int)(i * 100 / Health.Count());
                }
                else
                    per = (int)(i * 100 / Health.Count());

                skip = false;
                float hp = MathF.Round(Health[i].Key / hpmax, 2);



                mykeyframe += string.Format(@"{0}% {{
transform: scaleY({1});
transform-origin: bottom;
}}
", per, hp);

            }
            mykeyframe += @"}
";
            return myclass + mykeyframe;
        }

        public static string GetStyleClass(Unit unit, float delay = 0)
        {
            if (delay == 0)
            {
                string myclass = string.Format(@"
.m{0}t {{
    animation-name: m{0}k;
    animation-duration: {1}s;
    animation-timing-function: linear;
    animation-fill-mode: forwards;
}}
", unit.ID, MathF.Round(unit.Path.Count() * (float)Battlefield.Ticks.TotalSeconds, 2));

                return myclass;
            } else
            {
                string myclass = string.Format(@"
.m{0}t {{
    animation-name: m{0}k;
    animation-delay: {1}s;
    animation-duration: {2}s;
    animation-timing-function: linear;
    animation-fill-mode: forwards;
}}
", unit.ID, delay, MathF.Round(unit.Path.Count() * (float)Battlefield.Ticks.TotalSeconds, 2));

                return myclass;
            }
        }

        public static string GetStyleKeyframes(Unit unit)
        {
            string mykeyframe = string.Format(@"
@keyframes m{0}k {{
", unit.ID);

            int count = unit.Path.Count();
            var lastpos = new KeyValuePair<float, float>(0, 0);
            bool skip = false;

            for (int i = 0; i < count; i++)
            {
                float opacity = 1;
                float per = 0;

                // last pos is always at 100%
                if (i == count - 1)
                {
                    opacity = 0;
                    per = 100;
                }
                // last move animation at propper speed
                else if (i == count - 2)
                {
                    opacity = 1;
                    per = MathF.Round(((float)i * 100) / (float)count, 2);
                }
                // skip animation if unit did not move
                else if (unit.Path[i].Key == lastpos.Key && unit.Path[i].Value == lastpos.Value)
                {
                    // next move animation at propper speed
                    if (skip == true && (unit.Path[i + 1].Key == lastpos.Key && unit.Path[i + 1].Value == lastpos.Value))
                    {
                        continue;
                    }
                    else if (skip == false)
                    {
                        skip = true;
                        continue;
                    }
                    per = MathF.Round(((float)i * 100) / (float)count, 2);
                } else
                    per = MathF.Round(((float)i * 100) / (float)count, 2);

                skip = false;
                
                

                lastpos = new KeyValuePair<float, float>(unit.Path[i].Key, unit.Path[i].Value);
                mykeyframe += string.Format(@"{0}% {{
transform: translate({2}vw, {1}vh);
opacity: {3}
}}
", per, unit.Path[i].Key, unit.Path[i].Value, opacity);
            }
            mykeyframe += @"}
";

            return mykeyframe;
        }

        public static List<Unit> ShuffleUnits(List<Player> players)
        {
            List<List<Unit>> EvenLists = new List<List<Unit>>();
            List<List<Unit>> OddLists = new List<List<Unit>>();

            foreach (Player pl in players)
            {
                pl.LastSpawn = pl.GetBuild();

                int i = 0;
                List<Unit> evenlist = new List<Unit>();
                List<Unit> oddlist = new List<Unit>();

                foreach (Unit unit in pl.Units.Where(x => x.Status == UnitStatuses.Placed || x.Status == UnitStatuses.Spawned))
                {
                    i++;
                    if (i % 2 == 0)
                    {
                        evenlist.Add(unit);
                    }
                    else
                    {
                        oddlist.Add(unit);
                    }

                }
                EvenLists.Add(evenlist);
                OddLists.Add(oddlist);
            }

            List<Unit> combined = new List<Unit>();

            for (int i = 0; i < EvenLists.Count(); i++)
            {
                List<Unit> even = EvenLists[i];
                List<Unit> odd = OddLists[i];

                int max = even.Count();
                if (odd.Count() > max)
                    max = odd.Count();

                for (int j = 0; j < max; j++)
                {
                    if (j == max - 1)
                    {
                        if (odd.ElementAtOrDefault(j) != null)
                            combined.Add(odd[j]);
                        if (even.ElementAtOrDefault(j) != null)
                            combined.Add(even[j]);
                    }
                    else
                    {
                        combined.Add(odd[j]);
                        combined.Add(even[j]);
                    }
                }
            }



            return combined;
        }

        public static string GetBigPicture(string img)
        {
            return img.Replace("_tiny", "_t1");
        }

        public static string GetPicture(string img, int pos)
        {
            if (pos <= 3)
                return img.Replace(".png", "_t1.png");
            else
                return img.Replace(".png", "_t2.png");
        }
    }

    public class AddUnit
    {
        public int Delay { get; set; }
        public Unit Unit { get; set; }
    }
}
