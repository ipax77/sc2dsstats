# sc2dsstats

sc2dsstats is a C# WPF application to analyze your Starcraft 2 Direct Strike Replays and generates a GD Graph showing the win rate and damage output for each commander. 

To install the app just download and install the setup.exe: 
https://github.com/ipax77/sc2dsstats/blob/master/app.publish/setup.exe

A dotnet-core blazor electron port is available here: https://github.com/ipax77/dsweb_desktop

![sample graph](/images/sample.png)

# Requirements
The GUI requires Microsoft .NET Framework 4.6.1

Chart.js (https://github.com/chartjs) ,Perl using the Python s2_cli script from s2protocol (https://github.com/Blizzard/s2protocol) 
and trueskill (https://github.com/sublee/trueskill) are build in.

# Acknowledgements
Chart.js (https://github.com/chartjs) used for the radar Chart
s2protocol (https://github.com/Blizzard/s2protocol) used for decoding the replays
trueskill (https://github.com/sublee/trueskill) used for matchmaking
IronPython (https://ironpython.net/) to run s2protocol within C#
And all other packages used but not mentioned here.


# License

Copyright (c) 2019, Philipp Hetzner
Open sourced under the GNU General Public License version 3. See the included LICENSE file for more information.

