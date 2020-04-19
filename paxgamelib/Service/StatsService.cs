using paxgamelib.Data;
using paxgamelib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace paxgamelib.Service
{
    public static class StatsService
    {
        public static async Task<StatsRound> GenRoundStats(GameHistory _game, bool mstats = true)
        {
            _game.Spawn++;
            float armyvaluet1 = 0;
            float armyvaluet2 = 0;
            if (!_game.Health.FirstOrDefault().Equals(default(KeyValuePair<float, float>)))
            {
                armyvaluet1 = _game.Health.First().Key;
                armyvaluet2 = _game.Health.First().Value;
            }

            int winner = 0;
            if (_game.Health.Any())
            {
                if (_game.Health.Last().Key > 0 && _game.Health.Last().Value == 0)
                    winner = 1;
                else if (_game.Health.Last().Key == 0 && _game.Health.Last().Value > 0)
                    winner = 2;
            }

            StatsRound stats = new StatsRound();
            stats.winner = winner;
            stats.ArmyHPT1 = armyvaluet1;
            stats.ArmyHPT2 = armyvaluet2;

            foreach (Player player in _game.Players.OrderBy(o => o.Pos))
            {
                float damage = 0;
                float killed = 0;
                float army = 0;
                float tech = 0;
                Unit plmvp = new Unit();

                foreach (UnitAbility ability in player.AbilityUpgrades)
                    tech += ability.Cost;

                foreach (UnitUpgrade upgrade in player.Upgrades)
                    try
                    {
                        tech += UpgradePool.Upgrades.SingleOrDefault(x => x.Race == player.Race && x.Name == upgrade.Upgrade).Cost.ElementAt(upgrade.Level - 1).Value;
                    } catch { }


                
                foreach (Unit unit in _game.battlefield.Units.Where(x => x.Status == UnitStatuses.Spawned && x.Owner == player.Pos && x.Race == player.Race))
                {
                    damage += unit.DamageDoneRound;
                    killed += unit.MineralValueKilledRound;
                    army += unit.Cost;

                    unit.DamageDone += damage;
                    unit.MineralValueKilled += killed;

                    if (unit.DamageDoneRound > plmvp.DamageDoneRound)
                        plmvp = unit;
                }
                if (plmvp.DamageDoneRound > stats.MVP.DamageDoneRound)
                    stats.MVP = plmvp;

                stats.Damage.Add(damage);
                stats.Killed.Add(killed);
                stats.Army.Add(army);
                stats.Tech.Add(tech);
                stats.Mvp.Add(plmvp);

                if (mstats == true)
                {
                    M_stats chartstats = new M_stats();
                    chartstats.ArmyHPTeam1 = MathF.Round(stats.ArmyHPT1, 2);
                    chartstats.ArmyHPTeam2 = MathF.Round(stats.ArmyHPT2, 2);
                    chartstats.ArmyValue = MathF.Round(stats.Army.Last(), 2);
                    chartstats.DamageDone = MathF.Round(stats.Damage.Last(), 2);
                    if (winner == 1 && player.Pos <= 3)
                        chartstats.RoundsWon = 1;
                    else if (winner == 2 && player.Pos > 3)
                        chartstats.RoundsWon = 1;
                    chartstats.Upgrades = MathF.Round(stats.Tech.Last(), 2);
                    chartstats.VlaueKilled = MathF.Round(stats.Killed.Last(), 2);
                    player.Stats[_game.Spawn] = chartstats;
                }
            }
            _game.Stats.Add(stats);
            return stats;
        }

        public static float GetScore(GameHistory game)
        {
            float reward = 0;
            Stats result = new Stats();
            Stats oppresult = new Stats();
            StatsRound stats = game.Stats.Last();
            result.DamageDone = stats.Damage[1];
            result.MineralValueKilled = stats.Killed[1];
            oppresult.DamageDone = stats.Damage[0];
            oppresult.MineralValueKilled = stats.Killed[0];

            RESTResult rgame = new RESTResult();
            rgame.Result = stats.winner;
            rgame.DamageP1 = oppresult.DamageDone;
            rgame.MinValueP1 = oppresult.MineralValueKilled;
            rgame.DamageP2 = result.DamageDone;
            rgame.MinValueP2 = result.MineralValueKilled;

            float killsP1 = 0;
            float unitsP1 = 0;
            foreach (Unit unit in game.Players.SingleOrDefault(x => x.Pos == 1).Units.Where(x => x.Status != UnitStatuses.Available))
            {
                unitsP1++;
                killsP1 += unit.Kills;
            }
            float killsP2 = 0;
            float unitsP2 = 0;
            foreach (Unit unit in game.Players.SingleOrDefault(x => x.Pos == 4).Units.Where(x => x.Status != UnitStatuses.Available))
            {
                unitsP2++;
                killsP2 += unit.Kills;
            }

            float minerals = paxgame.Income * game.Spawn;
            float scoreP1 = ((killsP1 / unitsP2) * 3 + ((float)rgame.MinValueP1 / minerals) * 2 + ((float)rgame.DamageP1 / stats.ArmyHPT2)) / 6;
            //float scoreP2 = ((killsP2 / unitsP1) * 3 + ((float)rgame.MinValueP2 / minerals) * 2 + ((float)rgame.DamageP2 / stats.ArmyHPT1)) / 6;
            //float scoreP1 = (((float)rgame.MinValueP1 / minerals) * 2 + ((float)rgame.DamageP1 / stats.ArmyHPT2)) / 3;
            float scoreP2 = (((float)rgame.MinValueP2 / minerals) * 2 + ((float)rgame.DamageP2 / stats.ArmyHPT1)) / 3;

            // u cannot hide
            if (scoreP2 == 0)
                scoreP1 = -10;

            //reward = scoreP1 + 1 - scoreP2;
            reward = scoreP1;
            reward = MathF.Round(reward, 2, MidpointRounding.AwayFromZero);
            return reward;
        }


        public static float GetScore(GameHistory game, int minerals)
        {
            float reward = 0;
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];

            RESTResult rgame = new RESTResult();
            rgame.Result = game.Stats.Last().winner;
            rgame.DamageP1 = oppresult.DamageDone;
            rgame.MinValueP1 = oppresult.MineralValueKilled;
            rgame.DamageP2 = result.DamageDone;
            rgame.MinValueP2 = result.MineralValueKilled;

            float fmins = (float)minerals;
            float scoreP1 = (((float)rgame.MinValueP1 / fmins) * 2 + ((float)rgame.DamageP1 / fmins)) / 3;
            float scoreP2 = (((float)rgame.MinValueP2 / fmins) * 2 + ((float)rgame.DamageP2 / fmins)) / 3;

            reward = scoreP1 + 1 - scoreP2;
            reward = MathF.Round(reward, 1, MidpointRounding.AwayFromZero);
            return reward;
        }
    }


}
