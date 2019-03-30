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
    has 'NAME' => (is => 'rw', isa => 'Str');
    has 'MOD' => (is => 'rw', isa => 'Str', default => "0");
    has 'NUM' => (is => 'rw', isa => 'Str', default => "0");
    has 'ID' => (is => 'rw', isa => 'Num', default => 0);
    has 'MMID' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAMES' => (is => 'rw', isa => 'Num', default => 0);
    has 'ELO' => (is => 'rw', isa => 'Num', default => 1500);
    has 'TEAM' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAME' => (is => 'rw', isa => 'Num', default => 0);
    has 'REPORT' => (is => 'rw', isa => 'Str', default => "0");
    has 'CREATE' => (is => 'rw', isa => 'Num', default => 0);
    has 'SKILL' => (is => 'rw', isa => 'Str', default => "Intermediate");
    has 'SERVER' => (is => 'rw', isa => 'Str', default => "0");
    has 'RANDOM' => (is => 'rw', isa => 'Num', default => 0);
    has 'KVAL' => (is => 'rw', isa => 'Num', default => 40);
    has 'INDB' => (is => 'rw', isa => 'Num', default => 0);
    has 'CREDENTIAL' => (is => 'rw', isa => 'Num', default => 1);
    has 'TEAMMATES' => (is => 'rw', default   => sub { {} });
    has 'OPPONENTS' => (is => 'rw', default   => sub { {} });
    has 'PLAYED' => (is => 'rw', default   => sub { {} });





    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}