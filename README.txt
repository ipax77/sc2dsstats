Generates a graph with global winrate stats.

1. Edit the config.txt file located in the same folder as this file. 

2. Unzip the files precompiled_perl_for_sc2dsstats.zip and precompiled_python_for_sc2dsstats.zip located in the same folder as this file to the current folder.

3. Execute the doit.cmd script

4. If success there will be an stats.png file in the same folder as this file containing your stats.


Analysing the replays will need a lot of diskspace - expect ~800 MB for every 100 DS replays. 

Depending on the number of Replays it will take some time to compute - you can restart the prozess at any point, it will continue at the last position.

The precompiled Perl and Python is for Windoes 10 64bit. If you are using your own Perl and/or Python version you have to modify the file doit.cmd and adjust the Path to the Perl file. The Python path can be set in the config.txt file.
Requirements for Python version 2.7: s2protocol version 4.8.0.71061.1(https://github.com/Blizzard/s2protocol)
Requirements for Perl version 5.22: Config::Simple (https://metacpan.org/pod/Config::Simple), GD::Graph (https://metacpan.org/pod/GD::Graph)


Have fun.