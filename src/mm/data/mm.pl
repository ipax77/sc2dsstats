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
use Time::Local;

use lib ".";
use MMdb;
use MMmm;
use MMid;
use MMplayer;

use Data::Dumper;

my $out = "./data/bab.txt";
my $folder = "./data/";

my $DEBUG = 2; 
my $msg_back = "";
my $msg_count;

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
                                   Listen => 500,
                                   Proto => 'tcp',
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

my $t0 = timelocal(localtime());


&Log("Waiting for connection ..");

while (my $socket = $listen->accept) {
    my $client_address = $socket->peerhost;
    my $client_port    = $socket->peerport;
    &Log("$client_address $client_port has connected");

	#binmode $socket, ':encoding(UTF-8)';
	binmode $socket;
    async(\&handle_connection, $socket, $client_address)->detach;
	
    my $diff = timelocal(localtime()) - $t0;
    if ($diff > 300 && !$lock_db) {
        $t0 = timelocal(localtime());
        {
            lock ($lock_db);
            $lock_db = 1;
            &Log("Writing data do db ..");
            &SetCache();
            
            $lock_db = 0;
        }
        &CleanupMMIDS($t0);
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

        #print "PING: " . $ping . "\n";
        
        if (&CheckPing($ping)) {

            chomp($ping);
            chop($ping);
            
            ($name, $mmid, $pong) = &PingPong($ping);
            if (!$pong) {
                last;
            } elsif ($pong =~ /fin$/) {
                &Log("$name: fin") if $DEBUG > 1;
                last;
            }
            $pong = "sc2dsmm: " . $pong;
            &Log($ping . " => " . $pong) if $DEBUG > 1;

            $socket->send($pong);

        } else {
            last;
        }

    }

    close $socket;
    
    &Log($name . " disconected.") if $DEBUG;
    # Cleanup
    if (exists $mm->PLAYERS->{$name}) {
        {
            #  is there a pending mmid?
            # TODO - Disconnect? - Delete?
            if ($mm->PLAYERS->{$name}->MMID) {
                if (exists $mm->MMIDS->{$mm->PLAYERS->{$name}->MMID}) {
                    $mm->MMIDS->{$mm->PLAYERS->{$name}->MMID}->DISCONNECT(1);
                }
                
            }
            lock (%{ $mm->PLAYERS });
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
            print "MMPLAYERS setup for $name\n";
        } else {
            
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
            $response = "Result: ";
            $response .= $mm->Result($name, $mmid, $report);
        }

        elsif ($param =~ /Letmeplay: (.*)/) {
            my $opt = $1;
            $response = "Letmeplay: ";
            if ($opt eq "1") {
                $response .= $mm->Letmeplay($name);
            } else {
                my @opt = split(/;/, $opt);
                my $mode = $opt[0];
                my $num = $opt[1];
                my $skill = $opt[2];
                my $server = $opt[3];
                
                $response .= $mm->Letmeplay($name, $mode, $num, $skill, $server);
            }            
        }

        elsif ($param =~ /accept: (\d+)/) {
            $mmid = $1;
            $response = "Accept: ";
            $response .= $mm->Accept($name, $mmid);
            
        }

        elsif ($param =~ /decline: (\d+)/) {
            $mmid = $1;
            $response = "Decline: ";
            $response .= $mm->Decline($name, $mmid);
            
        }

        elsif ($param =~ /Findgame: (\d+)/) {
            my $ent = $1;
            $response = "Findgame: ";
            my $temp_mmid = $mm->FindGame($name, $ent);
            $temp_mmid = &Status() unless $temp_mmid;
            $response .= $temp_mmid;

        }

        elsif ($param =~ /Ready: (\d+)/) {
            $mmid = $1;
            $response = 0;
            my $i = 0;
            while (1) {
                if (exists $mm->MMIDS->{$mmid}) {
                    if ($mm->MMIDS->{$mmid}->READY) {
                        $response = "Ready: ";
                        $response .= $mm->MMIDS->{$mmid}->RESPONSE;
                        last;
                    }
                } else {
                    # someone declined :(
                    $response = "Ready: 0";
                    $mm->MMPLAYERS->{$name}->MMID(0) if exists $mm->MMPLAYERS->{$name};
                    last;
                    
                }
                $i++;
                if ($i > 60) {
                    $response = "Ready: 0";
                    $mm->MMPLAYERS->{$name}->MMID(0) if exists $mm->MMPLAYERS->{$name};
                    last;
                }
                sleep 1;
            }
            
        } 

        elsif ($param =~ /Ready_v2: (\d+)/) {
            $mmid = $1;

            $response = $mm->ReadyStatus($mmid, $name);
        }

        elsif ($param =~ /Status: (\d+)/) {
            my $mmid = $1;
            
            $response = $mm->ReadyStatus($mmid, $name);
        }

        elsif ($param =~ /^allowRandoms: (\d)/) {
            my $rng = $1;
            {
                lock (%{ $mm->MMPLAYERS });
                $mm->MMPLAYERS->{$name}->RANDOM($rng);
            }
            $response = "Findgame: 0";
        }

        elsif ($param =~ /^Ladder: (.*)/) {
            $response = "Ladder: ";
            $response .= $mm->Ladder($1);
        }

        elsif ($param =~ /^Matchup: (.*)/) {
            $response = "Matchup: ";
            $response .= $mm->Matchup($1);
        }
    }
    return $name, $mmid, $response;
}

sub CheckPing {
    my $msg = shift;
    my $good = 0;
    if (length($msg) < 2000) {
        $good = 1;
    } 
}

sub Status {

    my %status;
    foreach my $plname (keys %{ $mm->PLAYERS }) {
        next if $plname =~ /^Random(\d)/;
        my $pl = $mm->PLAYERS->{$plname};
        next if $mm->PLAYERS->{$plname}->MMID;
        $status{$pl->MOD . '|' . $pl->NUM} ++;
    }
    my $c = scalar keys %{$mm->MMIDS};
    my $status = "Games: $c; Players searching: ";
    foreach (sort keys %status) {
        $status .= $_ . " => " . $status{$_} . "; ";
    }
    return $status;
}

sub CleanupMMIDS {
    my $t0 = shift;

    {
        lock (%{ $mm->MMIDS });
        foreach my $mmid (keys %{ $mm->MMIDS }) {

            my $id = $mm->MMIDS->{$mmid};

            if ($id->REPORTED == $id->NEED) {
                {
                    lock (%{ $mm->MMPLAYERS });
                    foreach my $plname (keys %{ $id->PLAYERS }) {
                        my $pl = $id->PLAYERS->{$plname};
                        if (exists $pl->MMIDS->{$mmid}) {
                            delete $pl->MMIDS->{$mmid};
                        }
                    }

                }
                delete $mm->MMIDS->{$mmid};
                &Log("Deleting(1) $mmid.") if $DEBUG;
            }

            elsif ($id->TIMESTAMP && ($t0 - $id->TIMESTAMP) > 86400) {
                delete $mm->MMIDS->{$mmid};
                &Log("Deleting(2) $mmid.") if $DEBUG;
            }
        }
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
        lock (%{ $mm->MMPLAYERS });
        lock (%{ $mm->PLAYERS });
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
        if ($msg ne $msg_back) {
            my $log_date = strftime("%Y%m%d - %H:%M:%S", localtime());
            print $log_date . ": " . $msg . "\n";
            print LOG $log_date . ": " . $msg . "\n";
            $msg_back = $msg;
            $msg_count = 0;
        } else {
            $msg_count++;
        }

	}
}

$listen->close;
