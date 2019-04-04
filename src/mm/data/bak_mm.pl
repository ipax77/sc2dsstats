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
use MMid;

use Data::Dumper;

my $out = "./data/bab.txt";
my $folder = "./data/";

my %status : shared;
my %players : shared;
my %players_std_3v3 : shared;
my %players_cmdr_3v3 : shared;
my %id : shared;
my @player_pool : shared;
@player_pool = (\%players, \%players_cmdr_3v3, \%players_std_3v3);

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
	my $result = 0;
    my $size_check;
	my $count = 0;
	my $plref : shared;
	my $decline = 0;
	my $random = 0;
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
				} elsif ($param[0] =~ /^mmid: (\d+)/)	{
					$result = $1;
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
				} elsif ($param[0] =~ /^accept: (\d+)/) {
					{
						lock (%id);
						if (exists $id{$mmid}) {
							$id{$1}->ACCEPTED($id{$1}->ACCEPTED + 1);
						} else {

						}
					}
				} elsif ($param[0] =~ /^decline: (\d+)/) {
					{
						lock (%id);
						if (exists $id{$mmid}) {
							$id{$1}->DECLINED(1);
						}
						$decline = 1;
						$doit = 0;
						last;
					}

				}
				
				elsif ($param[0] eq "Deleteme") {
					&Delete($name);
					$doit = 0;
					last;
				}
				
				{
					lock (%players);
					$players{$name} = 0;
					$plref->{$name} = 0;
					$mm->PLAYERS(\%players);
					($mmid, $response, $random) = $mm->Matchup($name, $db, $plref, $param[2], $param[3]);
					if ($mmid) {

						$players{$name} = $mmid;
						$plref->{$name} = $mmid;

						{
							lock (%id);
							if (!exists $id{$mmid}) {
								my $id = new MMid();
								$id->MMID($mmid);
								$id->ACCEPTED($random);
								$id{$mmid} = $id;
								$id{$mmid}->ACCEPTED($random);
							}
						}

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
					if (!exists $id{$mmid}) {
						my $id = new MMid();
						$id->MMID($mmid);
						$id->REPORTED->{$name} = $mmid;
						if (exists $mm->MMPLAYERS->{$name}) {
							$id->PLAYERS->{$name} = $mm->MMPLAYERS->{$name};
						}
						$id{$mmid} = $id;
					} else {
						$id{$mmid}->REPORTED->{$name} = $mmid;
						if (exists $mm->MMPLAYERS->{$name}) {
							$id{$mmid}->PLAYERS->{$name} = $mm->MMPLAYERS->{$name};
						}
					}

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
			my $random;
			my $response;
			{
				lock ($plref);
				($mmid, $response, $random) = $mm->Matchup($name, $db, $plref);
			}
			
			if ($mmid) {
				&Log("MMID: " . $mmid . " RESP: " . $response . "Rnd: $random");
				if (!exists $id{$mmid}) {
					my $id = new MMid();
					$id->MMID($mmid);
					$id->REPORTED->{$name} = $mmid;
					if (exists $mm->MMPLAYERS->{$name}) {
						$id->PLAYERS->{$name} = $mm->MMPLAYERS->{$name};
					}
					$id{$mmid} = $id;
				} else {
					$id{$mmid}->REPORTED->{$name} = $mmid;
					if (exists $mm->MMPLAYERS->{$name}) {
						$id{$mmid}->PLAYERS->{$name} = $mm->MMPLAYERS->{$name};
					}
					print Dumper $id{$mmid};

				}

				if (!exists $id{$mmid}->REPORTED->{$name}) {
					$socket->send($response);
					$id{$mmid}->REPORTED->{$name} = $mmid;
				} else {

					if ($id{$mmid}->DECLINED) {
						# someone declined - reset
						&Reset($name, $mmid);
						$response = "sc2dsmm: Reset";
						&Log($name . ": " . $response);
						$socket->send($response);
					} elsif ($id{$mmid}->ACCEPTED == keys %{ $id{$mmid}->PLAYERS }) {
						$response = "sc2dsmm: Accepted";
						&Log($name . ": " . $response);
						$socket->send($response);
					}
				}

			} else {
				$socket->send(&GetSum());
			}
			
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
		
	if (!$result) {
		{
			lock (@player_pool);	
			foreach my $ref (@player_pool) {
				#print "Ref: $ref; Name: $name; MMID: $mmid; Ref->name: " . $ref->{$name} . "\n";
				#delete $ref->{$name} if exists $ref->{$name} && $ref->{$name} == $mmid;
				delete $ref->{$name} if exists $ref->{$name};
			}
			$mm->PLAYERS(\%players);

			if (exists $id{$mmid}) {
				$id{$mmid}->DISCONNECT($id{$mmid}->DISCONNECT + 1);
				if ($id{$mmid}->DISCONNECT == keys %{ $id{$mmid}->PLAYERS }) {
					delete $id{$mmid};
				}
			}
		}
	}
	
    $socket->close();
	&Log("$name has disconnected");
}

sub Reset {
	my $name = shift;
	my $mmid = shift;

	{
		lock (@player_pool);
		if (exists $mm->MMPLAYERS->{$name}) {
			my $pl1 = $mm->MMPLAYERS->{$name};
			$pl1->POS(0);
			$pl1->GAME(0);
			$pl1->CREATE(0);
			$pl1->SERVER(0);
			$pl1->RANDOM(0);
			$pl1->MMID(0);
			$pl1->TEAM(0);
		}

		foreach my $ref (@player_pool) {
			if ($ref) {
				foreach (keys %$ref) {
					if (exists $ref->{$name}) {
						$ref->{$name} = 0;
					}
				}
			}
		}
	}
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

sub Delete {
	my $name = shift;

	{
		lock (@player_pool);
		if (exists $mm->MMPLAYERS->{$name}) {
			delete $mm->MMPLAYERS->{$name};
		}

		

		foreach my $ref (@player_pool) {
			if ($ref) {
				foreach (keys %$ref) {
					if (exists $ref->{$name}) {
						delete $ref->{$name};
					}
				}
			}
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
