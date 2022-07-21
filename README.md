# sc2dsstats

sc2dsstats is a dotnet core – blazor - electron app for analyzing your Starcraft 2 Direct Strike Replays. It generates charts showing the win rate, synergy, mvp and other stats of each commander.

Website: https://sc2dsstats.pax77.org
Desktop App: https://github.com/ipax77/sc2dsstats/releases/latest

![sample graph](/images/dsweb_desktop.png)

# Dependencies
You need to include https://github.com/ipax77/ChartJs.Blazor using the branch chartjsGuidFix to run from source.
# sc2dsstats.decode
* Using IronPython + s2protocol to decode and parse replays

# sc2dsstats.app
* ElectronNET ASP .NET Core Balzor Server app

# sc2dsstats.web
* ASP .NET Core Blazor WASM Website

# sc2dsstats.db
* Database models used for MySQL (Server) and SQLite (App)

# sc2dsstats.rlib
* Razor library used by Server and App

# sc2dsstats.lib
* Needed for converting the old database model

# Acknowledgements
* Electron.NET (https://github.com/ElectronNET/Electron.NET)
* Chart.js (https://github.com/chartjs)
* s2protocol (https://github.com/Blizzard/s2protocol)
* IronPython (https://ironpython.net/)
* ChartJs.Blazor (https://github.com/mariusmuntean/ChartJs.Blazor)

And all other packages used but not mentioned here.

# License

Copyright (c) 2022, Philipp Hetzner
Open sourced under the GNU General Public License version 3. See the included LICENSE file for more information.

