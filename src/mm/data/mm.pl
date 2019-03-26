#!/usr/bin/perl
#

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

use Data::Dumper;

my $out = "./data/bab.txt";
my $folder = "./data/";

my %status : shared;
my %players : shared;
my %players_std_3v3 : shared;
my %players_cmdr_3v3 : shared;
my %id : shared;


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

my $t0 = [gettimeofday];

my $lock_db : shared;

#####

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



sub SetCache {
	my $mmdb = new MMdb();
	$mmdb->Connect();
	$mmdb->SetCache($mm->MMPLAYERS) if $mm->MMPLAYERS;
	$mmdb->DBH->disconnect or warn $mmdb->DBH->errstr;
}

sub handle_connection {
    my $socket = shift;
    my $ip = shift;
    my $output = shift || $socket;
    my $doit = 0;
    my $fh;
	my $id;
	my $mmid = 0;
	my $name;
    my $size_check;
	my $count = 0;
	my $plref : shared;
    while (<$socket>) {
		#print $_ . "\n";
        if (/Hello from \[([^\[]+)\]: (.*)/) {

            #$name = encode_utf8($1);
			$name = $1;
			my $param = $2;
			chomp($param);
			chop($param);
			chop($param);
			if (&CheckID($name) && &CheckID($param)) {
				$doit = 1;

				&Log("Hello from $name ($param)");
				my $response;

				my @param = split (/;/, $param);
				
				if ($param[0] eq "Standard") {
					if ($param[1] eq "3v3") {
						$plref = \%players_std_3v3;
					}
				} elsif ($param[0] eq "Commander") {
					if ($param[1] eq "3v3") {
						$plref = \%players_cmdr_3v3;
					}
				} elsif ($param[0] =~ /^mmid/)	{
					my $ref = $mm->Result($name, $param);
					&SetCache();
					last;
				} elsif ($param[0] eq "allowRandoms") {
					if (exists $mm->MMPLAYERS->{$name}) {
						{
							lock (%players); 
							$mm->MMPLAYERS->{$name}->RANDOM(1);
							&Log("Allowing randoms for $name");
						}
					}
				}

				{
					lock (%players);
					$players{$name} = 0;
					$plref->{$name} = 0;
					$mm->PLAYERS(\%players);
					($mmid, $response) = $mm->Matchup($name, $db, $plref, $param[2], $param[3]);
					if ($mmid) {
						{
							lock ($lock_db);
							if (!$lock_db) {
								$lock_db = 1;
								&SetCache();
								$lock_db = 0;
							}
						}
					}
				}

				&Log($mmid . ": " . $response) if $response;
				if ($mmid) {
					$socket->send($response);
				} else {
					$socket->send(&GetSum());
				}
			} else {
				$doit = 0;
				my $response = "Nice try :/";
				&Log($response);
				$socket->send($response);
				last;
			}
        } elsif (/(.*)Have fun\./) {
        	if ($doit) {
        	}
	            my $response = "Data received. TY!";
	            &Log($response);
	           $socket->send($response);
	           shutdown($socket, 1);
	           
	          last;
        } elsif ($doit) {
            
			if (/^Result: (\d+): (.*)/) {
				&Log("Got result from $name");
				$mm->Result($1, $2);
			}
			
			my $sleep = 3 + int rand(4);
			#&Log("$name Sleeping for $sleep seconds.");
			sleep $sleep;
			my $response;
			{
				lock ($plref);
				($mmid, $response) = $mm->Matchup($name, $db, $plref);
			}
			#&Log($response);
			if ($mmid) {
				$socket->send($response);
			} else {
				$socket->send(&GetSum());
			}
			

			#last if $count == 6;

            $size_check .= $_;
            {
            	use bytes;
            	my $bytes_size = length($size_check);
            	if ($bytes_size > 1048576) {
            		&Goodbye("Unexpected file size :/", $socket, $ip);
            		$doit = 0;
            	}
            }
            
            if (/^([^:]+): Thank you.$/) {
            	my $response = "sc2dsmm: You are welcome." . "\r\n";
            	$socket->send($response);
            }
            
            last unless $doit;
        }
    }
	{
		lock (%players);
		delete $players{$name} if exists $players{$name};
		$mm->PLAYERS(\%players);
		#delete $plref->{$name} if exists $plref->{$name};
	}
    $socket->close();
	&Log("$name has disconnected");
}

sub GetSum {
	my $gtotal = keys %{ $mm->MMIDS };
	my $total = keys %players;
	my $cmdr_3v3 = keys %players_cmdr_3v3;
	my $std_3v3 = keys %players_std_3v3;

	my $sum = "sc2dsmm: sum: Total games: " . $gtotal . "; Players searching: " . $total . "; cmdr_3v3: " . $cmdr_3v3 . "; std_3v3: " . $std_3v3 . "\n";
	return $sum;
}

sub CheckID {
	my $id = shift;
	my $good = 1;
	# todo utf8
	if ($id =~ m/[^a-zA-Z0-9]/) {
		#$good = 0;
	} elsif (length($id) > 64) {
		$good = 0;
    }
    return $good;
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
