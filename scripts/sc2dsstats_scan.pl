use strict;
use warnings;
use strict;
use warnings;
use GD::Graph::bars;
use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";
use XML::Simple;
use GD::Graph::bars;
use GD::Graph::hbars;
use POSIX qw(strftime);
use File::Basename;
use Encode qw(decode encode);
use File::Copy;
use DateTime qw(from_epoch);

my $DEBUG = 2;


#my $main_path = "D:/github/sc2dsstats_debug";

my $main_path = dirname($0);
$main_path = dirname($main_path);

my $config_file = $main_path . "/sc2dsstats.exe.Config";
my $logfile = $main_path . "/log.txt";
my %cfg;

open(LOG, ">", $logfile) or die $!;
&Log("Reading in Config file ..", 1);
close(LOG);

open(LOG, ">>", $logfile) or die $!;


my $cfg = XMLin($config_file);

$DEBUG = $cfg->{'appSettings'}{'add'}{'DEBUG'}{'value'};

if (defined $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'}) {
	$cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'} = encode("UTF-8", $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'});
}
my $player = $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'};
my $skip_std = 0;
if (defined $cfg->{'skip'}{'add'}{'STD'}{'value'}) {
	$skip_std = 	$cfg->{'skip'}{'add'}{'STD'}{'value'};
}
#my $start_date = $cfg->{'appSettings'}{'add'}{'START_DATE'}{'value'};
#my $end_date = $cfg->{'appSettings'}{'add'}{'END_DATE'}{'value'};
my $start_date = "19700101000000";
my $ed_temp = time + 60 * 24 * 60;
my $end_date = strftime("%Y%m%d%H%M%S", localtime($ed_temp));

if (defined $cfg->{'appSettings'}{'add'}{'START_DATE'}{'value'}) {
	$start_date = $cfg->{'appSettings'}{'add'}{'START_DATE'}{'value'} if $cfg->{'appSettings'}{'add'}{'START_DATE'}{'value'};
}

if (defined $cfg->{'appSettings'}{'add'}{'END_DATE'}{'value'}) {
	$end_date = $cfg->{'appSettings'}{'add'}{'END_DATE'}{'value'} if $cfg->{'appSettings'}{'add'}{'END_DATE'}{'value'};
}

my $s2_cli = $main_path . "/scripts/s2_cli.exe";
if (defined $cfg->{'appSettings'}{'add'}{'PYTHON'}{'value'} && defined $cfg->{'appSettings'}{'add'}{'S2_CLI'}{'value'}) {
	$s2_cli = $cfg->{'appSettings'}{'add'}{'PYTHON'}{'value'} . " " . $cfg->{'appSettings'}{'add'}{'S2_CLI'}{'value'};
}

my $store_path = $main_path . "/analyzes";
my $stats_dir = $store_path;
my $csv = $main_path . "/stats.csv";

my @getpool = ('details', 'trackerevents');
if (defined $cfg->{'appSettings'}{'add'}{'SKIP_MSG'}{'value'} && $cfg->{'appSettings'}{'add'}{'SKIP_MSG'}{'value'}) {
	push(@getpool, 'messageevents');
}


my %sum;
my %skip;
my $games;

&Log("Reading in stats file ..", 1);

&ReadCSV($csv, \%sum);

$games = keys %sum;

&Log("$games found", 2);

&Log("Working on replays between $start_date and $end_date ..", 1);

# Decoding the replays using s2protocol and python
#

my $todo_replays = 0;
opendir(REP, $cfg->{'appSettings'}{'add'}{'REPLAY_PATH'}{'value'}) or die "Could not read dir $cfg->{'appSettings'}{'add'}{'REPLAY_PATH'}{'value'}: $!\n";
while (my $p = readdir(REP)) {
	next if $p =~ /^\./;
	if ($p =~ /^Direct Strike/ || $p =~ /^Desert Strike/) {

			my $id = $p;
			if ($p =~ /(.*)\.SC2Replay/) {
				$id = $1;	
			}
			
			next if defined $sum{$id};

			my $gametime;
			# startdate
			if (defined $cfg->{'skip'}{'add'}{'START_DATE'}{'value'} && $cfg->{'skip'}{'add'}{'START_DATE'}{'value'}) {
				$gametime=POSIX::strftime( "%Y%m%d%H%M%S", localtime(( stat $p )[9] ) );
				if ($gametime < $start_date) { 
					&Log("(Info) Skipping $p due to START_DATE", 2);
					next;
				}
        	} 
        	
        	# enddate
			if (defined $cfg->{'skip'}{'add'}{'END_DATE'}{'value'} && $cfg->{'skip'}{'add'}{'END_DATE'}{'value'}) {
				$gametime=POSIX::strftime( "%Y%m%d%H%M%S", localtime(( stat $p )[9] ) );
				if ($gametime > $end_date) { 
					&Log("(Info) Skipping $p due to END_DATE", 2);
					next;
				}
        	} 
        	
        	# beta
			if (defined $cfg->{'skip'}{'add'}{'BETA'}{'value'} && $cfg->{'skip'}{'add'}{'BETA'}{'value'}) {
				if ($p =~ /Beta/) { 
					&Log("(Info) Skipping $p due to BETA", 2);
					next;
				}
        	}
        	
        	# hots
			if (defined $cfg->{'skip'}{'add'}{'HOTS'}{'value'} && $cfg->{'skip'}{'add'}{'HOTS'}{'value'}) {
				if ($p =~ /HotS/) { 
					&Log("(Info) Skipping $p due to HOTS", 2);
					next;
				}
        	} 
		$todo_replays ++;
	}
}	
closedir(REP);

&Log("We found $todo_replays new replays", 1);

open(CSV, ">>", $csv) or die "Could not write to $csv: $!\n";

my $done_replays = 1;
opendir(REP, $cfg->{'appSettings'}{'add'}{'REPLAY_PATH'}{'value'}) or die "Could not read dir $cfg->{'appSettings'}{'add'}{'REPLAY_PATH'}{'value'}: $!\n";
while (my $p = readdir(REP)) {
	next if $p =~ /^\./;
	if ($p =~ /^Direct Strike/ || $p =~ /^Desert Strike/) {
		
		my $id = $p;
		if ($p =~ /(.*)\.SC2Replay/) {
			$id = $1;	
		}
		
		next if defined $sum{$id};
		
		my $replay = $cfg->{'appSettings'}{'add'}{'REPLAY_PATH'}{'value'} . "/" . $p;
		my $info_path = &Info($replay);
		
		# Extracting data from the decoded Replays
		#		

		if ($info_path) {
			
			&Log("Reading in data from $id ..", 1);
			
			my %detail;
			my %skipmsg;
			
			
			
			&GetData($id, $info_path, \%detail, \@getpool, $cfg, \%skipmsg);	
			
			foreach my $id (sort keys %detail) {
				
				my $skip = 0;
        		foreach  my $d (sort keys %{ $detail{$id} }) {
        			
	        		if (defined $cfg->{'appSettings'}{'add'}{'SKIP_MSG'}{'value'} && $cfg->{'appSettings'}{'add'}{'SKIP_MSG'}{'value'}) {
						if (defined $skipmsg{$id}{'PLAYER'}) {
							if (defined $skipmsg{$id}{$skipmsg{$id}{'PLAYER'}} && $skipmsg{$id}{$skipmsg{$id}{'PLAYER'}}) {
								&Log("Skipping $id due to skipmsg", 2);
								$skip = 1;
								next;
							}
						}	
					}
					  
					
					if (!defined $detail{$id}{$d}{'NAME'}) {
						&Log("(CSV) Skipping $id due to no NAME", 2);
						$skip = 1;
						next;
					}
					if (!defined $detail{$id}{$d}{'RACE'}) {
						&Log("(CSV) Skipping $id due to no RACE", 2);
						$skip = 1;
						next;
					}
					if (!defined $detail{$id}{$d}{'RACE2'}) {
						# STD games
						$detail{$id}{$d}{'RACE2'} = $detail{$id}{$d}{'RACE'}; 
					}
					if (!defined $detail{$id}{$d}{'TEAM'}) {
						&Log("(CSV) Skipping $id due to no TEAM", 2);
						$skip = 1;
						next;
					}
					if (!defined $detail{$id}{$d}{'RESULT'}) {
						&Log("(CSV) Skipping $id due to no RESULT", 2);
						$skip = 1;
						next;
					}
					if (!defined $detail{$id}{$d}{'GAMETIME'}) {
						&Log("(CSV) Skipping $id due to no GAMETIME", 2);
						$skip = 1;
						next;
					}
					if (!defined $detail{$id}{$d}{'DURATION'}) {
						&Log("(CSV) Fixing $id due to no DURATION", 2);
						$detail{$id}{$d}{'DURATION'} = 0;
						#$skip = 1;
						#next;
					}
					
					if (!defined $detail{$id}{$d}{'ARMY'}) {
						&Log("(CSV) Fixing $id due to no ARMY", 2);
						$detail{$id}{$d}{'ARMY'} = 0;
						#$skip = 1;
						#next;
					}
					if (!defined $detail{$id}{$d}{'KILLSUM'}) {
						&Log("(CSV) SkFixingipping $id due to no KILLSUM", 2);
						$detail{$id}{$d}{'KILLSUM'} = 0;
						#$skip = 1;
						#next;
					}
					if (!defined $detail{$id}{$d}{'INCOME'}) {
						&Log("(CSV) Fixing $id due to no INCOME", 2);
						$detail{$id}{$d}{'INCOME'} = 0;
						#$skip = 1;
						#next;
					}


        					
        		}

        		if ($skip == 0) {
        			$games++;
	        		foreach  my $d (sort keys %{ $detail{$id} }) {       		
						 
    	    			my $ent = $games . "; " . $id . "; " . $detail{$id}{$d}{'NAME'} . "; " . $detail{$id}{$d}{'RACE'} . "; " . $detail{$id}{$d}{'RACE2'}. "; " .
			        			$detail{$id}{$d}{'TEAM'} . "; " . $detail{$id}{$d}{'RESULT'} . "; " . $detail{$id}{$d}{'KILLSUM'} . "; " .
			                	$detail{$id}{$d}{'DURATION'} . "; " . $detail{$id}{$d}{'GAMETIME'} . "; " . $d . "; " . $detail{$id}{$d}{'INCOME'} . "; " . $detail{$id}{$d}{'ARMY'} . ";\n";
			                		
        	        	print CSV $ent;
	        		}
        		}
        		
        		if (defined $cfg->{'appSettings'}{'add'}{'KEEP'}{'value'} && $cfg->{'appSettings'}{'add'}{'KEEP'}{'value'} == 0) {
					foreach my $get (@getpool) {
						my $done_file = $info_path . $get . ".txt";
						if (-e $done_file) {
							unlink($done_file) or die "Could not unlink $done_file: $!\n";
							if ($skip == 1) {
								open(DONE, ">", $done_file) or die "Could not write to $done_file: $!\n";
								print DONE "Und es war Sommer (delete this file if you want to rescan the replay)\n";
								close(DONE);
							}
						}	
					}
				}
        			
			}
			
		}
		

		
		my $done = $done_replays * 100 / $todo_replays;
		$done = sprintf("%.2f", $done);
		&Log($done_replays . " / " . $todo_replays . " (" . $done . "% complete)", 1);
		$done_replays ++;
	}
}
close(CSV);
closedir(REP);


close(LOG);


sub GetData {

	my $id = shift;
	my $path = shift;
	my $detail = shift;
	my $getpool = shift;
	my $cfg = shift;     
	my $skipmsg = shift;   	
	        	
	foreach my $ext (@$getpool) {
	        	
		my $stat_file = $path . $ext . ".txt";        	
        	my $duration;
			my $gameloop;
			my $player_count = 0;
			my $gametime;
			my $fix = 0;
			my %fix;
			my $playerId = 1;
				
			my %m_scoreValueMineralsCollectionRate;
			my %m_scoreValueMineralsLostArmy;
			my %m_scoreValueMineralsUsedActiveForces;
				
			&Log("(GetData) Working on $stat_file ..", 2);	
		
	        if ($stat_file =~ /SC2Replay_details\.txt$/) {
	                
	                
	                $player_count = 0;
	
	                my $offset = 0;
	                my $playerid = 0;
					my $name;
					my $race;
					my $result;
					my $teamid;
	
	                open(ST, "<", $stat_file) or die "Could not read $stat_file: $!\n";
                        while (<ST>) {
	                        if (/m_name/) {
		                        if (/'([^']+)',$/) {
		                                $name = $1;
		                                
		                                if ($name =~ /<sp\/>(.*)$/) {
		                                	$name = $1;	
		                                }
		                                
		                                if ($name =~ /\\/) {
		                                	$name =~ s/\\x(..)/chr hex $1/ge;
		                                	#$name = encode("UTF-8", $name);
		                                }
		                                
										
										if ($name eq $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'}) {
											$skipmsg->{$id}{'PLAYER'} = $player_count;
										}
		                        }
               				 }
	
       		                 elsif (/m_race/) {
		                        if (/([\\\w]*)',$/) {
		                                $race = $1;
		                                if ($race =~ /\\/) {
		                                	$race =~ s/\\x(..)/chr hex $1/ge;
		                                }
		                                
		                                
		                        }
             			    }

                      	  elsif (/m_result/) {
		                        if (/(\d+),$/) {
		                                $result = $1;
		                                
		                        }
	                  	    }

	                        elsif (/m_teamId/) {
		                        if (/\s(\d*),$/) {
		                                $teamid = $1;
		                                
		

		                        }
	                        }
	                        
	                        elsif (/m_workingSetSlotId/) {
	                        	if (/(\d+)\}\]?,$/) {
	                        		$playerid = $1 + 1;
	                        		$player_count = $playerid;
	                        		$detail->{$id}{$playerid}{'NAME'} = $name;
	                        		$detail->{$id}{$playerid}{'RACE'} = $race;
	                        		$detail->{$id}{$playerid}{'RESULT'} = $result;
									$detail->{$id}{$playerid}{'TEAM'} = $teamid;
	                        	}	
	                        }
	                        
	                        elsif (/m_timeLocalOffset/) {
	                        	if (/\:\s([\-\d]+)L,$/) {
	                        		$offset = $1;
	                        	}	
	                        }
	                        
	                        elsif (/m_timeUTC/) {
	                        	if (/(\d+)L,$/) {
	                        		my $georgian = $1;
	                        		
	                        		$georgian = int($georgian / 10000000);	
	                        		$offset = int($offset / 10000000);
	                        		my $geo = $georgian + $offset;
	                        		my $dt_geo = DateTime->from_epoch( epoch => $geo);
	                        		my $dgeo = $dt_geo->year - 369;
	                        		$dt_geo->set( year => $dgeo );
	                        		$gametime = $dt_geo->ymd('') . $dt_geo->hms(''); 
	                        		for (my $i = 1; $i <= $player_count; $i++) {
	                        			if (defined $detail->{$id}{$i}) { 
	                        				$detail->{$id}{$i}{'GAMETIME'} = $gametime;
	                        			}
	                        		}
	                        	}	
	                       }
	        			}
		        			
		                close(ST);
		        } elsif ($stat_file =~ /_trackerevents\.txt$/) {
		                
		                my $playerid;
		                my $controlPlayerId;
		                my $event;
		
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
				                                $detail->{$id}{$playerid}{'RACE2'} = $race2;
				                                if ($race2 eq "Stukov" || $race2 eq "Horner" || $race2 eq "Zagara" || $race2 eq "Kerrigan" || $race2 eq "Alarak" || $race2 eq "Nova") {
				                                	if ($detail->{$id}{$playerid}{'GAMETIME'} <= 20190121000000) {
				                                		$fix = 1;
				                                	}	
				                                }
				                         }
			                        }
			 	        	 } elsif (/_gameloop/) {
				        	 	if (/(\d+),$/) {
									$duration = $1;	
									$gameloop = $1;
				        	 	}
				        	 	
				        	 } elsif (/m_playerId/) {
				        	 	if (/(\d),$/) {
				        	 		$playerid = $1;	
				        	 	}	
				        	 } elsif (/m_scoreValueMineralsKilledArmy/) {
				                if (/(\d+),$/) {
				                        my $killarmy = $1;
				                        if (defined $detail->{$id}{$playerid}) {
					                        $detail->{$id}{$playerid}{'KILLSUM'} = $killarmy;
					                        $detail->{$id}{$playerid}{'DURATION'} = $duration;
				                        }
				                        
				                }
			        		} elsif (/m_scoreValueMineralsCollectionRate'\: (\d+),$/) {
			        			if (defined $detail->{$id}{$playerid}) {
									$m_scoreValueMineralsCollectionRate{$playerid}{$gameloop} = $1;
			        			}
							#} elsif (/m_scoreValueMineralsLostArmy'\: (\d+),$/) {
							#	
							} elsif (/m_scoreValueMineralsUsedActiveForces'\: (\d+),$/) {
								if (defined $detail->{$id}{$playerid}) {
									$m_scoreValueMineralsUsedActiveForces{$playerid}{$gameloop} = $1;
								}
							}	
			        		
			        		if ($fix) {
			        				if (/'_event'\: 'NNet.Replay.Tracker.SUnitBornEvent',$/) {
										$event = 1;
									}
	
									if (/'m_controlPlayerId'\: (\d),$/) {
										$controlPlayerId = $1;
									}
									
									if (/m_unitTypeName'\: 'StukovInfestedBunker',$/) {
										if ($event) {
											
											if (!defined $fix{$controlPlayerId}) {
												$fix{$controlPlayerId} = 375;
											} else {
												$fix{$controlPlayerId} += 375;
											}
										} else {
											#print "Something else\n";	
										}	
										$event = 0;
									} elsif (/m_unitTypeName'\: 'HornerAssaultGalleon',$/) {
										if ($event) {
											
											if (!defined $fix{$controlPlayerId}) {
												$fix{$controlPlayerId} = 475;
											} else {
												$fix{$controlPlayerId} += 475;
											}
										} else {
											#print "Something else\n";	
										}	
										$event = 0;
									}	
									

			        		}
			        		
			        		
			        		
		                }
		                close(TRACKEREVENTS);
		                
		                foreach my $p (keys %m_scoreValueMineralsCollectionRate) {
						
							foreach my $gl (sort { $a <=> $b } keys %{ $m_scoreValueMineralsCollectionRate{$p} }) {
								my $income = $m_scoreValueMineralsCollectionRate{$p}{$gl} / 9.15;
								$detail->{$id}{$p}{'INCOME'} += $income;
							}
							$detail->{$id}{$p}{'INCOME'} = sprintf("%.2f", $detail->{$id}{$p}{'INCOME'}); 
						}
						
						foreach my $p (keys %m_scoreValueMineralsUsedActiveForces) {
							next if $p > 6;
							foreach my $gl (sort { $a <=> $b } keys %{ $m_scoreValueMineralsUsedActiveForces{$p} }) {
								
								my $spawn = ($gl - 480) % 1440;
								if ($spawn == 0) {
									if ($p == 1 || $p == 4) {
										if (defined $detail->{$id}{$p}{'ARMY'}) {
											$detail->{$id}{$p}{'ARMY'} += $m_scoreValueMineralsUsedActiveForces{$p}{$gl};
										} else {
												$detail->{$id}{$p}{'ARMY'} = $m_scoreValueMineralsUsedActiveForces{$p}{$gl};
										}
									}
								} elsif ($spawn == 480) {
									if ($p == 2 || $p == 5) {
										if (defined $detail->{$id}{$p}{'ARMY'}) {
											$detail->{$id}{$p}{'ARMY'} += $m_scoreValueMineralsUsedActiveForces{$p}{$gl};
										} else {
												$detail->{$id}{$p}{'ARMY'} = $m_scoreValueMineralsUsedActiveForces{$p}{$gl};
										}
									}
								} elsif ($spawn == 960) {
									if ($p == 3 || $p == 6) {
										if (defined $detail->{$id}{$p}{'ARMY'}) {
											$detail->{$id}{$p}{'ARMY'} += $m_scoreValueMineralsUsedActiveForces{$p}{$gl};
										} else {
												$detail->{$id}{$p}{'ARMY'} = $m_scoreValueMineralsUsedActiveForces{$p}{$gl};
										}
									}
								}
							}
						}
						
						if ($fix) {
							# Dirty quick
							
							for (my $i = 1; $i <= $player_count; $i++) {
								if (defined $detail->{$id}{$i}{'RACE2'}) {
									if ($detail->{$id}{$i}{'RACE2'} eq "Nova") {
										$fix{$i} = 250;	
									} elsif ($detail->{$id}{$i}{'RACE2'} eq "Zagara") {
										$fix{$i} = 275;
									} elsif ($detail->{$id}{$i}{'RACE2'} eq "Alarak") {
										$fix{$i} = 300;
									} elsif ($detail->{$id}{$i}{'RACE2'} eq "Kerrigan") {
										$fix{$i} = 400;
									}
								}
							}
							
							foreach my $p (keys %fix) {
								$detail->{$id}{$p}{'ARMY'} += $fix{$p};
							}
						}
		                
		                
		                
		        } elsif ($stat_file =~ /_messageevents\.txt$/) {
		        	if (defined $cfg->{'appSettings'}{'add'}{'SKIP_MSG'}{'value'} && $cfg->{'appSettings'}{'add'}{'SKIP_MSG'}{'value'}) {
			        	
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
				        					$skipmsg->{$id}{$playerid + 1} = 1;
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

	


sub Info {
        my $rep = shift;
        if ($rep =~ /SC2Replay$/) {

			my $gametime;
			
			# startdate
			if (defined $cfg->{'skip'}{'add'}{'START_DATE'}{'value'} && $cfg->{'skip'}{'add'}{'START_DATE'}{'value'}) {
				$gametime=POSIX::strftime( "%Y%m%d%H%M%S", localtime(( stat $rep )[9] ) );
				if ($gametime < $start_date) { 
					&Log("(Info) Skipping $rep due to START_DATE", 2);
					return;
				}
        	} 
        	
        	# enddate
			if (defined $cfg->{'skip'}{'add'}{'END_DATE'}{'value'} && $cfg->{'skip'}{'add'}{'END_DATE'}{'value'}) {
				$gametime=POSIX::strftime( "%Y%m%d%H%M%S", localtime(( stat $rep )[9] ) );
				if ($gametime > $end_date) { 
					&Log("(Info) Skipping $rep due to END_DATE", 2);
					return;
				}
        	} 
        	
        	# beta
			if (defined $cfg->{'skip'}{'add'}{'BETA'}{'value'} && $cfg->{'skip'}{'add'}{'BETA'}{'value'}) {
				if ($rep =~ /Beta/) { 
					&Log("(Info) Skipping $rep due to BETA", 2);
					return;
				}
        	}
        	
        	# hots
			if (defined $cfg->{'skip'}{'add'}{'HOTS'}{'value'} && $cfg->{'skip'}{'add'}{'HOTS'}{'value'}) {
				if ($rep =~ /HotS/) { 
					&Log("(Info) Skipping $rep due to HOTS", 2);
					return;
				}
        	} 
        	
        	&Log("Decoding data from  " . basename($rep) . " ...", 1); 
			   
			foreach my $get (@getpool) {

	        	my $store_file = $store_path . "/" . basename($rep) . "_" . $get . ".txt";
	           	my $temp_file = $store_file . "_temp";

				if (-e $store_file) {
					&Log("(Info) Skipping $rep - plain file already existing ($store_file)", 1);
				} else {
					#my $exec = "$python $p_script \"$rep\" --$get > \"$temp_file\"";
					my $exec = "$s2_cli \"$rep\" --$get > \"$temp_file\"";
					&Log("(Info) " . $exec, 2);
					`$exec`;
					if (-s $temp_file) {
						&File::Copy::move($temp_file, $store_file) or die $!;
					} else {
						&Log("(Info) Failed extracting data from $rep - plase check if there is an update availabe.", 0);
						&File::Copy::move($temp_file, $store_file) or die $!;
						return;
					}
				}
	        }
        }
        my $ret = $store_path . "/" . basename($rep) . "_";
        return $ret;
}

sub Log {
	my $msg = shift;	
	my $debug = shift;
	print LOG $msg . "\n" if $DEBUG >= $debug;
	print $msg . "\n" if $DEBUG >= $debug;
}

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
				my $income = $line[11];
				my $army = $line[12];
				
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
				$income =~ s/\s+//g;
				$army =~ s/\s+//g;
			
				$sumref->{$replay}{$name}{'RACE'} = $race2;
				$sumref->{$replay}{$name}{'TEAM'} = $team;
				$sumref->{$replay}{$name}{'RESULT'} = $result;
				$sumref->{$replay}{$name}{'KILLSUM'} = $killsum;
				$sumref->{$replay}{$name}{'DURATION'} = $duration;
				$sumref->{$replay}{$name}{'GAMETIME'} = $gametime;		        
				$sumref->{$replay}{$name}{'PLAYERID'} = $playerid;
				$sumref->{$replay}{$name}{'INCOME'} = $income;
				$sumref->{$replay}{$name}{'ARMY'} = $army;
			}
        }
	}
	
}
