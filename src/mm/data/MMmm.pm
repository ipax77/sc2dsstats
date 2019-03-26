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

    my $std_elo = 1500;

    sub Result {
        my $self = shift;
        my $name = shift;
        my $res = shift;

        my $mmid = 0;
        my $blame = "";
        my $result = "";
        if ($res =~ /mmid: (\d+); blame: (.*)/) {
            $mmid = $1;
            $blame = $2;
        } elsif ($res =~ /^mmid: (\d+); result: (.*)/) {
            $mmid = $1;
            $result = $2;
        }

        if ($blame) {
            &Log("MMID: $mmid: blame: $blame");
        } else {
            

            # (PewPewPrince, Protoss, 28070), (PAX, Terran, 26520), (Nedved, Terran, 31295) vs (DerLodi, Terran, 26925), (SeyLerT, Zerg, 23710), (VrangelSERB, Protoss, 31000)
            my $logres = $result;
            if ($result =~ /\(([^,]+), ([^,]+), (\d+)\), \(([^,]+), ([^,]+), (\d+)\), \(([^,]+), ([^,]+), (\d+)\) vs \(([^,]+), ([^,]+), (\d+)\), \(([^,]+), ([^,]+), (\d+)\), \(([^,]+), ([^,]+), (\d+)\)/) {
                my @player;
                push(@player, $1);
                push(@player, $4);
                push(@player, $7);
                push(@player, $10);
                push(@player, $13);
                push(@player, $16);

                my $valid = 0;
                my $pcount = 0;
                if (exists $self->MMIDS->{$mmid}) {
                    
                    foreach my $i (0 .. $#player) {
                        foreach (@{ $self->MMIDS->{$mmid} }) {
                            if ($player[$i] eq $_->NAME) {
                                $pcount ++;
                            }
                        }
                    }
                    if ($pcount >= 2) {
                        $valid = 1;
                    } else {
                        &Log("Invalid report from $name ($pcount): $res");
                    }
                } else {
                    &Log("MMID $mmid not found ($name: $res)");
                }

                if ($valid) {
                    my %kval;
                    for my $i (0 .. $#player) {
                        if (exists $self->MMPLAYERS->{$player[$i]}) {
                            if ($i <=3) {
                                $self->MMPLAYERS->{$player[$i]}->POS($i);
                                $self->MMPLAYERS->{$player[$i]}->TEAM(1);

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
                                $self->MMPLAYERS->{$player[$i]}->POS($i);
                                $self->MMPLAYERS->{$player[$i]}->TEAM(2);

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
                            $logres =~ s/$player[$i]/Dummy/g;
                            if ($i <= 3) {
                                $kval{"1"} += 10;
                            } else {
                                $kval{"2"} += 10;
                            }
                        }
                    }

                    $kval{"1"} /= 3;
                    $kval{"2"} /= 3;
                
                    &Log("MMID: $mmid: result: $logres");
                    
                   

                    if (exists $self->MMIDS->{$mmid}) {
                        foreach my $pl (@ {$self->MMIDS->{$mmid} }) {
                        
                            my $elo_change;
                            my $opp_count = 0;
                            $pl->GAMES($pl->GAMES + 1);

                            foreach my $opp (@ { $self->MMIDS->{$mmid} }) {
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
                                $pl->ELO($pl->ELO + $elo_change) if $pl->NAME ne "Dummy";
                                &Log($pl->NAME . "new ELO: " . $pl->ELO);
                                $pl->INDB(2);
                            }		
                        }
                    }
                    delete $self->MMIDS->{$mmid};
                }
            }
        }
    }

    sub Matchup {
        my $self = shift;
        my $player = shift;
        my $db_cache = shift;
        my $players = shift;
        my $skill = shift || "Beginner";
        my $server = shift ||"NA";

        my $response;
        my $mmid = 0;

        if (!exists $self->MMPLAYERS->{$player}) {
            my $mmpl = new MMplayer();
            $mmpl->NAME($player);
            $mmpl->POS(0);
            $mmpl->MMID(0);
            $mmpl->ELO($std_elo);
            $mmpl->GAMES(0);
            $mmpl->ID(0);
            $mmpl->INDB(0);
            $mmpl->SKILL($skill) if $skill;
            $mmpl->SERVER($server) if $server;

            $self->MMPLAYERS->{$player} = $mmpl;
        }


        $mmid = &GetPool($self, $player, $players, $db_cache);
        
        $response = &SetPos($self, $mmid) if $mmid;
        print "MM: $response\n" if $response;

        return $mmid, $response;
    }

    sub SetPos {
        my $self = shift;
        my $mmid = shift;

        my $resp;
        my $creator = 1;
        my $games = 1;
        my $i = 0;
        my %server;
        my $server = "NA";

        $resp = "sc2dsmm: pos0: $mmid;";
        foreach (@{ $self->MMIDS->{$mmid} }) {
            $i++;
            $_->POS($i);
            $resp .= "sc2dsmm: pos" . $i . ": " . $_->NAME . ";";
            if ($i <= 3) {
                $_->TEAM(1);
            } else {
                $_->TEAM(2);
            }
            if ($_->GAMES > $games) {
                $games = $_->GAMES;
                $creator = $_->POS;
            }
            if ($_->SERVER) {
                $server{$_->SERVER} ++;
            }
        }
        
        foreach (sort { $server{$a} <=> $server{$b} } keys %server) {
            $server = $_;
            last;
        }

        $resp .= "sc2dsmm: pos7: $creator;";
        $resp .= "sc2dsmm: pos8: $server;";
        return $resp;
    }

    sub GetPool {
        my $self = shift;
        my $player = shift;
        my $players = shift;
        
        my @pool : shared;
        my @temp_pool;
        my %pool;
        my $elo = $self->MMPLAYERS->{$player}->ELO;
        my $mmid = $self->MMPLAYERS->{$player}->MMID;

        my $c = keys %$players;
        my $allowrandom = 0;

        if (!$mmid) {
            foreach my $name (keys %$players) {

                if (exists $self->MMPLAYERS->{$name} && exists $self->PLAYERS->{$name}) {
                    if ($self->MMPLAYERS->{$name}->MMID == 0) {
                        $pool{$name} = $self->MMPLAYERS->{$name}->ELO;

                        #my $elodiff = $self->MMPLAYERS->{$player}->ELO - $self->MMPLAYERS->{$name}->ELO;
                        if ($self->MMPLAYERS->{$name}->RANDOM) {
                            $allowrandom ++;
                        }
                    } else {

                        # player should be playing or reporting the result or the mmid is already reported
                        # print '$self->MMPLAYERS->{$name}->MMID is not 0 :(' . "\n";
                        if (!exists $self->MMIDS->{$self->MMPLAYERS->{$name}->MMID}) {
                            $self->MMPLAYERS->{$name}->MMID(0);
                            $pool{$name} = $self->MMPLAYERS->{$name}->ELO;
                        } else {
                            $mmid = 0;
                        }

                    }
                } else {
                    # print '$self->MMPLAYERS->{$name} does not exist :(' . "\n";
                    if (exists $self->MMPLAYERS->{$name}) {
                        delete $self->MMPLAYERS->{$name};
                    }
                }
            }

            my $i = 0;
            my $pos = 0;
            my $cc = scalar keys %pool;
            my %random;

            if ($cc < 6 && $allowrandom >= 2) {
                for (my $i = 1; $i <= (6 - $cc); $i++) {
                    my $dummy = new MMplayer();
                    $dummy->NAME("Random" . $i);
                    $dummy->POS(0);
                    $dummy->MMID(0);
                    $dummy->ELO($std_elo);
                    $dummy->GAMES(0);
                    $dummy->ID(0);
                    $dummy->INDB(0);
                    $pool{$dummy->NAME} = $dummy->ELO;
                    $random{$dummy->NAME} = $dummy;
                }
            }
            $cc = scalar keys %pool;
            
            #print '%pool: ' . $cc . "\n";

            if ($cc >= 6) {

                $mmid = 1000 + int rand(9000);
                while (exists($self->MMIDS->{$mmid})) {
                    $mmid = 1000 + int rand(9000);
                }

                foreach my $name (sort { $pool{$a} <=> $pool{$b} } keys %pool) {
                    my $mmpl = $self->MMPLAYERS->{$name};
                    if ($name =~ /^Random\d/) {
                        $mmpl = $random{$name};
                    }
                    next if $mmpl->NAME eq $player;
                    push(@temp_pool, $mmpl);
                    if ($mmpl->ELO == $self->MMPLAYERS->{$player}->ELO) {
                        $pos = $i;
                    }
                    $i++;
                }
                my $start_pos = $pos - 3;

                my $j = $start_pos;

                if ($j > (@temp_pool - 5)) {
                    $j = @temp_pool - 5;
                }
                $j = 0 if $start_pos < 0;
                


                #print "Total: " . @temp_pool . " => POS: " . $j . "\n";

                $self->MMPLAYERS->{$player}->MMID($mmid);
                push(@pool, $self->MMPLAYERS->{$player});
                foreach (1..5) {
                    $temp_pool[$j]->MMID($mmid);
                    push(@pool, $temp_pool[$j]);
                    $j++;
                }
                $self->MMIDS->{$mmid} = \@pool;
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
