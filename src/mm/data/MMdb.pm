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

my $DEBUG = 1;

has 'DBH' => (is => 'rw');
has 'SALT' => (is => 'rw', isa => 'Str');
has 'PWD' => (is => 'rw', isa => 'Str');

sub Connect {
	my $self = shift;
	my $pwd = shift || "geheim";
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
	my $sth = $self->DBH->prepare_cached('SELECT ID, NAME, ELO, GAMES, CREDENTIAL, SIGMA FROM mm_players')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;

	$sth->execute()
		or warn "Could not execute statement: " . $sth->errstr;

	my %data;
	while (my @data = $sth->fetchrow_array()) {
		my $mm = new MMplayer;
		print "MMDB: GetCache: $data[1] => $data[2]\n" if $DEBUG;
		$mm->NAME($data[1]);
		$mm->ID($data[0]);
		$mm->ELO($data[2]);
		$mm->GAMES($data[3]);
		$mm->CREDENTIAL($data[4]);
		$mm->SIGMA($data[5]);
		$mm->INDB(1);

		$mm->SIGMA(25/3) if $mm->SIGMA == 0;
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
		next unless $ref->{$name};
		next if $name =~ /^Random(\d)/;
		if ($ref->{$name}->INDB > 1) {
			$self->SetELO($ref->{$name}->ID, $ref->{$name}->ELO, $ref->{$name}->GAMES, $ref->{$name}->SIGMA);
		} elsif (!$ref->{$name}->INDB) {
			$new{$name}{'ELO'} = $ref->{$name}->ELO;
			$new{$name}{'GAMES'} = $ref->{$name}->GAMES;
			$new{$name}{'SIGMA'} = $ref->{$name}->SIGMA;
		}
	}
	$self->DBH->commit();
	my $id = 0;
	foreach my $name (keys %new) {
		$id = $self->AddPlayer($name, $new{$name}{'ELO'}, $new{$name}{'GAMES'}, $new{$name}{'SIGMA'}, 1);
		$ref->{$name}->ID($id);
		$ref->{$name}->INDB(1);
	}
	$self->DBH->commit();
	$self->DBH->{AutoCommit} = 1;
}

sub AddPlayer {
	my $self = shift;
	my $player = shift;
	my $elo = shift || 25.0;
	my $games = shift || 1;
	my $sigma = shift || 25/3;
	my $id = shift || 0;
	
	print "MMDB: Adding player $player\n" if $DEBUG;

	my $insert_handle = $self->DBH->prepare_cached('INSERT INTO mm_players VALUES (?,?,?,?,?,?)')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;
	$insert_handle->execute(0, $player, $elo, $games, $sigma, 0)
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
	my $sigma = shift;
	print "MMDB: Setting ELO for player $id ($newelo)\n" if $DEBUG;
	if ($newelo && $id) {
		my $update_handle = $self->DBH->prepare_cached('UPDATE mm_players SET ELO = ?, GAMES = ?, SIGMA = ?  WHERE ID = ?')
			or warn "Could not prepare_cached statement: " . $self->DBH->errstr;
		$update_handle->execute($newelo, $games, $sigma, $id)
			or warn "Could not execute statement: " . $self->DBH->errstr;
	}	
		
}

sub Delete {
	my $self = shift;
	my $name = shift;
	my $update_handle = $self->DBH->prepare_cached('DELETE FROM mm_players WHERE Name = ?')
		or warn "Could not prepare_cached statement: " . $self->DBH->errstr;
	$update_handle->execute($name)
		or warn "Could not execute statement: " . $self->DBH->errstr;


}

no Moose;
__PACKAGE__->meta->make_immutable;
