#!/usr/bin/perl

use strict;
use warnings;

use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";
use Encode;

use threads;
use threads::shared;
use IO::Socket;
use POSIX qw(strftime);
use Time::HiRes qw(gettimeofday tv_interval);

use lib ".";
use MMdb;
use MMmm;
use MMid;

use Data::Dumper;

my $out = "./data/bab.txt";
my $folder = "./data/";

my $DEBUG = 2;

my $log = "log.txt";
open(LOG, ">>", $log) or die "Could not write to $log: $!\n";

END {
	&Log("Writing final data do db ..");
	&SetCache();
}

my $listen = IO::Socket::INET->new(
                                   LocalHost => '0.0.0.0',
                                   LocalPort => 7891,
                                   ReuseAddr => 1,
                                   Listen => 100,
                                   Proto => 'tcp'
                                   ) or die $!;


# Reading in DB cache
#
&Log("Reading in db cache ..");
my $mmdb = new MMdb();
$mmdb->Connect();
my $db;
$db = $mmdb->GetCache();
$mmdb->DBH->disconnect or warn $mmdb->DBH->errstr;

my $mm = new MMmm();
$mm->MMPLAYERS(shared_clone($db));
#print Dumper $mm->MMPLAYERS;

my $lock_db : shared;

my $t0 = [gettimeofday];


&Log("Waiting for connection ..");

while (my $socket = $listen->accept) {
    my $client_address = $socket->peerhost;
    my $client_port    = $socket->peerport;
    &Log("$client_address $client_port has connected");

	#binmode $socket, ':encoding(UTF-8)';
	binmode $socket;
    async(\&handle_connection, $socket, $client_address)->detach;
	{
		lock ($lock_db);

		my $diff = tv_interval($t0);
		if ($diff > 300 && !$lock_db) {
			$lock_db = 1;
			&Log("Writing data do db ..");
			&SetCache();
			$t0 = [gettimeofday];
			$lock_db = 0;
		}
	}
	
}

sub handle_connection {
    my $socket = shift;
    my $ip = shift;
    my $output = shift || $socket;
    my $name = "";
    my $mmid = 0;

    while (<$socket>) {
        my $ping = $_;
        my $pong;

        if (&CheckPing($ping)) {

            ($name, $mmid, $pong) = &PingPong($ping);
            if (!$pong) {
                last;
            }

            &Log($ping . " => " . $pong) if $DEBUG;

            $pong .= "sc2dsmm: ";
            $socket->send($pong);

        } else {
            last;
        }

    }

    # Cleanup
    if (exists $mm->PLAYERS->{$name}) {
        {
            lock ($mm->PLAYERS);
            delete $mm->PLAYERS->{$name};
        }
    }
}

sub PingPong {
    my $msg = shift;

    my $response = 0;
    my $name = "";
    my $mmid = 0;

    if ($msg =~ /Hello from \[([^\[]+)\]: (.*)/) {
        $name = $1;
        my $param = $2;
        $mmid = 0;

        if (!exists $mm->MMPLAYERS->{$name}) {

            my $player = new MMplayer();
            $player->NAME($name);
            $mm->MMPLAYERS->{$name} = $player;
        }


        # Deleteme | Report | Letmeplay | Accept game | Decline game

        if ($param =~ /^Deleteme/) {

            &Log("$name: Deleteme");
            $response = &Delete($name);
            $response .= "Delete: ";
        }

        elsif ($param =~ /^mmid: (\d+); (.*)/) {
            $mmid = $1;
            my $report = $2;
            &Log("Result from $name ($mmid): $report");
            $response = $mm->Result($name, $mmid, $report);
        }

        elsif ($param =~ /Letmeplay: (.*)/) {
            my $opt = $1;
            my @opt = split(/;/, $opt);
            my $mode = $opt[0];
            my $num = $opt[1];
            my $skill = $opt[2];
            my $server = $opt[3];
            $response = $mm->Letmeplay($name, $mode, $num, $skill, $server);
            $response .= "Letmeplay: ";
        }

        elsif ($param =~ /accept: (\d+)/) {
            $mmid = $1;
            $response = $mm->Accept($name, $mmid);
            $response .= "Accept: ";
        }

        elsif ($param =~ /decline: (\d+)/) {
            $mmid = $1;
            $response = $mm->Decline($name, $mmid);
            $response .= "Decline: ";
        }

        elsif ($param =~ /Findgame: (.*)/) {
            $response = $mm->Findgame($name);
            $response .= "Findgame: ";
        }

        elsif ($param =~ /Ready: (.*)/) {
            $mmid = $1;
            my $response = 0;
            my $i = 0;
            while (1) {
                if (exists $mm->MMIDS->{$mmid}) {
                    if ($mm->MMIDS->{$mmid}->READY) {
                        $response = $mm->MMIDS->{$mmid}->RESPONSE;
                        last;
                    }
                } else {
                    last;
                }
                $i++;
                if ($i > 40) {
                    last;
                }
                sleep 1;
            }
            $response .= "Ready: ";
        }
    }
    return $name, $mmid, $response;
}

sub CheckPing {
    my $msg = shift;
    my $good = 0;
    if (length($msg) < 200) {
        $good = 1;
    }
}

sub SetCache {
	my $mmdb = new MMdb();
	$mmdb->Connect();
	$mmdb->SetCache($mm->MMPLAYERS) if $mm->MMPLAYERS;
	$mmdb->DBH->disconnect or warn $mmdb->DBH->errstr;
}

sub Delete {
	my $name = shift;

	{
        lock ($mm->MMPLAYERS);
        lock ($mm->PLAYERS);
		if (exists $mm->MMPLAYERS->{$name}) {
			delete $mm->MMPLAYERS->{$name};
		}

        if (exists $mm->PLAYERS->{$name}) {
            delete $mm->PLAYERS->{$name};
        }
	}

	{
        lock ($lock_db);
        $lock_db = 1;
        &Log("Deleting player $name ..");
        my $mmdb = new MMdb();
        $mmdb->Connect();
        $mmdb->Delete($name);
        $mmdb->DBH->disconnect or warn $mmdb->DBH->errstr;
        
        $lock_db = 0;
	}
}

sub Log {
    my $msg = shift;
	if ($msg) {
		my $log_date = "[" . strftime("%Y%m%d - %H:%M:%S", localtime()) . "] - ";
		print $log_date . $msg . "\n";
		print LOG $log_date . $msg . "\n";
	}
}

$listen->close;