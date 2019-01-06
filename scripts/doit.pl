# Copyright (c) 2019 Philipp Hetzner
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.


use strict;
use warnings;

use POSIX qw(strftime);
use File::Basename;
use GD::Graph::bars;
use File::Copy;
use Encode qw(decode encode);
use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";
use Config::Simple;

my $DEBUG = 2;

my $main_path = $ARGV[0];
$main_path =~ s/\\/\//g;

my $config_file = $main_path . "/config.txt";
my %cfg;

print "Reading in Config file ..\n" if $DEBUG;
Config::Simple->import_from($config_file, \%cfg);

if (defined $cfg{'default.player'}) {
	$cfg{'default.player'} = encode("UTF-8", $cfg{'default.player'});
}

foreach my $a (keys %cfg) {
	print $a . " => " . $cfg{$a} . "\n" if $DEBUG > 1;
}

$DEBUG = $cfg{'default.DEBUG'};

print "Hello World - Main path = $main_path\n" if $DEBUG > 1;


# Defining basic variables
#

my $python = $main_path . "python2.7/python-2.7.13/python.exe";
if (defined $cfg{'default.python_path'}) {
	$python = $cfg{'default.python_path'};
}
my $p_script = $main_path . "python2.7/python-2.7.13/Lib/site-packages/s2protocol/s2_cli.py";
my $store_path = $main_path . "analyzes";
my $stats_dir = $store_path;
my $stats_file = $main_path . "stats.csv";
my $png = $main_path . "stats.png";
my $daily_png = $main_path . "daily.png";
my $png_dps = $main_path . "dps.png";
my $latest_txt = $main_path . "latest.txt";

my $start_date = "19700101000000";
my $end_date = strftime("%Y%m%d%H%M%S", localtime());

my %detail;

my $games = 0;
my %replays;

my %sum;
my %skipmsg;


# Reading in maybe already existing stats file so we don't have to compute already known data
#

&ReadCSV($stats_file, \%sum);


# If start_date is defined in the config file make seperate data files
#

if (defined $cfg{'default.start_date'}) {
	$start_date = $cfg{'default.start_date'};
}

if (defined $cfg{'default.end_date'}) {
	$end_date = $cfg{'default.end_date'};	
}

if (defined $cfg{'default.start_date'} || defined $cfg{'default.end_date'}) {
	my $start_date_m;
	if ($start_date =~ /^(\d{8})/) {
		$start_date_m = $1;	
	}
	my $end_date_m;
	if ($end_date  =~ /^(\d{8})/) {
		$end_date_m = $1;	
	}
	$stats_file = $main_path . "stats_" . $start_date_m . "_TO_" . $end_date_m . ".csv";
	$png = $main_path . "stats_" . $start_date_m . "_TO_" . $end_date_m . ".png"; 
	$png_dps = $main_path . "dps_" . $start_date_m . "_TO_" . $end_date_m . ".png";	
}


###

print "Working on replays between $start_date and $end_date ..\n" if $DEBUG;

my $csv = $stats_file;

my $player = $cfg{'default.player'};
my @getpool = ('messageevents', 'trackerevents', 'details', 'gameevents');


# Decoding the replays using s2protocol and python
#

my $todo_replays = 0;
opendir(REP, $cfg{'default.replay_path'}) or die "Could not read dir $cfg{'default.replay_path'}: $!\n";
while (my $p = readdir(REP)) {
	next if $p =~ /^\./;
	if ($p =~ /^Direct Strike/ || $p =~ /^Desert Strike/) {
		$todo_replays ++;
	}
}	
closedir(REP);

my $done_replays = 1;
opendir(REP, $cfg{'default.replay_path'}) or die "Could not read dir $cfg{'default.replay_path'}: $!\n";
while (my $p = readdir(REP)) {
	next if $p =~ /^\./;
	if ($p =~ /^Direct Strike/ || $p =~ /^Desert Strike/) {
		my $replay = $cfg{'default.replay_path'} . "/" . $p;
		print "Working on $replay ..($done_replays out of $todo_replays done)\n" if $DEBUG;
		my $info_path = &Info($replay);
		$done_replays ++;
	}
}
closedir(REP);


#  Reading in maybe already existing stats file so we don't have to compute already known data
#

if (-e $stats_file) {
        open(SUM, "<", $stats_file) or die "Could not read $stats_file: $!\n";
        while (<SUM>) {
        	chomp;
			if (/^\d/) {
				my @line = split(/;/, $_);
				my $game = $line[0];
				my $replay = $line[1];
				
				$game =~ s/\s+//g;
				$replay =~ s/^\s+//;
				
 		        $replays{$replay} = 1;
 		        $games = $game;
 		        
			}
        }
}


# Extracting data from the decoded Replays
#

my $gametime;
my $new_id = 0;
my $id_count = 1;

opendir(STAT, $stats_dir) or die "Could not read dir $stats_dir: $!\n";
while (my $p = readdir(STAT)) {
        next if ($p =~ /^\./);
		next if ($p !~ /^Direct/);
        my $id;
        if ($p =~ /(.*)\.SC2Replay/) {
                $id = $1;
        }

        if (defined $replays{$id}) {
        	print "Skipping $id ..\n" if $DEBUG > 1;
        	next;	
        } else {
        	if ($id ne $new_id) {
        		print "$id_count out of $todo_replays done.\n" if $DEBUG;
        		$new_id = $id;
        		$id_count ++;
        		
        		#my $replay = $cfg{'default.replay_path'} . "/" . $id . ".SC2Replay";
	        	#if (-e $replay) {
	        	#	$gametime=POSIX::strftime( "%Y%m%d%H%M%S", localtime(( stat $replay )[9] ) );	
	        	#}  
				$games ++;	
        		
        	}
        	print "Working on $id ..\n" if  $DEBUG;	
        }
        
      
        if (defined $sum{$id}) {
        	
        	print "Fetching data from global csv file for $id ..\n" if $DEBUG > 1;
        	
        	my $i = 0;
        	foreach my $g (keys %{ $sum{$id} } ) {
        		$detail{$id}{$i}{'NAME'} = $g;
        		$detail{$id}{$i}{'RACE'} = $sum{$id}{$g}{'RACE'};
        		$detail{$id}{$i}{'TEAM'} = $sum{$id}{$g}{'TEAM'};
        		$detail{$id}{$i}{'RESULT'} = $sum{$id}{$g}{'RESULT'};
        		$detail{$id}{$i}{'KILLSUM'} = $sum{$id}{$g}{'KILLSUM'};
        		$detail{$id}{$i}{'DURATION'} = $sum{$id}{$g}{'DURATION'};
        		$detail{$id}{$i}{'GAMETIME'} = $sum{$id}{$g}{'GAMETIME'};
        		$detail{$id}{$i}{'GAMES'} = $games;
        		$i++;
        	}
        	
        } else {
        	
        	print "Reading in data from $id ..\n" if $DEBUG > 1;
        	
			my $duration;
			my $player_count = 0;
	
	        if ($p =~ /SC2Replay\.txt$/ || $p =~ /SC2Replay_details\.txt$/) {
	                my $stat_file = $stats_dir . "/" . $p;
	
	                $games ++;
	                $player_count = 0;
	
	                my $win_t0 = 0;
	                my $win_t1 = 0;
	                my $myteam = 0;
	
	                open(ST, "<", $stat_file) or die "Could not read $stat_file: $!\n";
	                        while (<ST>) {
		                        if (/m_name/) {
			                        if (/([\\\w]*)',$/) {
			                                $player_count++;
			                                my $name = $1;
			                                if ($name =~ /\\/) {
			                                	$name =~ s/\\x(..)/chr hex $1/ge;
			                                	$name = encode("UTF-8", $name);
			                                }
			                                $detail{$id}{$player_count}{'NAME'} = $name;
			                                $detail{$id}{$player_count}{'GAMES'} = $games;
											if ($name eq $cfg{'default.player'}) {
												$skipmsg{$id}{'PLAYER'} = $player_count;	
											}
			                        }
	               				 }
	
	       		                 if (/m_race/) {
			                        if (/([\\\w]*)',$/) {
			                                my $race = $1;
			                                if ($race =~ /\\/) {
			                                	$race =~ s/\\x(..)/chr hex $1/ge;
			                                }
			                                
			                                $detail{$id}{$player_count}{'RACE'} = $race;
			                        }
	             			    }
	
	                        if (/m_result/) {
		                        if (/(\d+),$/) {
		                                my $result = $1;
		                                $detail{$id}{$player_count}{'RESULT'} = $result;
		                        }
	                  	    }
	
	                        if (/m_teamId/) {
		                        if (/\s(\d*),$/) {
		                                my $teamid = $1;
		                                $detail{$id}{$player_count}{'TEAM'} = $teamid;
		
		                                if ($detail{$id}{$player_count}{'NAME'} eq $player) {
		                                        $myteam = $teamid;
		                                }
		
		
		                                if ($teamid == 0) {
		                                        $win_t0 +=      $detail{$id}{$player_count}{'RESULT'};
		                                } elsif ($teamid == 1) {
		                                        $win_t1 +=      $detail{$id}{$player_count}{'RESULT'};
		                                }
		                        }
	                        }
	                        
	                        if (/m_timeUTC/) {
	                        	if (/(\d+)L,$/) {
	                        		#$gametime = $1;	
	                        		
	                        	}	
	                        }
	
	
	
	                        }
	                close(ST);
	        } elsif ($p =~ /_trackerevents\.txt$/) {
	                my $stat_file = $stats_dir . "/" . $p;
	                my $playerid;
	
	                open(TRACKEREVENTS, "<", $stat_file) or die "Could not read $stat_file: $!\n";
	
	                while (<TRACKEREVENTS>) {
	
		                if (/m_controlPlayerId/) {
			                if (/(\d+),$/) {
		    	                $playerid = $1;
		        	        }
		                } elsif (/m_unitTypeName/) {
		                        if (/'(\w*)',$/) {
			                        my $unit = $1;
			                        if ($unit =~ /^Worker(.*)/) {
			                                my $race2 = $1;
			                                $detail{$id}{$playerid}{'RACE2'} = $race2;
			                         }
		                        }
		 	        	 } elsif (/_gameloop/) {
			        	 	if (/(\d+),$/) {
								$duration = $1;	
			        	 	}
			        	 	
			        	 } elsif (/m_playerId/) {
			        	 	if (/(\d),$/) {
			        	 		$playerid = $1;	
			        	 	}	
			        	 } elsif (/m_scoreValueMineralsKilledArmy/) {
			                if (/(\d+),$/) {
			                        my $killarmy = $1;
			                        $detail{$id}{$playerid}{'KILLSUM'} = $killarmy;
			                        $detail{$id}{$playerid}{'DURATION'} = $duration;
			                        
			                        if (!defined $detail{$id}{$playerid}{'GAMETIME'}) {
				                        my $replay = $cfg{'default.replay_path'} . "/" . $id . ".SC2Replay";
	        							if (-e $replay) {
	        								$gametime=POSIX::strftime( "%Y%m%d%H%M%S", localtime(( stat $replay )[9] ) );	
						        		}   	
				                        $detail{$id}{$playerid}{'GAMETIME'} = $gametime;
			                        }
			                }
		        		}
	                }
		        	 
	              
	               
	
	                close(TRACKEREVENTS);
	        } elsif ($p =~ /_messageevents\.txt$/ && defined($cfg{'default.SKIPMSG'})) {
	        	if ($cfg{'default.SKIPMSG'}) {
		        	my $stat_file = $stats_dir . "/" . $p;
		        	
		        	my $playerid;
		        	my $gameloop;
		        	my $msgevent;
		        	my $msg;
		        	open(MSGEVENTS, "<", $stat_file) or die "Could not read $stat_file: $!\n";
		        	
		        	while (<MSGEVENTS>) {
		        		
		        		if (/m_userId/) {
		        			if (/(\d)\},$/) {
		        				$playerid = $1;
		        			}	
		        			
		        		} elsif (/_gameloop/) {
		        			if (/(\d+),$/) {
		        				$gameloop = $1;	
		        			}		
		        		} elsif (/SChatMessage/) {
		        			$msgevent = 1;	
		        		} elsif (/m_string/) {
		        			if (/'([^']*)'\}$/) {
		        				$msg = $1;
		        				if ($msg eq "skipdsstats") {
			        				if ($gameloop < 2000 && $msgevent) {
			        					$skipmsg{$id}{$playerid + 1} = 1;
			        				}
		        				}
		        			}	
		        		}
		        	}
		        	
		        	close(MSGEVENTS);
	        	}	        	
	        }
        }
}
closedir(STAT);

# Writing summary into a csv file while skipping some
#

my $skip_normal = 0;

if (defined $cfg{'default.skip_normal'}) {
	$skip_normal = $cfg{'default.skip_normal'};
}

my $duration_skip = 0;

open(SUM, ">>", $stats_file) or die "Could not write to $stats_file: $!\n";
foreach my $id (sort keys %detail) {
        #foreach  my $d (sort {$a <=> $b} keys %{ $detail{$id} }) {
        foreach  my $d (sort keys %{ $detail{$id} }) {
                if (!defined $detail{$id}{$d}{'RACE2'}) {
                        $detail{$id}{$d}{'RACE2'} = $detail{$id}{$d}{'RACE'};
                }
                
             
                my $gametime;
                if (defined $detail{$id}{$d}{'GAMETIME'}) {
                	$gametime = $detail{$id}{$d}{'GAMETIME'};
                } else {
                	print "Skipping $id due to no gametime available :(\n" if $DEBUG > 1;
                	next;	
                }
                
                
                				
				#my $duration = $detail{$id}{$d}{'DURATION'} / 24.4;
				#$duration = sprintf("%.2f", $duration);
				
				my $duration;
				if (defined $detail{$id}{$d}{'DURATION'}) {
					$duration = $detail{$id}{$d}{'DURATION'};
				} else {
					print "Skipping $id due to no duration available :(\n" if $DEBUG > 1;
					next;
				}
				
				if (defined $cfg{'default.SKIPMSG'}) {
					if (defined $skipmsg{$id}{'PLAYER'}) {
						if (defined $skipmsg{$id}{$skipmsg{$id}{'PLAYER'}} && $skipmsg{$id}{$skipmsg{$id}{'PLAYER'}}) {
							print "Skipping $id due to skipmsg\n" if $DEBUG > 1;
							next;
						}
					}	
				}
				

				if ($duration && $gametime  && defined $detail{$id}{$d}{'GAMES'} && defined $detail{$id}{$d}{'NAME'} && defined $detail{$id}{$d}{'RACE2'} && defined $detail{$id}{$d}{'TEAM'} && defined $detail{$id}{$d}{'RESULT'}) {	
	                if ($gametime >= $start_date) { 
	                	if ($gametime <= $end_date) {
	                		
			                	print $detail{$id}{$d}{'GAMES'} . "; " . $id . "; " . $detail{$id}{$d}{'NAME'} . "; " . $detail{$id}{$d}{'RACE'} . "; " . $detail{$id}{$d}{'RACE2'}. "; " . $detail{$id}{$d}{'TEAM'} . "; " . $detail{$id}{$d}{'RESULT'} . "; " . $detail{$id}{$d}{'KILLSUM'} . "; " . $duration . "; " . $gametime . "; " . $d . ";\n" if $DEBUG > 1;
			                	print SUM $detail{$id}{$d}{'GAMES'} . "; " . $id . "; " . $detail{$id}{$d}{'NAME'} . "; " . $detail{$id}{$d}{'RACE'} . "; " . $detail{$id}{$d}{'RACE2'}. "; " . $detail{$id}{$d}{'TEAM'} . "; " . $detail{$id}{$d}{'RESULT'} . "; " . $detail{$id}{$d}{'KILLSUM'} . "; " . $duration . "; " . $gametime . "; " . $d . ";\n";
	                	} else {
	                		print "Skipping $id because of gametime ($start_date <= $gametime => $end_date)\n" if $DEBUG > 1;
	                	}
	                } else {
	                	print "Skipping $id because of gametime ($start_date <= $gametime => $end_date)\n" if $DEBUG > 1;	
	                }
				} else {
					print "Skipping entry for $id for unknown reasons :(\n" if $DEBUG > 1;	
				}
        }
}

close(SUM);

# Collecting and summarizing the givien data
#

my @races = split(/,/, $cfg{'default.commanders'});

print "Generating png for player $player ..\n" if $DEBUG;

%sum = ();
my %global;
my %vsglobal;

my @duration;

my %daily;
my %dglobal;
my @dduration;
my %dvsglobal;
my %sum2;


&ReadCSV($csv, \%sum);

foreach my $replay (keys %sum) {
	foreach my $name (keys %{ $sum{$replay} }) {
		
		my $race2 = $sum{$replay}{$name}{'RACE'};
		my $team = $sum{$replay}{$name}{'TEAM'};
		my $win = $sum{$replay}{$name}{'RESULT'};
		my $killsum = $sum{$replay}{$name}{'KILLSUM'};
		my $duration = $sum{$replay}{$name}{'DURATION'};
		my $gametime = $sum{$replay}{$name}{'GAMETIME'};
	
	
	
		if ($name eq $player) {

			my $d_skip = 0;
	
			if ($skip_normal) {
				if ($race2 eq "Zerg" || $race2 eq "Terran" || $race2 eq "Protoss") {
					$d_skip = 1;
					print "Skipping stats for $replay due to skip_normal\n" if $DEBUG > 1;	
				}
				
			}
			
	
			if (defined $cfg{'default.SKIP'}) {
				my $d_min = $duration / 24.4;
				if ($d_min < $cfg{'default.SKIP'}) {
					$d_skip = 1;
					print "Skipping stats for $replay due to duration ($d_min)\n" if $DEBUG > 1;
				}
			} 
			
			if (! $d_skip) {
				push(@duration, $duration);
			
				$global{'GAMES'} ++;
				if ($race2 eq "Terran" || $race2 eq "Zerg" || $race2 eq "Protoss") {
					$global{'GAMESNORMAL'} ++;
					if ($win == 1) {
						$global{'PAXWINNORMAL'} ++;	
					} else {
						$global{'PAXLOSNORMAL'} ++;
					}
				} else {
					$global{'GAMESCOMMANDER'} ++;
					if ($win == 1) {
						$global{'PAXWINCOMMANDER'} ++;	
					} else {
						$global{'PAXLOSCOMMANDER'} ++;
					}
					
				}
			}
			
			if (defined $cfg{'default.DAILY'}) {
				
				if ($gametime >= $cfg{'default.DAILY'}) {
					push(@dduration, $duration);
				
					$dglobal{'GAMES'} ++;
					if ($race2 eq "Terran" || $race2 eq "Zerg" || $race2 eq "Protoss") {
						$dglobal{'GAMESNORMAL'} ++;
						if ($win == 1) {
							$dglobal{'PAXWINNORMAL'} ++;	
						} else {
							$dglobal{'PAXLOSNORMAL'} ++;
						}
					} else {
						$dglobal{'GAMESCOMMANDER'} ++;
						if ($win == 1) {
							$dglobal{'PAXWINCOMMANDER'} ++;	
						} else {
							$dglobal{'PAXLOSCOMMANDER'} ++;
						}
					}
				}
			}
		}
	}
}

my $stats_commander = 0;
if (defined $global{'GAMESCOMMANDER'}) {
	if (!defined $global{'PAXWINCOMMANDER'}) {
		$global{'PAXWINCOMMANDER'} = 0;
	}
	$stats_commander = ($global{'PAXWINCOMMANDER'} * 100) / $global{'GAMESCOMMANDER'};
	$stats_commander = sprintf("%.2f", $stats_commander);
}
my $stats_normal = 0;
if (defined $global{'GAMESNORMAL'}) {
	if (!defined $global{'PAXWINNORMAL'}) {
		$global{'PAXWINNORMAL'} = 0;
	}
	$stats_normal = ($global{'PAXWINNORMAL'} * 100) / $global{'GAMESNORMAL'};
	$stats_normal = sprintf("%.2f", $stats_normal);
}

my $d_sum;
my $d_min = 100000000;
my $d_max = 0;

foreach my $d (@duration) {
	my $dm = $d / 24.4;
	$d_sum += $dm;
	
	if ($dm <= $d_min) {
		$d_min = $dm;	
	}
	
	if ($dm > $d_max) {
		$d_max = $dm;
	}
}

my $d_average = $d_sum / scalar(@duration) / 60;
$d_average = sprintf("%.2f", $d_average);
$d_max = $d_max / 60;
$d_max = sprintf("%.2f", $d_max);
$d_min = $d_min / 60;
$d_min = sprintf("%.2f", $d_min);

my $dstats_commander = 0;
my $dstats_normal = 0;
my $dd_sum;
my $dd_min = 100000000;
my $dd_max = 0;
my $dd_average;

if (defined $cfg{'default.DAILY'}) {
	$dstats_commander = 0;
	if (defined $dglobal{'GAMESCOMMANDER'}) {
		if (!defined $dglobal{'PAXWINCOMMANDER'}) {
			$dglobal{'PAXWINCOMMANDER'} = 0;
		}
		$dstats_commander = ($dglobal{'PAXWINCOMMANDER'} * 100) / $dglobal{'GAMESCOMMANDER'};
		$dstats_commander = sprintf("%.2f", $dstats_commander);
	}
	$dstats_normal = 0;
	if (defined $dglobal{'GAMESNORMAL'}) {
		if (!defined $dglobal{'PAXWINNORMAL'}) {
			$dglobal{'PAXWINNORMAL'} = 0;
		}
		$dstats_normal = ($dglobal{'PAXWINNORMAL'} * 100) / $dglobal{'GAMESNORMAL'};
		$dstats_normal = sprintf("%.2f", $dstats_normal);
	}
	
	foreach my $d (@dduration) {
		my $dm = $d / 24.4;
		$dd_sum += $dm;
		
		if ($dm <= $dd_min) {
			$dd_min = $dm;	
		}
		
		if ($dm > $dd_max) {
			$dd_max = $dm;
		}
	}
	
	if (@dduration > 0) {
		$dd_average = $dd_sum / scalar(@dduration) / 60;
	} else {
		$dd_average = 0;
	}
	$dd_average = sprintf("%.2f", $dd_average);
	$dd_max = $dd_max / 60;
	$dd_max = sprintf("%.2f", $dd_max);
	$dd_min = $dd_min / 60;
	$dd_min = sprintf("%.2f", $dd_min);
}


foreach my $g (keys %sum) {
	foreach my $n (keys %{ $sum{$g} }) {

		#$vsglobal{$n}{'PLAYED'} ++;
		
		if (defined $sum{$g}{$n}{'RACE'}) {
			
			my $race = $sum{$g}{$n}{'RACE'};
			$vsglobal{$n}{$race}{'PLAYED'} ++;
			
			if ($sum{$g}{$n}{'RESULT'} == 1) {
				$vsglobal{$n}{$race}{'WON'} ++;					
			} elsif ($sum{$g}{$n}{'RESULT'} == 2) {
				$vsglobal{$n}{$race}{'LOST'} ++;
			}
			
			if (defined $cfg{'default.DAILY'}) {
				if ($sum{$g}{$n}{'GAMETIME'} >= $cfg{'default.DAILY'}) {
					my $race = $sum{$g}{$n}{'RACE'};
					$dvsglobal{$n}{$race}{'PLAYED'} ++;
					
					if ($sum{$g}{$n}{'RESULT'} == 1) {
						$dvsglobal{$n}{$race}{'WON'} ++;					
					} elsif ($sum{$g}{$n}{'RESULT'} == 2) {
						$dvsglobal{$n}{$race}{'LOST'} ++;
					}
				}
			}
			
			
		}
	}
}

my $mvp;
my %mvp;
my %dps;
foreach my $g (keys %sum) {
	my $maxdmg = 1;
	my $playerdmg = 0;
	my $playerrace;

	foreach my $n (keys %{ $sum{$g} }) {
		my $dmg = $sum{$g}{$n}{'KILLSUM'};
		if ($dmg > $maxdmg) {
			$maxdmg = $dmg;	
		}
		if ($n eq $player) {
			if (defined $sum{$g}{$n}{'KILLSUM'} && defined $sum{$g}{$n}{'DURATION'}) {
				my $duration = $sum{$g}{$n}{'DURATION'} / 24.4;
				
				my $doit = 1;
				if (defined $cfg{'default.SKIP'}) {
					if ($duration < $cfg{'default.SKIP'}) {
						$doit = 0;	
					}
				}

				if ($skip_normal) {
					if ($sum{$g}{$n}{'RACE'} eq "Zerg" || $sum{$g}{$n}{'RACE'} eq "Terran" || $sum{$g}{$n}{'RACE'} eq "Protoss") {
						$doit = 0;
						print "Skipping stats for $g due to skip_normal\n" if $DEBUG > 1;	
					}
					
				}				
				if ($doit) {
					
					$playerdmg = $sum{$g}{$n}{'KILLSUM'};
					$playerrace = $sum{$g}{$n}{'RACE'};
					
					my $dps  = $sum{$g}{$n}{'KILLSUM'} / $duration;
					push (@{ $dps{$sum{$g}{$n}{'RACE'}} }, $dps);
				}
			}
		}
	}	
	
	if ($playerdmg == $maxdmg) {
		$mvp{$playerrace} ++;
	}
	
}

my %l_dps;
my @x_dps;
my @y_dps;

print "DPS per race:\n";
foreach my $r (sort keys %dps) {
	print $r . "; ";
	my $sum;
	my $min = 10000000000;
	my $max = 0;
	foreach my $dps (@{ $dps{$r} }) {
		$sum += $dps;
		if ($dps > $max) {
			$max = $dps;	
		}
		
		if ($dps < $min) {
			$min = $dps;	
		}
	}
	my $dps_average = $sum / scalar(@{ $dps{$r} });
	$dps_average = sprintf("%.2f", $dps_average);
	if (!defined $mvp{$r}) {
		$mvp{$r} = 0;
	}
	my $mvp_per = $mvp{$r} * 100 / $vsglobal{$player}{$r}{'PLAYED'};
	$mvp_per = sprintf("%.2f", $mvp_per);
	$min = sprintf("%.2f", $min);
	$max = sprintf("%.2f", $max);
	#print $dps_average . "(min " . $min . ", max " . $max . " (mvp " . $mvp{$r} . "/" . $vsglobal{$player}{$r}{'PLAYED'} . " (" . $mvp_per . "%))\n";
	print $dps_average . "; " . $min . "; " . $max . "; " . $mvp{$r} . "; " . $vsglobal{$player}{$r}{'PLAYED'} . "; " . $mvp_per . "%\n";
	
	$l_dps{$r . " (" . $mvp_per . "%)"} = $dps_average;
	
}
print "\n";

# Generating the Graphs
#

my $hs = $start_date;
if ($start_date =~ /^(\d{8})/) {
	$hs = $1;
}
my $he = $end_date;
if ($end_date =~ /^(\d{8})/) {
	$he = $1;	
}

my $title_dps = "DPS with MVP % (most damage done) based on ValueMineralsKilledArmy ($hs to $he)";

my $graph2 = GD::Graph::bars->new(1600, 600);
$graph2->set(
    x_label             => 'Commanders',
    y_label             => 'DPS',
    title               => $title_dps,
    
    # shadows
    bar_spacing     => 8,
    shadow_depth    => 4,
    shadowclr       => 'dred',
        
    y_max_value         => 100,
    y_min_value         => 0,
    y_tick_number       => 1,
    y_label_skip        => 1,
    x_label_skip        => 1,
    x_labels_vertical => 1,
    
    bar_spacing     => 10,
    accent_treshold => 200,
    transparent     => 0,
    
    transparent         => 0,
    bgclr               => 'white',
    long_ticks          => 1,
) or die $graph2->error;




foreach my $r (sort {$l_dps{$a} <=> $l_dps{$b}} keys %l_dps) {
	push (@x_dps, $r);
	push (@y_dps, $l_dps{$r});
}

my @data_dps = (\@x_dps, \@y_dps);


$graph2->set_title_font('C:/Windows/Fonts/arial.ttf', 18);
$graph2->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
$graph2->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
$graph2->set_x_axis_font('C:/Windows/Fonts/arial.ttf', 14);
$graph2->set(show_values => 1 );
$graph2->set_values_font('C:/Windows/Fonts/arial.ttf', 12);

$graph2->set( dclrs => [ qw(blue blue blue blue) ] );
my $gd_dps = $graph2->plot(\@data_dps) or die $graph2->error;
 
open(IMG, ">:unix", $png_dps) or die $!;
binmode IMG;
print IMG $gd_dps->png;
close(IMG);


my $total;
my $total_won;
my $total_lost;

if (! defined $global{'GAMESNORMAL'}) {
	$global{'GAMESNORMAL'} = 0;
}
print "Total games: " . $global{'GAMES'} . "; Normal: " . $global{'GAMESNORMAL'} . " (" . $stats_normal . "%) Commander: " . $global{'GAMESCOMMANDER'} . " (" . $stats_commander . "%) ($hs to $he) \n";
print "Average duration: $d_average mins (min $d_min , max $d_max)\n";

my $title;
if ($skip_normal) {
	$title = "Winrate (cmdr: " . $global{'GAMESCOMMANDER'} . " (" . $stats_commander . "%)); gametime $d_average min ($hs to $he)";
} else {
	$title = "Winrate (Total: " . $global{'GAMES'} . "; std: " . $global{'GAMESNORMAL'} . " (" . $stats_normal . "%) cmdr: " . $global{'GAMESCOMMANDER'} . " (" . $stats_commander . "%)); gametime $d_average min ($hs to $he)";
}

my $graph = GD::Graph::bars->new(1600, 600);
$graph->set(
    x_label             => 'Commanders',
    y_label             => 'Winrate',
    title               => $title,
    
    # shadows
    bar_spacing     => 8,
    shadow_depth    => 4,
    shadowclr       => 'dred',
        
    y_max_value         => 100,
    y_min_value         => 0,
    y_tick_number       => 1,
    y_label_skip        => 1,
    x_label_skip        => 1,
    x_labels_vertical => 1,
    
    bar_spacing     => 10,
    accent_treshold => 200,
    transparent     => 0,
    
    transparent         => 0,
    bgclr               => 'white',
    long_ticks          => 1,
) or die $graph->error;

my @x;
my @y;

my %l;



foreach my $n (keys %vsglobal) {
	if ($n eq $player) {
		print "Stats for player $player:\n";
		foreach my $r (sort keys %{ $vsglobal{$n} }) {
			
			my $doit = 1;
			if ($skip_normal) {
				if ($r eq "Zerg" || $r eq "Terran" || $r eq "Protoss") {
					$doit = 0; 	
				}	
			}
			
			if ($doit) {
			
				print "$r";
				print "; " . $vsglobal{$n}{$r}{'PLAYED'} . "; ";
			
				my $winp_r = 0;
				if (defined $vsglobal{$n}{$r}{'PLAYED'}) {
					if (!defined $vsglobal{$n}{$r}{'WON'}) {
						$vsglobal{$n}{$r}{'WON'} = 0;
					}
					$winp_r = $vsglobal{$n}{$r}{'WON'} * 100 / $vsglobal{$n}{$r}{'PLAYED'};
					$winp_r = sprintf("%.2f", $winp_r);
				}
				print  $vsglobal{$n}{$r}{'WON'} . "; " if $DEBUG > 1 && defined $vsglobal{$n}{$r}{'WON'};
				print  $vsglobal{$n}{$r}{'LOST'} . "; " if $DEBUG > 1 && defined $vsglobal{$n}{$r}{'LOST'};
				print $winp_r . "%\n" if $DEBUG > 1;
				
				#	played 38 
				# win 21
				
				$l{$r . " (" . $vsglobal{$n}{$r}{'PLAYED'} . ")"} = $winp_r;
				
				$total += $vsglobal{$n}{$r}{'PLAYED'};			
				$total_won += $vsglobal{$n}{$r}{'WON'} if defined $vsglobal{$n}{$r}{'WON'};
				$total_lost += $vsglobal{$n}{$r}{'LOST'} if defined $vsglobal{$n}{$r}{'LOST'};
			}
		}
	}
}

foreach my $r (sort {$l{$a} <=> $l{$b}} keys %l) {
	push (@x, $r);
	push (@y, $l{$r});
}

my @data = (\@x, \@y);


$graph->set_title_font('C:/Windows/Fonts/arial.ttf', 18);
$graph->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
$graph->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
$graph->set_x_axis_font('C:/Windows/Fonts/arial.ttf', 14);
$graph->set(show_values => 1 );
$graph->set_values_font('C:/Windows/Fonts/arial.ttf', 12);

$graph->set( dclrs => [ qw(blue blue blue blue) ] );
my $gd = $graph->plot(\@data) or die $graph->error;
 
open(IMG, ">:unix", $png) or die $!;
binmode IMG;
print IMG $gd->png;
close(IMG);


if (defined $cfg{'default.DAILY'}) {
	my $total;
	my $total_won;
	my $total_lost;
	
	my $daily;
	if ($cfg{'default.DAILY'} =~ /^(\d{8})/) {
		$daily = $1;
	}
	
	if (! defined $dglobal{'GAMESNORMAL'}) {
		$dglobal{'GAMESNORMAL'} = 0;
	}
	print "Total games: " . $dglobal{'GAMES'} . "; Normal: " . $dglobal{'GAMESNORMAL'} . " (" . $dstats_normal . "%) Commander: " . $dglobal{'GAMESCOMMANDER'} . " (" . $dstats_commander . "%) ($daily) \n";
	print "Average duration: $dd_average mins (min $dd_min , max $dd_max)\n";
	
	my $title;
	if ($skip_normal) {
		$title = "Winrate today (cmdr: " . $dglobal{'GAMESCOMMANDER'} . " (" . $dstats_commander . "%)); gametime $dd_average min ($hs to $he)";
	} else {
		$title = "Winrate today (Total: " . $dglobal{'GAMES'} . "; std: " . $dglobal{'GAMESNORMAL'} . " (" . $dstats_normal . "%) cmdr: " . $dglobal{'GAMESCOMMANDER'} . " (" . $dstats_commander . "%)); gametime $dd_average min ($daily)";
	}
	
	my $graph = GD::Graph::bars->new(1600, 600);
	$graph->set(
	    x_label             => 'Commanders',
	    y_label             => 'Winrate',
	    title               => $title,
	    
	    # shadows
	    bar_spacing     => 8,
	    shadow_depth    => 4,
	    shadowclr       => 'dred',
	        
	    y_max_value         => 120,
	    y_min_value         => 0,
	    y_tick_number       => 1,
	    y_label_skip        => 1,
	    x_label_skip        => 1,
	    
	    
	    bar_spacing     => 10,
	    accent_treshold => 200,
	    transparent     => 0,
	    
	    transparent         => 0,
	    bgclr               => 'white',
	    long_ticks          => 1,
	) or die $graph->error;
	
	my @x;
	my @y;
	
	my %l;
	
	
	
	foreach my $n (keys %dvsglobal) {
		if ($n eq $player) {
			print "Stats for player $player:\n";
			foreach my $r (sort keys %{ $dvsglobal{$n} }) {
				
				my $doit = 1;
				if ($skip_normal) {
					if ($r eq "Zerg" || $r eq "Terran" || $r eq "Protoss") {
						$doit = 0; 	
					}	
				}
				
				if ($doit) {
				
					print "$r";
					print "; " . $dvsglobal{$n}{$r}{'PLAYED'} . "; ";
				
					my $winp_r = 0;
					if (defined $dvsglobal{$n}{$r}{'PLAYED'}) {
						if (!defined $dvsglobal{$n}{$r}{'WON'}) {
							$dvsglobal{$n}{$r}{'WON'} = 0;
						}
						$winp_r = $dvsglobal{$n}{$r}{'WON'} * 100 / $dvsglobal{$n}{$r}{'PLAYED'};
						$winp_r = sprintf("%.2f", $winp_r);
					}
					print  $dvsglobal{$n}{$r}{'WON'} . "; " if $DEBUG > 1 && defined $dvsglobal{$n}{$r}{'WON'};
					print  $dvsglobal{$n}{$r}{'LOST'} . "; " if $DEBUG > 1 && defined $dvsglobal{$n}{$r}{'LOST'};
					print $winp_r . "%\n" if $DEBUG > 1;
					
					#	played 38 
					# win 21
					
					$l{$r} = $winp_r;
					
					$total += $dvsglobal{$n}{$r}{'PLAYED'};			
					$total_won += $dvsglobal{$n}{$r}{'WON'} if defined $dvsglobal{$n}{$r}{'WON'};
					$total_lost += $dvsglobal{$n}{$r}{'LOST'} if defined $dvsglobal{$n}{$r}{'LOST'};
				}
			}
		}
	}
	
	foreach my $r (sort {$l{$a} <=> $l{$b}} keys %l) {
		push (@x, $r);
		push (@y, $l{$r});
	}
	
	my @data = (\@x, \@y);
	
	
	$graph->set_title_font('C:/Windows/Fonts/arial.ttf', 18);
	$graph->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
	$graph->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
	$graph->set_x_axis_font('C:/Windows/Fonts/arial.ttf', 14);
	$graph->set(show_values => 1 );
	$graph->set_values_font('C:/Windows/Fonts/arial.ttf', 12);
	
	$graph->set( dclrs => [ qw(blue blue blue blue) ] );
	my $gd = $graph->plot(\@data) or die $graph->error;
	 
	open(IMG, ">:unix", $daily_png) or die $!;
	binmode IMG;
	print IMG $gd->png;	
	close(IMG);
	

	
}


if (defined $cfg{'default.DAILY'}) {
	
	my $latest = $cfg{'default.DAILY'};
	my $latest_game;
	
	foreach my $g (keys %sum) {
		foreach my $n (keys %{ $sum{$g} }) {
			
			if ($sum{$g}{$n}{'GAMETIME'} > $latest) {
				$latest_game = $g;
				$latest = $sum{$g}{$n}{'GAMETIME'};
			}
		}
	}
	
	
	# failsafe :(
	my %str;
	foreach my $n (keys %{ $sum{$latest_game} }) {
		foreach my $ent (keys %{ $sum{$latest_game}{$n} }) {
			$str{$n}{$ent} = $sum{$latest_game}{$n}{$ent};	
		}
	}
	
	
	open(LATEST, ">", $latest_txt) or die "Could not write to $latest_txt: $!\n"; 

	foreach my $n (sort { $str{$a}->{'TEAM'} <=> $str{$b}->{'TEAM'} } keys %str) {
		print "team: $str{$n}{'TEAM'}; name: $n; race: $str{$n}{'RACE'}; win: $str{$n}{'RESULT'}; ValueKilled: $str{$n}{'KILLSUM'}\n";
		print LATEST "team: $str{$n}{'TEAM'}; name: $n; race: $str{$n}{'RACE'}; win: $str{$n}{'RESULT'}; ValueKilled: $str{$n}{'KILLSUM'}\n";
	}
	close(LATEST);
	
}

# Reading in csv file
#

sub ReadCSV {
	my $csv = shift;
	my $sumref = shift;
	
	
	if (-e $csv) {
        open(SUM, "<", $csv) or die "Could not read $csv: $!\n";
        while (<SUM>) {
        	chomp;
			if (/^\d/) {
				my @line = split(/;/, $_);
				my $game = $line[0];
				my $replay = $line[1];
				my $name = $line[2];
				my $race = $line[3];
				my $race2 = $line[4];
				my $team = $line[5]; 
				my $result = $line[6];
				my $killsum = $line[7];
				my $duration = $line[8];
				my $gametime = $line[9];
				my $playerid = $line[10];
				
				$game =~ s/\s+//g;
				$replay =~ s/^\s+//;
				$name =~ s/\s+//g;
				$race =~ s/\s+//g;
				$race2 =~ s/\s+//g;
				$team =~ s/\s+//g;
				$result =~ s/\s+//g;
				$killsum =~ s/\s+//g;
				$duration =~ s/\s+//g;
				$gametime =~ s/\s+//g;
				$playerid =~ s/\s+//g;
			
				$sumref->{$replay}{$name}{'RACE'} = $race2;
				$sumref->{$replay}{$name}{'TEAM'} = $team;
				$sumref->{$replay}{$name}{'RESULT'} = $result;
				$sumref->{$replay}{$name}{'KILLSUM'} = $killsum;
				$sumref->{$replay}{$name}{'DURATION'} = $duration;
				$sumref->{$replay}{$name}{'GAMETIME'} = $gametime;		        
				$sumref->{$replay}{$name}{'PLAYERID'} = $playerid;
			}
        }
	}
	
}

# Decoding the replays (skipping files already existing)
#

sub Info {
        my $rep = shift;
        if ($rep =~ /SC2Replay$/) {

                foreach my $get (@getpool) {
                        my $store_file = $store_path . "/" . basename($rep) . "_" . $get . ".txt";
                        my $temp_file = $store_file . "_temp";
						if (-e $store_file) {
							print "Skipping $rep - plain file already existing ($store_file)\n" if $DEBUG > 1;
						} else {
							my $exec = "$python $p_script \"$rep\" --$get > \"$temp_file\"";
							print $exec . "\n" if $DEBUG > 1;
							`$exec`;
							&File::Copy::move($temp_file, $store_file) or die $!;
						}
                }

        }
        my $ret = $store_path . "/" . basename($rep) . "_";
        return $ret;
}


