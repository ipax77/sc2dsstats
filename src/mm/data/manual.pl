#!/usr/bin/perl

use strict;
use warnings;

use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";

use lib ".";
use MMdb;
use MMmm;
use MMid;
use MMplayer;


my $result = "ladder.txt";
my $mm = new MMmm();

# read in db cache

my $i = 10000000;

open (my $fh, "<", $result) or die $!;
while (<$fh>) {
    next if /^\s/;
    chomp;

    $i++;
    print "Game: " . $_ . "\n";
    my $mmid = $i;
    my $result = $_;

    my $id = new MMid();
    $id->MMID($mmid);
    $mm->MMIDS->{$mmid} = $id;

    
    my $player = &GetPlayers($mmid, $result);
    my @elo_player;
    foreach (@$player) {
        my $name = $_->NAME;

        if (!exists $mm->MMPLAYERS->{$name}) {
            $mm->MMPLAYERS->{$name} = $_;
        } else {
            
        }
        push(@elo_player, $name);
    }


    my $pystring;
    foreach my $plname (keys %{ $mm->MMIDS->{$mmid}->PLAYERS }) {
        my $pl = $mm->MMIDS->{$mmid}->PLAYERS->{$plname};
        my $win = $pl->TEAM;
        $win = 0 if $pl->TEAM == 2;
        my $ent = "(" . $pl->NAME . "," . $win . "," . $pl->ELO . "," . $pl->SIGMA . ")";
        $pystring .= ", " if $pystring;
        $pystring .= $ent;
    }
    print "Pystring: $pystring\n";
    my $ret = `mypython.py $pystring`;
    print "Pyreturn: " $ret . "\n";
    my @ret = split(/\),\(/ $ret);
    foreach (@ret) {
        my @plinfo = split (/,/, $_);
        my $name = $plinfo[0];
        my $elo = $plinfo[1];
        my $elo_diff = $plinfo[2];
        my $sigma = $plinfo[3];

        if (exists $mm->MMIDS->{$mmid}->PLAYERS->{$name}) {
            my $pl = $mm->MMIDS->{$mmid}->PLAYERS->{$name};
            $pl->ELO($elo);
            $pl->ELO_CHAGE($elo_diff);
            $pl->SIGMA($sigma);
        }
    }

    #$mm->SetElo($mmid, \@elo_player);
    print "\n";
}

my %plsort;
foreach my $name (keys %{ $mm->MMPLAYERS }) {
    $plsort{$name} = $mm->MMPLAYERS->{$name}->ELO;
}

foreach (sort {$plsort{$a} <=> $plsort{$b} } keys %plsort) {
    print "Ladder: " . $_ . "(" . $mm->MMPLAYERS->{$_}->GAMES . ") => " . $plsort{$_} . "\n";
}


sub GetPlayers {
    my $mmid = shift;
    my $result = shift;

    my $id = $mm->MMIDS->{$mmid};

    

    my @player;
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
                        $plrep = $id->PLAYERS->{$ent[0]};
                    } else {

                        if (exists $mm->MMPLAYERS->{$ent[0]}) {
                            $plrep = $mm->MMPLAYERS->{$ent[0]};
                            $id->PLAYERS->{$ent[0]} = $plrep;
                        } else {
                            my $pl = new MMplayer();
                            $pl->NAME($ent[0]);
                            $id->PLAYERS->{$ent[0]} = $pl;
                            $plrep = $id->PLAYERS->{$ent[0]};
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
                    print "$mmid: this should not happen ..\n";
                    last;
                }
                push(@player, $plrep) if $plrep;
                $t = $2;
                $t =~ s/^\s+//g;    
            }
        }
    }
    return \@player;
}

# write db cache