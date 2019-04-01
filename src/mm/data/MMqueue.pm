#!/usr/bin/perl

use threads;
use strict;

{
    package MMqueue;
    use Moose;
    use threads::shared;

    use utf8;
    use open IN => ":utf8";
    use open OUT => ":utf8";

    use Data::Dumper;

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

        my $c = scalar keys %$pool;

        if ($c < $self->NEED) {
            return $mmid;
        }

        # generate mmid



        return $mmid;
    }


    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}