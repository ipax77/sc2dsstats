#!/usr/bin/perl

package MMdb;
use Moose;

use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";

use DBI;
use Encode;
use Data::Dumper;

use lib ".";
use MMplayer;


has 'DBH' => (is => 'rw');
has 'SALT' => (is => 'rw', isa => 'Str');
has 'PWD' => (is => 'rw', isa => 'Str');

sub Connect {
	my $self = shift;
	my $pwd = shift || "Dk5Z=YUS" . encode("utf8", "\xf6") . "F";
	my $user = shift || "sc2dsstats";
	my $db = shift || "sc2dsstats";
	my $dbh;
	#$dbh = DBI->connect("DBI:mysql:database=" . $db . ";host=db",
	$dbh = DBI->connect("DBI:mysql:database=" . $db . ";host=db",
							$user,
							$pwd,
							{'RaiseError' => 1});	
	$self->DBH($dbh);
}

sub Info {
	my $self = shift;
	my $player = shift;

	my $sth = $self->DBH->prepare_cached('SELECT ID, NAME, ELO, GAMES, CREDENTIAL FROM mm_players WHERE Name = ?')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;

	my $id = 0;
	my $elo = 0;
	my $games = 0;
	my $cred = 0;

	$sth->execute()
		or warn "Could not execute statement: " . $sth->errstr;
	if ($sth->rows == 0) {
		#$id = $self->AddPlayer($player);
		#$elo = 1500;	
	} else {	
		while (my @data = $sth->fetchrow_array()) {
			$id = $data[0];
			$elo = $data[2];
			$games = $data[3];
			$cred = $data[4];
		}
	}
	return $id, $elo, $games, $cred;
}

sub GetCache {
	my $self = shift;
	my $sth = $self->DBH->prepare_cached('SELECT ID, NAME, ELO, GAMES, CREDENTIAL FROM mm_players')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;

	$sth->execute()
		or warn "Could not execute statement: " . $sth->errstr;

	my %data;
	while (my @data = $sth->fetchrow_array()) {
		my $mm = new MMplayer;
		print "MMDB: GetCache: $data[1]\n";
		$mm->NAME($data[1]);
		$mm->ID($data[0]);
		$mm->ELO($data[2]);
		$mm->GAMES($data[3]);
		$mm->CREDENTIAL($data[4]);
		$mm->INDB(1);

		$data{$data[1]} =  $mm;
	}
	return \%data;
}

sub SetCache {
	my $self = shift;
	my $ref = shift;
	
	my %new;
	$self->DBH->{AutoCommit} = 0;
	foreach my $name (keys %$ref) {
		
		if ($ref->{$name}->INDB > 1) {
			$self->SetELO($ref->{$name}->ID, $ref->{$name}->ELO, $ref->{$name}->GAMES);
		} elsif (!$ref->{$name}->INDB) {
			$new{$name}{'ELO'} = $ref->{$name}->ELO;
			$new{$name}{'GAMES'} = $ref->{$name}->GAMES;
		}
	}
	$self->DBH->commit();
	my $id = 0;
	foreach my $name (keys %new) {
		$id = $self->AddPlayer($name, $new{$name}{'ELO'}, $new{$name}{'GAMES'}, 1);
		$ref->{$name}->ID($id);
		$ref->{$name}->INDB(1);
	}
	$self->DBH->commit();
	$self->DBH->{AutoCommit} = 1;
}

sub AddPlayer {
	my $self = shift;
	my $player = shift;
	my $elo = shift || 1600;
	my $games = shift || 1;
	my $id = shift || 0;
	
	print "MMDB: Adding player $player\n";

	my $insert_handle = $self->DBH->prepare_cached('INSERT INTO mm_players VALUES (?,?,?,?,?)')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;
	$insert_handle->execute(0, $player, $elo, $games, 0)
		or warn "Could not execute statement: " . $self->DBH->errstr;
	if ($id == 0) {
		$id = $self->DBH->last_insert_id (undef, undef, qw(sc2dsstats player)) or warn "no insert id?";
	}
	return $id;			
}

sub GetELO {
	my $self = shift;
	my $player = shift;
	
	my $sth = $self->DBH->prepare_cached('SELECT PlayerID, Name, ELO, Games FROM player WHERE Name = ?')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;
	
	my $id = 0;
	my $elo = 0;
	my $games = 0;
	
	$sth->execute($player)
		or warn "Could not execute statement: " . $sth->errstr;
	if ($sth->rows == 0) {
		$id = $self->AddPlayer($player);
		$elo = 1500;	
	} else {	
		while (my @data = $sth->fetchrow_array()) {
			$id = $data[0];
			$elo = $data[2];
			$games = $data[3];
		}
	}
	return $id, $elo, $games;
}

sub SetELO {
	my $self = shift;
	my $id = shift;
	my $newelo = shift;
	my $games = shift;
	print "MMDB: Setting ELO for player $id ($newelo)\n";
	if ($newelo && $id) {
		my $update_handle = $self->DBH->prepare_cached('UPDATE mm_players SET ELO = ?, Games = ? WHERE ID = ?')
			or die "Could not prepare_cached statement: " . $self->DBH->errstr;
		$update_handle->execute($newelo, $games, $id)
			or die "Could not execute statement: " . $self->DBH->errstr;
	}	
		
}

no Moose;
__PACKAGE__->meta->make_immutable;
