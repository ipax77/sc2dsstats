# sc2dsstats

sc2dsstats is a dotnet core – blazor - electron app for analyzing your Starcraft 2 Direct Strike Replays. It generates some Graphs showing the win rate, synergy, mvp and damage output of each commander.

Website: https://sc2sstats.pax77.org
Desktop App: https://github.com/ipax77/sc2dsstats/releases/latest

![sample graph](/images/dsweb_desktop.png)

# sc2dsstats.data
* Import replays into database
* Find duplicate replays

# sc2dsstats.decode
* Using IronPython + s2protocol to decode and parse replays

# sc2dsstats.desktop
* ElectronNET ASP .NET Core Balzor client app

# sc2dsstats.lib
* Library with database model/context
* Global data

# sc2dsstats.rest
* REST Server to collect replay stats from clients

# sc2dsstats.shared
* Shared Blazor Pages
* Getting stats from database

# sc2dsstats.web
* ASP .NET Core Blazor website

# paxgamelib
* Basic gameplay used for 'A-move simulator'

# Acknowledgements
* Chart.js (https://github.com/chartjs) used for the radar Chart
* s2protocol (https://github.com/Blizzard/s2protocol) used for decoding the replays
* trueskill (https://github.com/sublee/trueskill) used for matchmaking
* IronPython (https://ironpython.net/) to run s2protocol within C#
And all other packages used but not mentioned here.

# License

Copyright (c) 2020, Philipp Hetzner
Open sourced under the GNU General Public License version 3. See the included LICENSE file for more information.

