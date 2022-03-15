// See https://aka.ms/new-console-template for more information
using sc2dsstats.parse;

Console.WriteLine("Hello, World!");

var replay = await ParseService.ParseReplayAsync(@"C:\Users\pax77\Documents\StarCraft II\Accounts\107095918\2-S2-1-226401\Replays\Multiplayer\Direct Strike (4486).SC2Replay");

if (replay != null)
{
    foreach (var player in replay.DSPlayer)
    {
        Console.WriteLine($"{player.POS}({player.REALPOS}) => {player.NAME}|{player.RACE}");
    }
}

