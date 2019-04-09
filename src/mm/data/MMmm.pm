#!/usr/bin/perl

use threads;
use strict;
use Inline Python => <<'END_PYTHON';
from trueskill import Rating, TrueSkill, rate, quality, rate_1vs1, quality_1vs1, setup, expose
END_PYTHON
setup(25, 25/3, 25/6, 25/300, 0);

{
    package MMmm;
    use Moose;
    use threads::shared;

    use utf8;
    use open IN => ":utf8";
    use open OUT => ":utf8";

    use Games::Ratings::LogisticElo;
    use Data::Dumper;

    use lib ".";
    use MMplayer;
    use MMid;
    use MMqueue;

    my $log;
    my $msg_bak = "";
    my $msg_count = 0;
    my $DEBUG = 2;
    my $lock_db : shared;

    around 'new' => sub {
        my $orig = shift;
        my $class = shift;
        my $self = $class->$orig(@_);
        my $shared_self : shared = shared_clone($self);

        # here the blessed() already be the version in threads::shared
        #print Dumper($shared_self),"\n";

        open($log, ">>", "logmm.txt") or die $!;

        return $shared_self;
    };

    has 'MOD' => (is => 'rw');
    has 'VECTOR' => (is => 'rw');
    has 'CYCLE' => (is => 'rw', isa => 'Num', default => 0);
    has 'MMPLAYERS' => (
        traits    => ['Hash'],
        is        => 'rw',
        default   => sub { {} },
    );
    has 'MMIDS' => (
        traits    => ['Hash'],
        is        => 'rw',
        default   => sub { {} },
    );
    has 'PLAYERS' => (
        traits    => ['Hash'],
        is        => 'rw',
        default   => sub { {} },
    );
    has 'QUEUE' => (
        traits    => ['Hash'],
        is        => 'rw',
        default   => sub { {} },
    );
    has 'TIMESTAMP' => (
        traits    => ['Hash'],
        is        => 'rw',
        default   => sub { {} },
    );



    #Report | Letmeplay | Accept game | Decline game

    sub Result {
        my $self = shift;
        my $name = shift;
        my $mmid = shift;
        my $report = shift;

        my $response = 0;
        my $ladder = 0;
        my $quality = 0;

        if ($mmid < 1000) {

            my $id = new MMid();
            $id->MMID($mmid);
            $id->LADDER(1);
            $self->MMIDS->{$mmid} = $id;
            $ladder = 1;
        }
        #print "MMID: $mmid\n";
        if (exists $self->MMIDS->{$mmid}) {
            my $id = $self->MMIDS->{$mmid};

            if ($id->REPORT_SAVED) {
                #&Log("Game Reported 1 ($name): MMID: $mmid; $report") if $DEBUG > 1;
                #if ($id->RESPONSE) {
                #    if (exists $id->PLAYERS->{$name}) {
                #        return $id->RESPONSE . ";pos7: " . sprintf("%.2f", $id->PLAYERS->{$name}->ELO) . ";";
                #    }
                #    else {
                #        return $id->RESPONSE;
                #    }
                #}
            }

            if ($id->REPORTED) {

                # How many reports from one player for one mmid are valid?
                if (exists $id->PLAYERS->{$name}) {
                    my $pl = $id->PLAYERS->{$name};
                    if (exists $pl->MMIDS->{$mmid}) {
                        $pl->MMIDS->{$mmid} ++;
                    }
                }
            }

            if ($report =~ /^blame: (.*)/) {
                $id->REPORTED($id->REPORTED+1);
                $id->BLAMED($id->BLAMED+1);
                $id->REPORT->{$name} = $report;
            }

            elsif ($report =~ /^result: (.*)/) {
                
                my $result = $1;
                my @player;
                my @elo_player;
                my $valid = 0;
                # (PewPewPrince, Protoss, 28070), (PAX, Terran, 26520), (Nedved, Terran, 31295) vs (DerLodi, Terran, 26925), (SeyLerT, Zerg, 23710), (VrangelSERB, Protoss, 31000)
                # (player1, Nova, 236260), (player2, Abathur, 296070),  vs (player4, Kerrigan, 159330), (player5, Fenix, 294065), 

                my @res = split(/ vs /, $result);
                if ($#res == 1) {
                    my $t1 = 0;
                    my $k = 0;
                    foreach my $i (0..$#res) {
                        my $t = $res[$i];
                        $t =~ s/^\s+//g;
                        
                         my $plrep;
                        while ($t =~ /^\(([^\(]+)(.*)/) {
                            $k++;
                            my @ent = split(/,/, $1);
                            if ($#ent <= 3) {
                                for my $j (0..$#ent) {
                                    next if $j > 2;
                                    my $ent = $ent[$j];
                                    $ent =~ s/\s+//g;
                                    $ent =~ s/\)//g;
                                    $ent[$j] = $ent;
                                }
                               
                                if (exists $id->PLAYERS->{$ent[0]}) {
                                    $valid++;
                                    $plrep = $id->PLAYERS->{$ent[0]};

                                } else {
                                    if (!$ladder) {
                                        $plrep = new MMplayer();
                                        $plrep->NAME('Dummy');
                                        $id->PLAYERS->{$ent[0]} = $plrep;
                                        &Log("$mmid: Just a dummy :( " . $ent[0]) if $DEBUG;
                                    } else {

                                        if (exists $self->MMPLAYERS->{$ent[0]}) {
                                            $id->PLAYERS->{$ent[0]} = $self->MMPLAYERS->{$ent[0]};
                                            $self->MMPLAYERS->{$ent[0]}->LADDER(1);
                                            $plrep = $self->MMPLAYERS->{$ent[0]};
                                        } else {
                                            $plrep = new MMplayer();
                                            $plrep->NAME($ent[0]);
                                            $plrep->INDB(1);
                                            $plrep->LADDER(1);
                                            $id->PLAYERS->{$ent[0]} = $plrep;
                                            $self->MMPLAYERS->{$ent[0]} = $plrep;
                                        }
                                    }
                                }
                                $plrep->RACE($ent[1]);
                                $plrep->KILLSUM($ent[2]);
                                my $team = $i + 1;
                                if ($team == 1) {
                                    $plrep->POS($k);
                                    $plrep->TEAM(1);
                                }  else {
                                    $plrep->POS($k);  
                                    $plrep->TEAM(2);                                 
                                }


                            } else {
                                &Log("$mmid: this should not happen ..") if $DEBUG;
                                last;
                            }
                            push(@player, $plrep) if $plrep;
                            push(@elo_player, $plrep->NAME) if $plrep;
                            $t = $2;
                            $t =~ s/^\s+//g;                            
                        }
                    }
                }

                #if ($valid >= $id->NEED) {
                if ($valid >= 2 || $ladder) {
                    &Log("Report: Valid1: $valid") if $DEBUG > 1;
                    # TODO: Check blame / multiple reports / difference / leaver
                    {
                        lock (%{ $self->MMIDS });
                        #$self->SetElo($mmid, \@elo_player) unless $id->REPORTED;
                        #$self->SetMMR($mmid, \@elo_player) unless $id->REPORTED;

                        my %elo_temp;
                        my %sigma_temp;

                        # do we have a report pending?
                        foreach my $plname (keys %{$id->PLAYERS }) {
                            my $pl = $id->PLAYERS->{$plname};

                            # are there previous pending reports?
                            foreach my $pmmid (keys %{ $pl->MMIDS }) {
                                next if $pmmid == $mmid;
                                if (exists $self->MMIDS->{$pmmid}) {
                                    my $pid = $self->MMIDS->{$pmmid};
                                    foreach my $pplname (keys %{ $pid->PLAYERS }) {
                                        my $ppl = $pid->PLAYERS->{$pplname};

                                        # one report has to be enough (if any and only for this player)
                                        if ($plname eq $pplname) {
                                            if ($ppl->ELO_TEMP || $ppl->SIGMA_TEMP) {
                                                $ppl->ELO($ppl->ELO_TEMP);
                                                $ppl->SIGMA($ppl->SIGMA_TEMP);
                                                $ppl->ELO_TEMP(0);
                                                $ppl->SIGMA_TEMP(0);

                                                delete $ppl->MMIDS->{$pmmid};
                                                &Log("Report:  Previous: $plname: " . $ppl->ELO) if $DEBUG > 1;
                                            }
                                        }
                                    }
                                }
                            }

                            # not yet reported from (almost) all players

                            $elo_temp{$plname} = $pl->ELO_TEMP;
                            $sigma_temp{$plname} = $pl->SIGMA_TEMP;
                        }

                        # set temp rating (|| ladder)
                        #$self->SetMMR($mmid) unless $id->REPORTED;
                        $self->SetMMR($mmid);

                        # do we have another valid report?
                        my $valid2 = 0;
                        foreach my $plname (keys %{$id->PLAYERS }) {
                            my $pl = $id->PLAYERS->{$plname};
                            next if $pl->ELO_TEMP == 0 && $pl->SIGMA_TEMP == 0;
                            if ($elo_temp{$plname} == $pl->ELO_TEMP && $sigma_temp{$plname} == $pl->SIGMA_TEMP) {
                                $valid2 ++;
                            }
                        }
                        &Log("Report: Valid2: $valid2") if $DEBUG > 1;
                        $quality = $valid + $valid2;
                        

                        # two (almost) identical reports should be enough
                        if ($valid2 >= 2) {
                            if (($valid + $valid2) > $id->REPORT_QUALITY) {
                                if (!$id->REPORT_SAVED) {
                                    foreach my $plname (keys %{$id->PLAYERS }) {
                                        my $pl = $id->PLAYERS->{$plname};
                                        $pl->ELO($pl->ELO_TEMP);
                                        $pl->SIGMA($pl->SIGMA_TEMP);
                                        $pl->INDB(2); # db update needed
                                    }              
                                    &Log("LOGMM: $name: Finalizing MU change for $mmid") if $DEBUG;
                                    $id->REPORT_SAVED(1);
                                }
                            } else {
                                # report not good enough
                                foreach my $plname (keys %{$id->PLAYERS }) {
                                    my $pl = $id->PLAYERS->{$plname};
                                    $pl->ELO_TEMP($elo_temp{$plname});
                                    $pl->SIGMA_TEMP($sigma_temp{$plname});
                                }
                            }
                        }
                        $id->REPORT_QUALITY($quality) if $quality > $id->REPORT_QUALITY;
                        Log("LOGMM: $name: " . $mmid . " quality => " . $id->REPORT_QUALITY) if $DEBUG;
                        $id->REPORTED($id->REPORTED+1);
                    }

                } else {
                    return $mmid;
                }

            }
            
            my $myelo = "";
            if ($id->REPORTED) {
                $response = "pos0: $mmid;";
                foreach my $plname (keys %{ $id->PLAYERS }) {
                    my $pl = $id->PLAYERS->{$plname};
                    my $pl_elo;
                    if (!$ladder) {
                        if ($id->REPORT_SAVED) {
                            $pl_elo = sprintf("%.2f", $pl->ELO);
                        } else {
                            $pl_elo = sprintf("%.2f", $pl->ELO_TEMP);
                        }
                    } else {
                        $pl_elo = sprintf("%.2f", $pl->ELO_LADDER);
                    }
                    my $pl_elo_change = sprintf("%.2f", $pl->ELO_CHANGE);
                    $response .= "pos" . $pl->POS . ": " . $pl->NAME . "|" . $pl_elo . "|" . $pl_elo_change . "|" . $pl->RACE . "|" . $pl->KILLSUM . ";";
                }

               $id->RESPONSE($response) if $quality >= $id->REPORT_QUALITY;
                if ($response) {
                    if (exists $id->PLAYERS->{$name}) {
                        if ($id->REPORT_SAVED) {
                            $myelo = "pos7: " . sprintf("%.2f", $id->PLAYERS->{$name}->ELO) . ";";
                            $self->SetCache() unless $ladder;
                        } else {
                            $myelo = "pos7: " . sprintf("%.2f", $id->PLAYERS->{$name}->ELO_TEMP) . ";";
                        }
                    }
                }
               
            }

            return $id->RESPONSE . $myelo if $id->RESPONSE;

        } else {
            print "$mmid: No MMID :(\n";
            $response = 0;
        }
        &Log("Game Reported ($name): MMID: $mmid; $report; $response") if $DEBUG;

        return $response;
    }


    sub SetMMR {
        my $self = shift;
        my $mmid = shift;

        my %rating;
        my $pystring;
        my @t1;
        my @t2;
        my @winners;
        my @loosers;
        my $maxl = 0;



        foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
            my $pl = $self->MMIDS->{$mmid}->PLAYERS->{$plname};
            my $win = $pl->TEAM;
            $win = 0 if $pl->TEAM == 2;
            my $ent = "(" . $pl->NAME . "," . $win . "," . $pl->ELO . "," . $pl->SIGMA . ")";
            $pystring .= ", " if $pystring;
            $pystring .= $ent;

            #inline
            if (!exists $rating{$plname}) {
                if (!$pl->LADDER) {
                    $rating{$plname} = new Rating($pl->ELO, $pl->SIGMA);
                } else {
                    $rating{$plname} = new Rating($pl->ELO_LADDER, $pl->SIGMA_LADDER);
                }
                
            }

            if ($pl->TEAM == 1) {
                push(@t1, $rating{$plname});
                push(@winners, $plname);
            } elsif ($pl->TEAM == 2) {
                push(@t2, $rating{$plname});
                push(@loosers, $plname);
            }
            if (!$pl->LADDER) {
                $pl->GAMES($pl->GAMES + 1);
            } else {
                $pl->GAMES_LADDER($pl->GAMES_LADDER + 1);
            }
            if (length($plname) > $maxl) {
                $maxl = length($plname);
            }
        }


        my $tie = 0;
        my $newts = main::rate([\@t1, \@t2], [0, 1]);


        for my $wini (0..$#t1) {
            my $plname = $winners[$wini];
            my $pl = $self->MMIDS->{$mmid}->PLAYERS->{$plname};
            $rating{$plname} = new Rating($newts->[0]->[$wini]->{mu}+0, $newts->[0]->[$wini]->{sigma}+0);
            my $newmmr = $rating{$plname}->{mu}+0;
            if (!$pl->LADDER) {
                $pl->ELO_CHANGE($newmmr - $pl->ELO);        
                $pl->ELO_TEMP($newmmr);
                my $newsigma = $rating{$plname}->{sigma}+0;
                $pl->SIGMA_CHANGE($newsigma - $pl->SIGMA);
                $pl->SIGMA_TEMP($newsigma);
            } else {
                $pl->ELO_CHANGE($newmmr - $pl->ELO_LADDER);        
                $pl->ELO_LADDER($newmmr);
                my $newsigma = $rating{$plname}->{sigma}+0;
                $pl->SIGMA_CHANGE($newsigma - $pl->SIGMA_LADDER);
                $pl->SIGMA_LADDER($newsigma);

            }
            if (length($plname) < $maxl) {
                $plname .= " " x ($maxl - length($plname));
            } 
            &Log("$plname: MMR: " .  sprintf("%.2f", $pl->ELO) . " (" .  sprintf("%.2f", $pl->ELO_CHANGE) . "), SIGMA: " .  sprintf("%.2f", $pl->SIGMA) . " (" .  sprintf("%.2f", $pl->SIGMA_CHANGE) . ")") if $DEBUG > 1;
        }

        for my $losi (0..$#t2) {
            my $plname = $loosers[$losi];
            my $pl = $self->MMIDS->{$mmid}->PLAYERS->{$plname};
            $rating{$plname} = new Rating($newts->[1]->[$losi]->{mu}+0, $newts->[1]->[$losi]->{sigma}+0);
            my $newmmr = $rating{$plname}->{mu}+0;
            if (!$pl->LADDER) {
                $pl->ELO_CHANGE($newmmr - $pl->ELO);        
                $pl->ELO_TEMP($newmmr);
                my $newsigma = $rating{$plname}->{sigma}+0;
                $pl->SIGMA_CHANGE($newsigma - $pl->SIGMA);
                $pl->SIGMA_TEMP($newsigma);
            } else {
                $pl->ELO_CHANGE($newmmr - $pl->ELO_LADDER);        
                $pl->ELO_LADDER($newmmr);
                my $newsigma = $rating{$plname}->{sigma}+0;
                $pl->SIGMA_CHANGE($newsigma - $pl->SIGMA_LADDER);
                $pl->SIGMA_LADDER($newsigma);

            }
            if (length($plname) < $maxl) {
                $plname .= " " x ($maxl - length($plname));
            } 
            &Log("$plname: MMR: " .  sprintf("%.2f", $pl->ELO) . " (" .  sprintf("%.2f", $pl->ELO_CHANGE) . "), SIGMA: " .  sprintf("%.2f", $pl->SIGMA) . " (" .  sprintf("%.2f", $pl->SIGMA_CHANGE) . ")") if $DEBUG > 1;
        }


    }

    sub Matchup {
        my $self = shift;
        my $msg = shift;

        my $response = "";

        my @players = split(/,/, $msg);

        my %players;
        my $lastnoeleast = 'PAX';
        foreach my $ent (@players) {
            $ent =~ s/\s+//g;
            $ent =~ s/;+//g;
            $players{$ent} = 1;
            $lastnoeleast = $ent;
        }

        my $q = new MMqueue();
        my $mmid = $q->Queue($self, $lastnoeleast, \%players, 1);

        if ($mmid) {
            $self->MMIDS->{$mmid}->LADDER(1);
            $response = $self->MMIDS->{$mmid}->BEST;
        }
        return $response;
    }

    sub Ladder {
        my $self = shift;
        my $msg  = shift;

        my $response = "";

        my %rat;
        foreach my $plname (keys %{ $self->MMPLAYERS }) {
            my $pl = $self->MMPLAYERS->{$plname};
            next unless $pl->LADDER;

            my $rat = new Rating($pl->ELO_LADDER, $pl->SIGMA_LADDER);
            my $exp = main::expose($rat);
            $exp = 0 if $exp < 0;
            $rat{$plname} = $exp;
        }

        my $i = 0;
        foreach (reverse sort {$rat{$a} <=> $rat{$b} } keys %rat) {
            $i++;
            my $pl = $self->MMPLAYERS->{$_};
            $response .= "pos" . $i . ": " . $_ . "," . $self->MMPLAYERS->{$_}->GAMES_LADDER . "," . sprintf("%.2f", $rat{$_}) . "," . sprintf("%.2f", $pl->ELO_LADDER) . "," . sprintf("%.2f", $pl->SIGMA_LADDER) . ";";

            {
                lock (%{ $self->MMPLAYERS });
                if (!$pl->CREDENTIAL) {
                    delete $self->MMPLAYERS->{$_};
                } else {
                    if ($pl->LADDER) {
                        $pl->GAMES_LADDER(0);
                        $pl->ELO_LADDER(25.0);
                        $pl->SIGMA_LADDER(25/3);
                    }
                }
            }
        }
        {
            lock (%{ $self->MMIDS });
            for my $i (1..999) {
                if (exists $self->MMIDS->{$i}) {
                    delete $self->MMIDS->{$i};
                }
            }
        }

        return $response;
    }

    sub SetElo {
        my $self = shift;
        my $mmid = shift;
        my $player = shift;

        my @player = @{ $player };

        my %kval;
        for my $i (0 .. $#player) {
            if (exists $self->MMPLAYERS->{$player[$i]}) {
                if ($i <= ($#player / 2)) {
                    #$self->MMPLAYERS->{$player[$i]}->POS($i);
                    #$self->MMPLAYERS->{$player[$i]}->TEAM(1);
                    my $pl = $self->MMPLAYERS->{$player[$i]};
                    $kval{"1"} = 0 if !exists $kval{"1"};
                    my $kval = 40;
                    if ($pl->GAMES > 5) {
                        $kval = 30;
                    }
                    if ($pl->GAMES > 10) {
                        $kval = 20;	
                    }
                    if ($pl->GAMES > 20) {
                        $kval = 10;	
                    }
                    
                    if ($pl->ELO >= 2400) {
                        $kval = 10;	
                    }
                    $kval{"1"} += $kval;
                } else {
                    #$self->MMPLAYERS->{$player[$i]}->POS($i);
                    #$self->MMPLAYERS->{$player[$i]}->TEAM(2);
                    my $pl = $self->MMPLAYERS->{$player[$i]};
                    $kval{"2"} = 0 if !exists $kval{"1"};
                    my $kval = 40;
                    if ($pl->GAMES > 5) {
                        $kval = 30;
                    }
                    if ($pl->GAMES > 10) {
                        $kval = 20;	
                    }
                    if ($pl->GAMES > 20) {
                        $kval = 10;	
                    }
                    
                    if ($pl->ELO >= 2400) {
                        $kval = 10;	
                    }
                    $kval{"2"} += $kval;
                }
            } else {
                if ($i <= ($#player / 2)) {
                    $kval{"1"} += 10;
                } else {
                    $kval{"2"} += 10;
                }
            }
        }

        $kval{"1"} /= ($#player / 2);
        $kval{"2"} /= ($#player / 2);

        print "KVAL Team1: " . int($kval{"1"}) . " | Team2: " . int($kval{"2"}) . "\n";
    
        if (exists $self->MMIDS->{$mmid}) {
            foreach my $pl_name (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
            
                my $pl = $self->MMIDS->{$mmid}->PLAYERS->{$pl_name};
                my $elo_change;
                my $opp_count = 0;

                foreach my $opp_name (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {

                    my $opp = $self->MMIDS->{$mmid}->PLAYERS->{$opp_name};
                    # every opponent
                    if ($opp->TEAM != $pl->TEAM) {
                        $opp_count ++;		
                        my $player = Games::Ratings::LogisticElo->new;
                        
                        if ($pl->TEAM == 1) {
                            $player->set_rating($self->MMPLAYERS->{$pl->NAME}->ELO);
                            $player->set_coefficient(int($kval{$pl->TEAM}));
                            $player->add_game({
                            opponent_rating => $self->MMPLAYERS->{$opp->NAME}->ELO,
                            result => 'win', ## 'win' or 'draw' or 'loss'
                            });
                        } else {
                            $player->set_rating($self->MMPLAYERS->{$pl->NAME}->ELO);
                            $player->set_coefficient(int($kval{$pl->TEAM}));
                            $player->add_game({
                            opponent_rating => $self->MMPLAYERS->{$opp->NAME}->ELO,
                            result => 'loss', ## 'win' or 'draw' or 'loss'
                            });
                        }
                        $elo_change += $player->get_rating_change;
                        #push(@{ $self->OUTPUT->{$pl->POS}{'CHANGE'} }, $player->get_rating_change);
                    }
                }
                if ($opp_count > 0) {
                    $elo_change /= $opp_count;
                    $pl->ELO_CHANGE($elo_change);
                    $pl->ELO($pl->ELO + $elo_change) if $pl->NAME ne "Dummy";
                    &Log($pl->NAME . " new ELO: " . $pl->ELO . " (" . $elo_change . ")");
                    
                    $pl->GAMES($pl->GAMES + 1);
                    $pl->INDB(2);
                }		
            }
        }


    }

    sub Letmeplay {
        my $self = shift;
        my $name = shift;
        my $mod = shift || 0;
        my $num = shift || 0;
        my $skill = shift || 0;
        my $server = shift || 0;

        my $mmid = 0;

        if ($mod) {
            if (exists $self->PLAYERS->{$name}) {
                # reset
                {
                    lock (%{ $self->PLAYERS });
                    delete $self->PLAYERS->{$name};
                }
                sleep 9;
                $self->PLAYERS->{$name} = $self->MMPLAYERS->{$name};
            } else {
                $self->PLAYERS->{$name} = $self->MMPLAYERS->{$name};
            }
        } else {
            # searching again after one (or more) declined
            $self->PLAYERS->{$name} = $self->MMPLAYERS->{$name};
        }

        {
            lock (%{ $self->PLAYERS });
            my $pl = $self->PLAYERS->{$name};
            $pl->MOD($mod) if $mod;
            $pl->NUM($num) if $num;
            $pl->SKILL($skill) if $skill;
            $pl->SERVER($server) if $server;
            $pl->GAME(0);
            $pl->MMID(0);
            $pl->INQUEUE(0);
            $pl->POS(0);
            $pl->CYCLE(0) if $mod;
            $pl->RANDOM(0);
            $pl->LADDER(0);
            $pl->ACCEPTED(0);
            $pl->DECLINED(0);
            $pl->CREDENTIAL(1);
        }

        # maybe good enough to check at next report
        #if ($pl->ELO_TEMP || $pl->ELO_SIGMA) {
        #    $pl->ELO($pl->ELO_TEMP);
        #    $pl->SIGMA($pl->SIGMA_TEMP);
        #    $pl->ELO_TEMP(0);
        #    $pl->SIGMA_TEMP(0);
        #}

        #$mmid = $self->FindGame($name);

        return $mmid;
    }

    sub Accept {
        my $self = shift;
        my $name = shift;
        my $mmid = shift;

        my $result = 0;

        if (!exists $self->MMPLAYERS->{$name}) {
            return "fin";
        }
        my $player = $self->MMPLAYERS->{$name};

        # TODO: fix dirty fix
        #if ($player->MMID != $mmid) {
        #    $mmid = $player->MMID;
        #}

        if (exists $self->MMIDS->{$mmid}) {
        
            {
                lock (%{ $self->MMIDS });
                $self->MMIDS->{$mmid}->ACCEPTED($self->MMIDS->{$mmid}->ACCEPTED + 1);
                if ($self->MMIDS->{$mmid}->ACCEPTED >= $self->MMIDS->{$mmid}->NEED) {
                    $self->MMIDS->{$mmid}->READY(1);
                    &Log("$name: Game accepted: MMID: $mmid; " . $self->MMIDS->{$mmid}->RESPONSE) if $DEBUG;
                }
                $result = $mmid;

                lock (%{ $self->MMPLAYERS });
                $self->MMPLAYERS->{$name}->ACCEPTED(1);
            }

        } else {
            if (exists $self->MMPLAYERS->{$name}) {
                if ($self->MMPLAYERS->{$name}->MMID == $mmid) {
                    {
                        lock (%{ $self->MMPLAYERS });
                        $self->MMPLAYERS->{$name}->ACCEPTED(0);
                        $self->MMPLAYERS->{$name}->MMID(0);
                        &Log($name . " reset2.") if $DEBUG > 1;
                    }
                } else {

                }
            }
        } 

        return $result;
    }

    sub Decline {
        my $self = shift;
        my $name = shift;
        my $mmid = shift;

        my $result = 0;

       if (!exists $self->MMPLAYERS->{$name}) {
            return "fin";
        }

        if (!$mmid) {
            if (exists $self->MMPLAYERS->{$name}) {
                if ($self->MMPLAYERS->{$name}->MMID) {
                    $mmid = $self->PLAYERS->{$name}->MMID;
                }
            }
        }

        # TODO: Let it life and fill with next
        if (exists $self->MMIDS->{$mmid}) {
            {
                lock (%{ $self->PLAYERS });
                foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
                    if (exists $self->PLAYERS->{$plname}) {
                        if ($self->PLAYERS->{$plname}->ACCEPTED || $self->PLAYERS->{$plname}->DECLINED) {
                            #$self->PLAYERS->{$plname}->MMID(0);
                            #$self->PLAYERS->{$plname}->ACCEPTED(0);
                            #&Log($name . ": " . $plname . " reset.") if $DEBUG > 1;
                        }
                    }
                }

                lock (%{ $self->MMIDS });
                $self->MMIDS->{$mmid}->DECLINED($self->MMIDS->{$mmid}->DECLINED+1);    

                lock (%{ $self->MMPLAYERS });
                $self->MMPLAYERS->{$name}->DECLINED(1);
                delete $self->PLAYERS->{$name} if exists $self->PLAYERS->{$name};
                if ($self->MMIDS->{$mmid}->ACCEPTED + $self->MMIDS->{$mmid}->DECLINED >= $self->MMIDS->{$mmid}->NEED) {
                    delete $self->MMIDS->{$mmid};
                }
                $result = 1;
            }
        }


        return $result;
    }

    sub ReadyStatus {
        my $self = shift;
        my $mmid = shift;
        my $name = shift;

        my $response = "Status: 0";

        if (!exists $self->MMPLAYERS->{$name}) {
            return "fin";
        }

        if (exists $self->MMIDS->{$mmid}) {
            my $id = $self->MMIDS->{$mmid};

            # someone declined :(
            if ($id->DECLINED) {
                $response = "Ready: 0";
                if (exists $self->MMPLAYERS->{$name}) {
                    {
                        lock (%{ $self->MMPLAYERS });
                        $self->MMPLAYERS->{$name}->MMID(0);
                        $self->MMPLAYERS->{$name}->ACCEPTED(0);
                        $self->MMPLAYERS->{$name}->DECLINED(0);
                    }
                }
                return $response;
            }
            

            # all accepted?
            if ($self->MMIDS->{$mmid}->READY) {
                $response = "Ready: ";
                $response .= $self->MMIDS->{$mmid}->RESPONSE;
                return $response;
            }

            my $players;
            my $pl_count = 0;
            my $pl_acc = 0;
            # current status of acceptance
            $response = "Status: ";
            my $result = "";
            {
                lock (%{ $self->MMIDS });
                foreach my $plname (sort { $id->PLAYERS->{$a}->POS <=> $id->PLAYERS->{$b}->POS } keys %{ $id->PLAYERS }) {
                    $result .= $id->PLAYERS->{$plname}->ACCEPTED . ";";
                    $players .= "$plname(" . $id->PLAYERS->{$plname}->ACCEPTED . ");";
                    $pl_count++;
                    if ($id->PLAYERS->{$plname}->ACCEPTED) {
                        $pl_acc ++;
                    }
                }
            }
            &Log("$mmid: $players - ($pl_acc/$pl_count)");
            $result = "0" unless $result;
            $response .= $result;
            return $response;

        } else {
            # someone declined :(
            $response = "Ready: 0";
            if (exists $self->MMPLAYERS->{$name}) {
                {
                    lock (%{ $self->MMPLAYERS });
                    $self->MMPLAYERS->{$name}->MMID(0);
                    $self->MMPLAYERS->{$name}->ACCEPTED(0);
                    $self->MMPLAYERS->{$name}->DECLINED(0);
                }
            }
            return $response;
        }

        return $response;
    }


    sub FindGame {
        my $self = shift;
        my $name = shift;
        my $ent = shift;

        if (!exists $self->MMPLAYERS->{$name}) {
            return "fin";
        }

        my $mmid = 0;
        my $random = 0;
        my $notrandom = 0;
        {
            lock (%{ $self->MMPLAYERS });
            if (exists $self->MMPLAYERS->{$name}) {
                $mmid = $self->MMPLAYERS->{$name}->MMID;
                if (!$mmid) {
                    $self->MMPLAYERS->{$name}->ACCEPTED(0);
                } else {
                    &Log("$name: mmid: $mmid");
                    return $mmid;
                }
                $self->PLAYERS->{$name}->CYCLE($self->PLAYERS->{$name}->CYCLE+1);
            } else {

            }

            if (!exists $self->PLAYERS->{$name}) {
                &Log("indahouse") if $DEBUG > 1;

            }

            if (!$mmid && $self->PLAYERS->{$name}->CYCLE >= 2) {        
            
                my $player = $self->MMPLAYERS->{$name};

                my $need = 6;
                if ($player->NUM =~ /^(\d)/) {
                    $need = $1 * 2;
                }
                my %pool;

                foreach my $pl (keys %{ $self->PLAYERS }) {

                    if (!$self->PLAYERS->{$pl}->MMID && ($self->PLAYERS->{$pl}->MOD eq $player->MOD) && ($self->PLAYERS->{$pl}->NUM eq $player->NUM)) {
                        
                        if ($player->RANDOM) {
                            next if $self->PLAYERS->{$pl}->RANDOM == 0;
                        }

                        $pool{$self->PLAYERS->{$pl}->NAME} = $self->PLAYERS->{$pl}->ELO;

                        # randoms
                        if ($self->PLAYERS->{$pl}->RANDOM ) {
                            $random ++;
                        } else {
                            $notrandom ++;
                        }
                    }

                }

                my $count = keys %pool;

                if ($player->RANDOM && $notrandom < 2) {
                    $count = 0;
                }

                if ($count >= $need) {
                    { 
                        lock (%{ $self->MMIDS });
                        my $queue = new MMqueue();
                        $mmid = $queue->Queue($self, $name, \%pool);
                    }

                    if ($mmid) {
                        # Set POS
                        my $server = "NA";
                        my %server;
                        my $resp = "pos0: $mmid;";
                        my $creator = 1;
                        my $maxgames = 0;
                        foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
                            $resp .= "pos" . $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS . ": " . $self->MMIDS->{$mmid}->PLAYERS->{$plname}->NAME . ";";
                            $server{$self->MMPLAYERS->{$plname}->SERVER} ++;
                            if ($self->MMIDS->{$mmid}->PLAYERS->{$plname}->GAMES > $maxgames) {
                                $maxgames = $self->MMIDS->{$mmid}->PLAYERS->{$plname}->GAMES;
                                $creator = $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS;
                            }
                        }

                        foreach (sort { $server{$a} <=> $server{$b} } keys %server) {
                        $server = $_;
                            last;
                        }
                        $resp .= "pos7: $creator;";
                        $resp .= "pos8: $server;";
                        $self->MMIDS->{$mmid}->SERVER($server);
                        $self->MMIDS->{$mmid}->MOD($player->MOD);
                        $self->MMIDS->{$mmid}->NUM($player->NUM);
                        $self->MMIDS->{$mmid}->NEED($need);
                        $self->MMIDS->{$mmid}->RESPONSE($resp);
                    }
                # randoms
                } elsif ($random >= 2 && $self->PLAYERS->{$name}->RANDOM) {
                    
                    # if notrandoms are arround wait a bit
                    my $doit = 0;
                    if ($notrandom) {
                        $self->CYCLE($self->CYCLE + 1);
                        if ($self->CYCLE > 60) {
                            $doit = 1;
                            $self->CYCLE(0);
                        }
                    } else {
                        $self->CYCLE($self->CYCLE + 1);
                        if ($self->CYCLE > 30) {
                            $doit = 1;
                            $self->CYCLE(0);
                        }
                    }

                    if ($doit) {
                        # fill with randoms
                        for (my $i = 1; $i <= ($need - $random); $i++) {

                            my $skill = "Intermediate";
                            my $server = $player->SERVER;
                            my $mod = $player->MOD;
                            my $num = $player->NUM;

                            my $pl = new MMplayer();
                            $pl->NAME("Random" . $i);
                            $pl->MOD($mod);
                            $pl->NUM($num);
                            $pl->SKILL($skill);
                            $pl->SERVER($server);
                            $pl->GAME(0);
                            $pl->MMID(0);
                            $pl->POS(0);
                            $pl->GAMES(0);
                            $pl->RANDOM(1);

                            $self->MMPLAYERS->{$pl->NAME} = $pl;
                            $self->PLAYERS->{$pl->NAME} = $pl;
                            $pool{$pl->NAME} = $pl->ELO;
                        }

                        my $queue = new MMqueue();
                        $mmid = $queue->Queue($self, $name, \%pool);

                        if ($mmid) {
                            # Set POS
                            my $server = "NA";
                            my %server;
                            my $resp = "pos0: $mmid;";
                            my $creator = 1;
                            my $maxgames = 0;
                            foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
                                $resp .= "pos" . $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS . ": " . $self->MMIDS->{$mmid}->PLAYERS->{$plname}->NAME . ";";
                                $server{$self->MMPLAYERS->{$plname}->SERVER} ++;
                                if ($self->MMIDS->{$mmid}->PLAYERS->{$plname}->GAMES >= $maxgames) {
                                    if (!$self->MMIDS->{$mmid}->PLAYERS->{$plname}->NAME =~ /^Random(\d)/) {
                                        $maxgames = $self->MMIDS->{$mmid}->PLAYERS->{$plname}->GAMES;
                                        $creator = $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS 
                                    }
                                }
                            }

                            foreach (sort { $server{$a} <=> $server{$b} } keys %server) {
                            $server = $_;
                                last;
                            }
                            $resp .= "pos7: $creator;";
                            $resp .= "pos8: $server;";
                            $self->MMIDS->{$mmid}->SERVER($server);
                            $self->MMIDS->{$mmid}->MOD($player->MOD);
                            $self->MMIDS->{$mmid}->NUM($player->NUM);
                            $self->MMIDS->{$mmid}->NEED($random);
                            $self->MMIDS->{$mmid}->RESPONSE($resp);
                        }
                    }
                }
            }
        }
        return $mmid;
    }

    sub SetCache {

        &Log("Writing data to db ..") if $DEBUG;
        my $self = shift;
        {
            lock ($lock_db);
            my $mmdb = new MMdb();
            $mmdb->Connect();
            $mmdb->SetCache($self->MMPLAYERS) if $self->MMPLAYERS;
            $mmdb->DBH->disconnect or warn $mmdb->DBH->errstr;
        }
    }

    sub Log {
        my $msg = shift;
        if ($msg) {
            if ($msg ne $msg_bak) {
                print $log "LOGMM: $msg\n";
                print "LOGMM: $msg\n";
                $msg_bak = $msg;
            } else {
                $msg_count ++;
            }

        }
    }

    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);

}

