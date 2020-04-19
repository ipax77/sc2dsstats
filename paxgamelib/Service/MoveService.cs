using Microsoft.Extensions.Logging;
using paxgamelib.Data;
using paxgamelib.Models;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgamelib.Service
{
    public static class MoveService
    {
        internal static ILogger _logger;
        
        static readonly List<double> DodgeAngles = new List<double>()
        {
            45,
            135,
            0,
            180,
            325,
            225,
            270
        };


        public static async Task Move(Unit unit, Unit enemyinvision, Battlefield battlefield)
        {
            if (unit.Speed == 0) return;
            Vector2 maintarget = new Vector2();
            float speed = 1;
            float size = 0;
            if (enemyinvision.Name != null)
            {
                maintarget = enemyinvision.RealPos;
                speed = unit.Speed / paxgame.Battlefieldmodifier;
                size = enemyinvision.Size / paxgame.Battlefieldmodifier;
                //speed = unit.Speed;
                if (speed < 1)
                    speed = 1;
                _logger.LogDebug(unit.ID + " moving to enemy " + enemyinvision.ID + " with speed " + speed);
            }
            else if (unit.Owner <= 3)
            {
                //maintarget = battlefield.Def2.RealPos;
                if (unit.RealPos.X >= battlefield.Def2.BuildPos.X - 2)
                    maintarget = battlefield.Def2.RealPos;
                else
                    maintarget = new Vector2(battlefield.Def2.BuildPos.X, unit.BuildPos.Y);
                _logger.LogDebug(unit.ID + " moving to def2");
            }
            else if (unit.Owner > 3)
            {
                //maintarget = battlefield.Def1.RealPos;
                if (unit.RealPos.X <= battlefield.Def1.BuildPos.X + 2)
                    maintarget = battlefield.Def1.RealPos;
                else
                    maintarget = new Vector2(battlefield.Def1.BuildPos.X, unit.BuildPos.Y);
                _logger.LogDebug(unit.ID + " moving to def1");
            }


            Vector2 newpos = new Vector2();
            Vector2 newintpos = new Vector2();

            (newpos, newintpos) = GetTargetPos(speed, unit, maintarget, size);

            int i = 0;
            bool hasTarget = true;

            while (battlefield.UnitPostions.Keys.Contains(newintpos))
            {
                Vector2 dodgetarget = RotatePoint(maintarget, unit.RealPos, DodgeAngles[i]);
                (newpos, newintpos) = GetTargetPos(speed, unit, dodgetarget, size);
                i++;
                if (i > 6)
                {
                    hasTarget = false;
                    break;
                }
            }

            if (hasTarget == true)
            {
                battlefield.UnitPostions.TryRemove(unit.Pos, out _);
                battlefield.UnitPostions.TryAdd(newintpos, true);
                _logger.LogDebug(unit.ID + " Moving to " + newpos + " (" + i + ")");
                unit.Pos = newintpos;
                unit.RealPos = newpos;
                unit.LastRelPos = unit.RelPos;
                unit.RelPos = GetRelPos(unit.RealPos);
            }
            else
                _logger.LogDebug(unit.ID + " stuck");
        
        }

        public static (Vector2, Vector2) GetTargetPos(float speed, Unit unit, Vector2 maintarget, float size)
        {
            float d = Vector2.Distance(unit.RealPos, maintarget);
            d -= size;
            if (d < 0)
                d *= -1;
            float t = 1;
            if (d > 0)
            {
                t = speed / d;
                if (t > 1)
                    t = 1;
            }

            Vector2 newpos = new Vector2();
            newpos.X = (1 - t) * unit.RealPos.X + t * maintarget.X;
            newpos.Y = (1 - t) * unit.RealPos.Y + t * maintarget.Y;

            // TODO depeding of unit size
            Vector2 newintpos = new Vector2((float)Math.Round(newpos.X, 1, MidpointRounding.AwayFromZero), (float)Math.Round(newpos.Y, 1, MidpointRounding.AwayFromZero));
            //Vector2 newintpos = new Vector2((int)newpos.X, (int)newpos.Y);
            return (newpos, newintpos);
        }

        public static KeyValuePair<float, float> GetRelPos(Vector2 pos)
        {
            float distance_left = Vector2.Distance(new Vector2(0, pos.Y), pos);
            float distance_top = Vector2.Distance(new Vector2(pos.X, 0), pos);
            float distance_left_percent = MathF.Round(distance_left * 100 / Battlefield.Xmax, 2);
            float distance_top_percent = MathF.Round(distance_top * 100 / Battlefield.Ymax, 2);

            return new KeyValuePair<float, float>(distance_top_percent, distance_left_percent);
        }

        public static Vector2 RotatePoint(Vector2 pointToRotate, Vector2 centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Vector2
            {
                X =
                    (float)
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X),
                Y =
                    (float)
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y)
            };
        }
    }
}
