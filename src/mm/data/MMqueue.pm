#!/usr/bin/perl

use threads;
use strict;
use Inline Python => <<'END_PYTHON';
from trueskill import Rating, TrueSkill, rate, quality, rate_1vs1, quality_1vs1, setup 
END_PYTHON
setup(25, 25/3, 25/6, 25/300, 1129/61928);

{
    package MMqueue;
    use Moose;
    use threads::shared;

    use utf8;
    use open IN => ":utf8";
    use open OUT => ":utf8";

    use Data::Dumper;
    use Time::HiRes qw(gettimeofday tv_interval);

    use lib ".";
    use MMmm;
    use MMplayer;
    use MMid;

    around 'new' => sub {
        my $orig = shift;
        my $class = shift;
        my $self = $class->$orig(@_);
        my $shared_self : shared = shared_clone($self);

        # here the blessed() already be the version in threads::shared
        #print Dumper($shared_self),"\n";
        return $shared_self;
    };

    has 'NUM' => (is => 'rw', isa => 'Num', default => 0);
    has 'NAME' => (is => 'rw', isa => 'Str');
    has 'PLAYERS' => (is => 'rw', default   => sub { {} });
    has 'POS' => (is => 'rw', isa => 'Num', default => 0);
    has 'NEED' => (is => 'rw', isa => 'Num', default => 6);


    sub Queue {
        my $self = shift;
        my $mm = shift;
        my $name = shift;
        my $pool = shift;

        my $mmid = 0;
        my $c = scalar keys %$pool;

        if ($c < $self->NEED) {
            return $mmid;
        }

        # generate mmid

        print $c . "\n";
        my %rating;
        my $j;

        my @pool;
        foreach (keys %$pool) {
            next if $name eq $_;
            push(@pool, $_);
        }
        my @cppool = @pool;

        my $mindiff = 0;
        my $best;
        my @best;
        my @b1;
        my @b2;
        while(1) {
            $j++;
            my $i = 0;
            my @t1;
            my @t2;
            my @p1;
            my @p2;
            my @shuffle;
            push @shuffle, splice @cppool, rand @cppool, 1 while @shuffle < $self->NEED - 1;
            splice @shuffle, rand @shuffle, 0, $name;
            foreach my $plname (@shuffle) {
                $i++;
                my $pl = $mm->MMPLAYERS->{$plname};
                if (!exists $rating{$plname}) {
                    #print "$name: Setting rat for $plname (" . $pl->ELO . " => " . $pl->SIGMA . ")\n";
                    $rating{$plname} = new Rating($pl->ELO, $pl->SIGMA);
                    #print "Setting rating for $plname (" . $pl->ELO . ")\n";
                }

                
                if ($i <= ($self->NEED / 2)) {
                    push(@t1, $rating{$plname});
                    push(@p1, $plname);
                } elsif ($i <= $self->NEED) {
                    push(@t2, $rating{$plname});
                    push(@p2, $plname);
                }
            }

            my $bab;
            my $quality = main::quality([\@t1, \@t2]);
            foreach (@p1) {
                $bab .= $_ . " ";
            }
            $bab .= "vs ";
            foreach (@p2) {
                $bab .= $_ . " ";
            }
            $bab .= @p1 . " vs " . @p2 . " => " . $quality;
          
            if ($quality > $mindiff) {
                $mindiff = $quality;
                $best = $bab;
                @b1 =  @p1;
                @b2 =  @p2;
            }

            @cppool = @pool;
            last if ($mindiff > 0.5 && $j > 10);
            last if ($mindiff > 0.4 && $j > 50);
            last if ($j > 216);
        }

        print "BEST: " . $best . "\n";

        $mmid = $self->Setup($mm, \@b1, \@b2);

        return $mmid;
    }

    sub Setup {
        my $self = shift;
        my $mm = shift;
        my $t1 = shift;
        my $t2 = shift;

        my @t1 = @{$t1};
        my @t2 = @{$t2};

        my $mmid = 1000 + int rand(9000);
        while (exists $mm->MMIDS->{$mmid}) {
            $mmid = 1000 + int rand(9000);
        }

        my $id = new MMid();
        $id->MMID($mmid);
        $mm->MMIDS->{$mmid} = $id;

        for my $i (0..$#t1) {
            my $pl = $mm->MMPLAYERS->{$t1[$i]};
            $pl->POS($i+1);
            $pl->TEAM(1);
            $pl->MMID($mmid);
            $id->PLAYERS->{$pl->NAME} = $pl;
        }
        for my $i (0..$#t2) {
            my $pl = $mm->MMPLAYERS->{$t2[$i]};
            $pl->POS($i+1+$#t1+1);
            $pl->TEAM(2);
            $pl->MMID($mmid);
            $id->PLAYERS->{$pl->NAME} = $pl;
        }
        #my $t0 = [gettimeofday];
        #$id->TIMESTAMP(\$t0);

        return $mmid;
    }

    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}