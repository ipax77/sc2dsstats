using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using paxgamelib.Models;
using paxgamelib.Data;

namespace paxgamelib.Service
{
    public static class GetMovesService
    {
        private static readonly HttpClient client = new HttpClient();
        private const string RESTurl = "http://192.168.178.35:5077";

        public static async Task<string> TestMsg()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/string"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var stringTask = client.GetStringAsync("http://paxpy:5077/test");

            var msg = await stringTask;
            return msg;
        }

        public static async Task<string> GetMove(float[][] board, float[] moves)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            float[][][] msg = new float[2][][];
            for (int i = 0; i < 20; i++)
            {
                msg[0] = new float[20][];
                for (int j = 0; j < 61; j++)
                    msg[0][i] = new float[61];
            }

            msg[1] = new float[1][];
            msg[1][0] = new float[3 * 20 * 60 + 3];

            msg[0] = board;
            msg[1][0] = moves;

            string stmsg = JsonSerializer.Serialize(msg);
            //var stringTask = client.PostAsync("http://paxpy:5077/getmove", new StringContent(stmsg, Encoding.UTF8, "application/json"));
            DateTime a = DateTime.UtcNow;
            var stringTask = client.PostAsync("http://localhost:5077/getmove", new StringContent(stmsg, Encoding.UTF8, "application/json"));
            TimeSpan t1 = DateTime.UtcNow - a;
            var action = await stringTask;
            TimeSpan t2 = DateTime.UtcNow - a;
            var result = action.Content.ReadAsStringAsync();
            TimeSpan t3 = DateTime.UtcNow - a;
            RESTResultAction ra = new RESTResultAction();
            ra = JsonSerializer.Deserialize<RESTResultAction>(result.Result);
            TimeSpan t4 = DateTime.UtcNow - a;

            Console.WriteLine(t1.TotalSeconds);
            Console.WriteLine(t2.TotalSeconds);
            Console.WriteLine(t3.TotalSeconds);
            Console.WriteLine(t4.TotalSeconds);

            return ra.task;
        }

        public static async Task<string> GetMoveFaster(string moves)
        {
            /*
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/string"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
            */

            //var stringTask = client.GetStringAsync("http://paxpy:5077/getsmove/" + moves);
            var stringTask = client.GetStringAsync("http://192.168.178.35:5077/getsmove/" + moves);
            var msg = await stringTask;
            RESTResultAction ra = new RESTResultAction();
            ra = JsonSerializer.Deserialize<RESTResultAction>(msg);
            return ra.task;
        }

        public static async Task<int> GetMoveNG(Player _player, Player _opp)
        {
            string getstring = _player.Game.ID.ToString();
            getstring += "Y";
            getstring += String.Join("X", _player.GetAIMoves());
            getstring += "Y";
            getstring += String.Join("X", _opp.GetAIMoves());
            var stringTask = client.GetStringAsync(RESTurl + "/getsmove/" + getstring);
            var msg = await stringTask;
            RESTResultAction ra = new RESTResultAction();
            ra = JsonSerializer.Deserialize<RESTResultAction>(msg);
            int move = 0;
            int.TryParse(ra.task, out move);
            return move;
        }

        public static object ActionToMove(int action)
        {
            Unit unit = new Unit();
            UnitAbility ability;
            UnitUpgrade upgrade;
            Object obj = new object();
            int num = 0;
            if (action == 3601)
            {
                ability = AbilityPool.Abilities.Where(x => x.Ability == UnitAbilities.Stimpack).SingleOrDefault().DeepCopy();
                obj = ability;
            } else if (action == 3602)
            {
                ability = AbilityPool.Abilities.Where(x => x.Ability == UnitAbilities.CombatShield).SingleOrDefault().DeepCopy();
                obj = ability;
            } else if (action == 3603)
            {
                ability = AbilityPool.Abilities.Where(x => x.Ability == UnitAbilities.ConcussiveShells).SingleOrDefault().DeepCopy();
                obj = ability;
            } else
            {
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 20; j++)
                        for (int k = 0; k < 60; k++)
                        {
                            if (num == action)
                            {
                                if (i == 0)
                                    unit = UnitPool.Units.Where(x => x.Name == "Marine").SingleOrDefault().DeepCopy();
                                else if (i == 1)
                                    unit = UnitPool.Units.Where(x => x.Name == "Marauder").SingleOrDefault().DeepCopy();
                                else if (i == 2)
                                    unit = UnitPool.Units.Where(x => x.Name == "Reaper").SingleOrDefault().DeepCopy();
                                
                                unit.PlacePos = new System.Numerics.Vector2((float)k / 2, (float)j / 2);
                                obj = unit;
                            }
                            num++;
                        }
            }


            return obj;
        }

    }

    public class RESTResultAction
    {
        public string task { get; set; }
    }
}
