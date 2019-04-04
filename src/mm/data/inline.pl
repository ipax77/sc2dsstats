#!/usr/bin/perl

use strict;
use warnings;

use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";

use Data::Dumper;
use Time::HiRes qw(gettimeofday tv_interval);

use lib ".";
use MMdb;
use MMmm;
use MMid;
use MMplayer;
use MMqueue;

use Inline Python => <<'END_PYTHON';
from trueskill import Rating, TrueSkill, rate, quality, rate_1vs1, quality_1vs1, setup, expose
END_PYTHON

setup(25, 25/3, 25/6, 25/300, 1129/61928);


my $result = "ladder.txt";
my $mm = new MMmm();

# read in db cache

my $i = 10000000;

my %rating;

open (my $fh, "<", $result) or die $!;
while (<$fh>) {
    next if /^\s/;
    chomp;

    $i++;
    print "-" x 80 . "\n";
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

    $mm->SetMMR($mmid);

}

my @pool = ('PAX', 'Panzerfaust', 'Arkos', 'Raggy',  'Bonejury', 'Lolz');

my %pool;
#foreach (keys %{ $mm->MMPLAYERS }) {
foreach (@pool) {
    $pool{$_} = $mm->MMPLAYERS->{$_}->ELO;
}

my $t0 = [gettimeofday];

my $q = new MMqueue();
my $game = $q->Queue($mm, 'PAX', \%pool);

my $diff = tv_interval($t0);

print "Time: $diff\n";
print $game . "\n";



my %plsort;
foreach my $name (keys %{ $mm->MMPLAYERS }) {
    $plsort{$name} = $mm->MMPLAYERS->{$name}->ELO;
}

foreach (sort {$plsort{$a} <=> $plsort{$b} } keys %plsort) {
    my $mmr = $plsort{$_};
    $mmr = sprintf("%.2f", $mmr);
    my $sigma = $mm->MMPLAYERS->{$_}->SIGMA;
    $sigma = sprintf("%.2f", $sigma);

    print "MU (Sigma): " . $_ . "(" . $mm->MMPLAYERS->{$_}->GAMES . ") => " . $mmr . " (" . $sigma . ")" . "\n";
}

my %rat;
foreach my $plname (keys %{ $mm->MMPLAYERS }) {
    my $pl = $mm->MMPLAYERS->{$plname};
    my $rat = new Rating($pl->ELO, $pl->SIGMA);
    my $exp = main::expose($rat);
    $exp = 0 if $exp < 0;
    $rat{$plname} = $exp;
}

foreach (sort {$rat{$a} <=> $rat{$b} } keys %rat) {
    print "Ladder: " . $_ . "(" . $mm->MMPLAYERS->{$_}->GAMES . ") => " . $rat{$_} . "\n";
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