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
    has 'ID' => (is => 'rw', isa => 'Num');
    has 'MMID' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAMES' => (is => 'rw', isa => 'Num', default => 0);
    has 'ELO' => (is => 'rw', isa => 'Num');
    has 'TEAM' => (is => 'rw', isa => 'Num', default => 0);
    has 'GAME' => (is => 'rw', isa => 'Num', default => 0);
    has 'REPORT' => (is => 'rw', isa => 'Str', default => "0");
    has 'CREATE' => (is => 'rw', isa => 'Num', default => 0);
    has 'SKILL' => (is => 'rw', isa => 'Str');
    has 'SERVER' => (is => 'rw', isa => 'Str', default => "0");
    has 'RANDOM' => (is => 'rw', isa => 'Num', default => 0);
    has 'KVAL' => (is => 'rw', isa => 'Num');
    has 'INDB' => (is => 'rw', isa => 'Num');
    has 'CREDENTIAL' => (is => 'rw', isa => 'Num');
    has 'TEAMMATES' => (is => 'rw', default   => sub { {} });
    has 'OPPONENTS' => (is => 'rw', default   => sub { {} });
    has 'PLAYED' => (is => 'rw', default   => sub { {} });





    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}