#!/usr/bin/perl

use threads;
use strict;

{
    package MMid;
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

    has 'MMID' => (is => 'rw', isa => 'Num', default => 0);
    has 'TIMESTAMP' => (is => 'rw', default   => sub { () });
    has 'SERVER' => (is => 'rw', isa => 'Str', default => "0");
    has 'MOD' => (is => 'rw', isa => 'Str', default => "0");
    has 'NUM' => (is => 'rw', isa => 'Str', default => "0");
    has 'READY' => (is => 'rw', isa => 'Num', default => 0);
    has 'NEED' => (is => 'rw', isa => 'Num', default => 6);
    has 'ACCEPTED' => (is => 'rw', isa => 'Num', default => 0);
    has 'DECLINED' => (is =>'rw', isa => 'Num', default => 0);
    has 'DISCONNECT' => (is =>'rw', isa => 'Num', default => 0);
    has 'REPORTED' => (is =>'rw', isa => 'Num', default => 0);
    has 'BLAMED' => (is =>'rw', isa => 'Num', default => 0);
    has 'REPORTS' => (is => 'rw', default   => sub { {} });
    has 'PLAYERS' => (is => 'rw', default   => sub { {} });
    has 'RESPONSE' => (is => 'rw', isa => 'Str', default => "0");





    no Moose;
    __PACKAGE__->meta->make_immutable(inline_constructor => 0);
}