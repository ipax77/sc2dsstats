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
    use Time::Local;

    use lib ".";
    use MMmm;
    use MMplayer;
    use MMid;

    my $DEBUG = 2;

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
        my $ladder = shift || 0;

        my $mmid = 0;
        my $c = scalar keys %$pool;

        if ($c < $self->NEED) {
            return $mmid;
        }

        #print $c . "\n";
        my %rating;
        my $j;

        my @pool;
        my $mc = "0";
        my $qc = "0";
        foreach (keys %$pool) {
            next if $name eq $_ && !$mm->MMPLAYERS->{$name}->LADDER;
            if ($mm->MMPLAYERS->{$_}->MMID) {
                $mc ++;
                next;
            }
            push(@pool, $_);
        }
        
        my $cc = @pool;
        if ($cc < ($self->NEED - 1)) {
            
            print "MMQU: $name: $cc => " . $mc . "\n";
            return $mmid if !$ladder;
        } else {
            print "MMQU: good\n";
        }

        my @cppool = @pool;

        my $mindiff = 0;
        my $quality = 0;
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
            if (!$mm->MMPLAYERS->{$name}->LADDER) {
                push @shuffle, splice @cppool, rand @cppool, 1 while @shuffle < $self->NEED - 1;
                splice @shuffle, rand @shuffle, 0, $name;
            } else {
                push @shuffle, splice @cppool, rand @cppool, 1 while @shuffle < $self->NEED;
            }
            foreach my $plname (@shuffle) {
                $i++;
                my $pl = $mm->MMPLAYERS->{$plname};
                if (!exists $rating{$plname}) {
                    #print "$name: Setting rat for $plname (" . $pl->ELO . " => " . $pl->SIGMA . ")\n";
                    if (!$pl->LADDER) {
                        $rating{$plname} = new Rating($pl->ELO, $pl->SIGMA);
                    } else {
                        $rating{$plname} = new Rating($pl->ELO_LADDER, $pl->SIGMA_LADDER);
                    }
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
            $quality = main::quality([\@t1, \@t2]);
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
            #last if ($j); # DEBUG
            last if ($mindiff > 0.5 && $j > 10);
            last if ($mindiff > 0.4 && $j > 50);
            last if ($j > 216);
        }

        print "BEST: " . $best . "\n";

        if ($ladder || $self->CheckQuality($mm, $name, $quality)) {
            {
                lock (%{ $mm->PLAYERS });
                $mmid = $self->Setup($mm, \@b1, \@b2);
                $mm->MMIDS->{$mmid}->BEST($best) if $mmid || $ladder;

                if ($mmid) {
                    # remove from queue
                    foreach (keys %{ $mm->MMIDS->{$mmid}->PLAYERS }) {
                        if (exists $mm->PLAYERS->{$_}) {
                            delete $mm->PLAYERS->{$_};
                        }
                    }
                }

            }
        }

        if ($mmid && $mmid > 999) {
            my $t0 = timelocal(localtime());
            $mm->TIMESTAMP->{$mmid} = shared_clone($t0);
            $mm->MMIDS->{$mmid}->TIMESTAMP(shared_clone($t0));
        }

        return $mmid;
    }

    sub CheckQuality {
        my $self = shift;
        my $mm = shift;
        my $name = shift;
        my $quality = shift;

        my $good = 0;

        #return 1; # DEBUG

        # TODO: maybe every player has a right for quality?

        if ($quality >= 0.5) {
            $good = 1;
            return $good;
        }

        my $pl = $mm->MMPLAYERS->{$name};

        if ($pl->CYCLE >= 101) {
            $good = 1;
            return $good;
        }

        my $waittime = 15;
        $waittime = 30 if $pl->MOD eq "2v2";
        $waittime = 60 if $pl->MOD eq "1v1";
        
        if ($quality >= 0.4 && $pl->CYCLE > ($waittime / 3)) {
            $good = 1;
            return $good;
        }


        if ($quality >= 0.3 && $pl->CYCLE > ($waittime / 2)) {
            $good = 1;
            return $good;
        }

        if ($quality >= 0.2 && $pl->CYCLE > ($waittime)) {
            $good = 1;
            return $good;
        }


        return $good;
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
            #return 0 if $pl->MMID;
            $pl->POS($i+1);
            $pl->TEAM(1);
            $pl->MMID($mmid);
            $id->PLAYERS->{$pl->NAME} = $pl;
            
        }
        for my $i (0..$#t2) {
            my $pl = $mm->MMPLAYERS->{$t2[$i]};
            #return 0 if $pl->MMID;
            $pl->POS($i+1+$#t1+1);
            $pl->TEAM(2);
            $pl->MMID($mmid);
            $pl->MMIDS->{$mmid} = 1;
            $id->PLAYERS->{$pl->NAME} = $pl;
            
        }
        
        if ($DEBUG > 1) {
            my $msg = "LOGQU: $mmid: ";
            foreach (keys %{ $id->PLAYERS }) {
                $msg .= "$_;";
            }
            print $msg . "\n";
        }

        return $mmid;
    }

    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}