#!/usr/bin/perl

use threads;
use strict;

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

    my $log;

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
    has 'QUEUE_CMDR_3v3' => (is => 'rw');
    has 'QUEUE_CMDR_2v2' => (is => 'rw');
    has 'QUEUE_CMDR_1v1' => (is => 'rw');
    has 'QUEUE_STD_3v3' => (is => 'rw');
    has 'QUEUE_STD_2v2' => (is => 'rw');
    has 'QUEUE_STD_1v1' => (is => 'rw');


    #Report | Letmeplay | Accept game | Decline game

    sub Result {
        my $self = shift;
        my $name = shift;
        my $mmid = shift;
        my $report = shift;

        my $response = 0;

        if (exists $self->MMIDS->{$mmid}) {
            my $id = $self->MMIDS->{$mmid};
            
            if ($id->REPORTED) {
                &Log("Game Reported 1 ($name): MMID: $mmid; $report");
                return $id->RESPONSE if $id->RESPONSE;
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

                my @res = split(/vs/, $result);
                if ($#res == 1) {
                    my $t1 = 0;
                    my $k = 0;
                    foreach my $i (0..$#res) {
                        my $t = $res[$i];
                        $t =~ s/^\s+//g;
                        
                         my $plrep;
                         print "i: $i\n";
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
                                    $plrep = new MMplayer();
                                    $plrep->NAME('Dummy');
                                    print "$mmid: Just a dummy :( " . $ent[0] . "\n";
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
                                print "$mmid: this should not happen ..\n";
                                last;
                            }
                            push(@player, $plrep) if $plrep;
                            push(@elo_player, $plrep->NAME) if $plrep;
                            $t = $2;
                            $t =~ s/^\s+//g;                            
                            print "$mmid: the new t is $t\n";
                        }
                    }
                }

                #if ($valid >= $id->NEED) {
                if ($valid >= 2) {

                    # TODO: Check blame / multiple reports / difference / leaver
                    {
                        lock (%{ $self->MMIDS });
                        $self->SetElo($mmid, \@elo_player) unless $id->REPORTED;
                        $id->REPORTED($id->REPORTED+1);
                        #$response = 1;
                    }
                } else {
                    print "$mmid: Not valid :( \n";
                }

            }
            

            if ($id->REPORTED) {
                $response = "pos0: $mmid;";
                foreach my $plname (keys %{ $id->PLAYERS }) {
                    my $pl = $id->PLAYERS->{$plname};
                    my $pl_elo = sprintf("%.2f", $pl->ELO);
                    my $pl_elo_change = sprintf("%.2f", $pl->ELO_CHANGE);
                    $response .= "pos" . $pl->POS . ": " . $pl->NAME . "|" . $pl_elo . "|" . $pl_elo_change . "|" . $pl->RACE . "|" . $pl->KILLSUM . ";";
                }
               $id->RESPONSE($response);
            }

        } else {
            print "$mmid: No MMID :(\n";
            $response = 0;
        }
        &Log("Game Reported ($name): MMID: $mmid; $report; $response");
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
                    &Log($pl->NAME . " new ELO: " . $pl->ELO);
                    
                    $pl->GAMES($pl->GAMES + 1);
                    $pl->INDB(2);
                }		
            }
        }


    }

    sub Letmeplay {
        my $self = shift;
        my $name = shift;
        my $mod = shift;
        my $num = shift;
        my $skill = shift;
        my $server = shift;

        my $mmid = 0;

        if (exists $self->PLAYERS->{$name}) {
            # reset

        } else {
            $self->PLAYERS->{$name} = $self->MMPLAYERS->{$name};
        }

        my $pl = $self->PLAYERS->{$name};
        $pl->MOD($mod);
        $pl->NUM($num);
        $pl->SKILL($skill);
        $pl->SERVER($server);
        $pl->GAME(0);
        $pl->MMID(0);
        $pl->POS(0);
        $pl->RANDOM(0);

        #$mmid = $self->FindGame($name);

        return $mmid;
    }

    sub Accept {
        my $self = shift;
        my $name = shift;
        my $mmid = shift;

        my $result = 0;

        my $player = $self->MMPLAYERS->{$name};

        if (exists $self->MMIDS->{$mmid}) {
        
            {
                lock (%{ $self->MMIDS });
                $self->MMIDS->{$mmid}->ACCEPTED($self->MMIDS->{$mmid}->ACCEPTED + 1);
                if ($self->MMIDS->{$mmid}->ACCEPTED >= $self->MMIDS->{$mmid}->NEED) {
                    $self->MMIDS->{$mmid}->READY(1);
                    &Log("Game accepted: MMID: $mmid; " . $self->MMIDS->{$mmid}->RESPONSE);
                }
                $result = $mmid;
            }
        } else {
            
        }

        return $result;
    }

    sub Decline {
        my $self = shift;
        my $name = shift;
        my $mmid = shift;

        my $result = 0;

        # TODO: Let it life and fill with next
        if (exists $self->MMIDS->{$mmid}) {
            {
                lock (%{ $self->PLAYERS });
                foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
                    if (exists $self->PLAYERS->{$plname}) {
                        $self->PLAYERS->{$plname}->MMID(0);
                    }
                }

                lock (%{ $self->MMIDS });
                delete $self->MMIDS->{$mmid};       
                $result = 1;
            }
        }

        return $result;
    }


    sub FindGame {
        my $self = shift;
        my $name = shift;
        my $ent = shift;

        my $mmid = 0;
        my $random = 0;
        my $notrandom = 0;
        {
            lock (%{ $self->PLAYERS });
            if (exists $self->PLAYERS->{$name}) {
                $mmid = $self->PLAYERS->{$name}->MMID;
            } else {

            }

            if (!$mmid) {        
            
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

                    $mmid = 1000 + int rand(9000);
                    while (exists $self->MMIDS->{$mmid}) {
                        $mmid = 1000 + int rand(9000);
                    }

                    my $id = new MMid();
                    $id->MMID($mmid);
                    $self->MMIDS->{$mmid} = $id;

                    my @pool;
                    my $pos = 0;
                    my $i = 0;

                    foreach my $plname (sort { $pool{$a} <=> $pool{$b} } keys %pool) {
                        push(@pool, $plname);
                        if ($player->ELO == $self->MMPLAYERS->{$plname}->ELO) {
                            $pos = $i;
                        }
                        $i++;
                    }

                    my $start_pos = $pos - ($need / 2);

                    my $j = $start_pos;

                    if ($j > (@pool - $need - 1)) {
                        $j = @pool - $need - 1;
                    }
                    $j = 0 if $start_pos < 0;

                    for (0 .. ($need - 1)) {
                        my $plname = $pool[$j];
                        $self->MMIDS->{$mmid}->PLAYERS->{$plname} = $self->MMPLAYERS->{$plname};
                        $self->MMPLAYERS->{$plname}->MMID($mmid);
                        $j++;
                    }

                    # Set POS
                    my $server = "NA";
                    my %server;
                    my $k = 0;
                    my $resp = "pos0: $mmid;";
                    my $creator = 1;
                    my $maxgames = 0;
                    foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
                        $k++;
                        $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS($k);
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



                        $mmid = 1000 + int rand(9000);
                        while (exists $self->MMIDS->{$mmid}) {
                            $mmid = 1000 + int rand(9000);
                        }

                        my $id = new MMid();
                        $id->MMID($mmid);
                        $self->MMIDS->{$mmid} = $id;

                        my @pool;
                        my $pos = 0;
                        my $i = 0;

                        foreach my $plname (sort { $pool{$a} <=> $pool{$b} } keys %pool) {
                            push(@pool, $plname);
                            if ($player->ELO == $self->MMPLAYERS->{$plname}->ELO) {
                                $pos = $i;
                            }
                            $i++;
                        }

                        my $start_pos = $pos - ($need / 2);

                        my $j = $start_pos;

                        if ($j > (@pool - $need - 1)) {
                            $j = @pool - $need - 1;
                        }
                        $j = 0 if $start_pos < 0;

                        &Log("Random: $random; Notrandom: $notrandom; CYCLE: " . $self->CYCLE );

                        for (0 .. ($need - 1)) {
                            my $plname = $pool[$j];
                            $self->MMIDS->{$mmid}->PLAYERS->{$plname} = $self->MMPLAYERS->{$plname};
                            $self->MMPLAYERS->{$plname}->MMID($mmid);
                            $j++;
                        }

                        # Set POS
                        my $server = "NA";
                        my %server;
                        my $k = 0;
                        my $resp = "pos0: $mmid;";
                        my $creator = 1;
                        my $maxgames = 0;
                        foreach my $plname (keys %{ $self->MMIDS->{$mmid}->PLAYERS }) {
                            $k++;
                            $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS($k);
                            $resp .= "pos" . $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS . ": " . $self->MMIDS->{$mmid}->PLAYERS->{$plname}->NAME . ";";
                            $server{$self->MMPLAYERS->{$plname}->SERVER} ++;
                            if ($self->MMIDS->{$mmid}->PLAYERS->{$plname}->GAMES >= $maxgames) {
                                $maxgames = $self->MMIDS->{$mmid}->PLAYERS->{$plname}->GAMES;
                                $creator = $self->MMIDS->{$mmid}->PLAYERS->{$plname}->POS if !$self->MMIDS->{$mmid}->PLAYERS->{$plname}->NAME =~ /^Random(\d)/;
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
        return $mmid;
    }

    sub Log {
        my $msg = shift;
        if ($msg) {
            print $log "LOGMM: $msg\n";
            print "LOGMM: $msg\n";
        }
    }

    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);

}
