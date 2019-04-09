#!/usr/bin/perl

use threads;
use strict;

{
    package MMplayer;
    use Moose;
    use threads::shared;

    use utf8;
    use open IN => ":utf8";
    use open OUT => ":utf8";

    use Data::Dumper;

    around 'new' => sub {
        my $orig = shift;
        my $class = shift;
        my $self = $class->$orig(@_);
        my $shared_self : shared = shared_clone($self);

        # here the blessed() already be the version in threads::shared
        #print Dumper($shared_self),"\n";
        return $shared_self;
    };

    has 'POS' => (is => 'rw', isa => 'Num', default => 0);
    has 'LADDER' => (is => 'rw', isa => 'Num', default => 0);
    has 'QUEUE_POS' => (is => 'rw', isa => 'Num', default => 999);
    has 'INQUEUE' => (is => 'rw', isa => 'Num', default => 0);
    has 'CYCLE' => (is => 'rw', isa => 'Num', default => 0);
    has 'NAME' => (is => 'rw', isa => 'Str');
    has 'MOD' => (is => 'rw', isa => 'Str', default => "0");
    has 'NUM' => (is => 'rw', isa => 'Str', default => "0");
    has 'ID' => (is => 'rw', isa => 'Num', default => 0);
    has 'MMID' => (is => 'rw', isa => 'Num', default => 0);
    has 'MMIDS' => (is => 'rw', default   => sub { {} });
    has 'ACCEPTED' => (is => 'rw', isa => 'Num', default => 0);
    has 'DECLINED' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAMES' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAMES_LADDER' => (is => 'rw', isa => 'Num', default => 0);
    has 'ELO' => (is => 'rw', default => 25.0);
    has 'ELO_TEMP' => (is => 'rw', default => 0);
    has 'ELO_LADDER' => (is => 'rw', default => 25.0);
    has 'ELO_CHANGE' => (is => 'rw', default => 0);
    has 'SIGMA' => (is => 'rw', default => 25/3);
    has 'SIGMA_TEMP' => (is => 'rw', default => 0);
    has 'SIGMA_LADDER' => (is => 'rw', default => 25/3);
    has 'SIGMA_CHANGE' => (is => 'rw', default => 0);
    has 'RATING' => (is => 'rw', default => 0);
    has 'RACE' => (is => 'rw', isa => 'Str', default => "0");
    has 'KILLSUM' => (is => 'rw', isa => 'Str', default => "0");
    has 'TEAM' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAME' => (is => 'rw', isa => 'Num', default => 0);
    has 'REPORT' => (is => 'rw', isa => 'Str', default => "0");
    has 'CREATE' => (is => 'rw', isa => 'Num', default => 0);
    has 'SKILL' => (is => 'rw', isa => 'Str', default => "Intermediate");
    has 'SERVER' => (is => 'rw', isa => 'Str', default => "0");
    has 'RANDOM' => (is => 'rw', isa => 'Num', default => 0);
    has 'KVAL' => (is => 'rw', isa => 'Num', default => 40);
    has 'INDB' => (is => 'rw', isa => 'Num', default => 0);
    has 'CREDENTIAL' => (is => 'rw', isa => 'Num', default => 0);
    has 'TEAMMATES' => (is => 'rw', default   => sub { {} });
    has 'OPPONENTS' => (is => 'rw', default   => sub { {} });
    has 'PLAYED' => (is => 'rw', default   => sub { {} });
    





    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}