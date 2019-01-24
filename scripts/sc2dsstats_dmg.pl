use strict;
use warnings;
use strict;
use warnings;
use GD::Graph::bars;
use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";
use XML::Simple;
use GD::Graph::bars;
use GD::Graph::hbars;
use POSIX qw(strftime);
use File::Basename;
use Encode qw(decode encode);

my $DEBUG = 2;

my $main_path = dirname($0);
$main_path = dirname($main_path);

my $config_file = $main_path . "/sc2dsstats.exe.Config";
my $csv = $main_path . "/stats.csv";
my $logfile = $main_path . "/log_dmg.txt";
my $png = $main_path . "/dmg.png";

my %cfg;

open(LOG, ">", $logfile) or die $!;

&Log("Reading in config file $config_file ..", 1);

my $cfg = XMLin($config_file);

#$cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'}

$DEBUG = $cfg->{'appSettings'}{'add'}{'DEBUG'}{'value'};

if (defined $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'}) {
	$cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'} = encode("UTF-8", $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'});
}
my $player = $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'};
my $skip_std = 0;
if (defined $cfg->{'appSettings'}{'add'}{'STD'}{'value'}) {
	$skip_std = 	$cfg->{'appSettings'}{'add'}{'STD'}{'value'};
}



#my $start_date = $cfg->{'appSettings'}{'add'}{'START_DATE'}{'value'};
#my $end_date = $cfg->{'appSettings'}{'add'}{'END_DATE'}{'value'};

my $start_date = "19700101000000";
my $ed_temp = time + 60 * 24 * 60;
my $end_date = strftime("%Y%m%d%H%M%S", localtime($ed_temp));

my $interest = "Abathur";
my $player_stats = 1;
my $opp_stats = 0;
my $basedon = "army";
my $alignment = "horizontal";

if (defined $ARGV[0]) {
	$start_date = $ARGV[0];
}
if (defined $ARGV[1]) {
	$end_date = $ARGV[1];
}
if (defined $ARGV[2]) {
	$skip_std = $ARGV[2];
}
if (defined $ARGV[3]) {
	$interest = $ARGV[3];
	$opp_stats = 1;	
} if (defined $ARGV[4]) {
	$player_stats = $ARGV[4];	
} if (defined $ARGV[5]) {
	$basedon = $ARGV[5];	
}
if (defined $ARGV[6]) {
	$alignment = $ARGV[6];	
}
if (defined $ARGV[7]) {
	$opp_stats = $ARGV[7];	
}

my %global;

my @duration;

my @commanders = ('Abathur' , 'Alarak', 'Artanis', 'Dehaka', 'Fenix', 'Tychus', 'Horner', 'Karax', 'Kerrigan' ,'Raynor', 'Stukov', 'Swann', 'Nova', 'Vorazun', 'Zagara');

# 3v3
my @opp = (0, 4, 5, 6, 1, 2, 3);

my %opp;
my %oppdmg;


my %sum;
my %skip;

my %dmg;

&Log("Reading in stats file $csv ..", 1);

&ReadCSV($csv, \%sum);

foreach my $replay (keys %sum) {
	
	my $orace;
	my $race;
	
	foreach my $name (sort keys %{ $sum{$replay} }) {
		
		my $race2 = $sum{$replay}{$name}{'RACE'};
		my $team = $sum{$replay}{$name}{'TEAM'};
		my $win = $sum{$replay}{$name}{'RESULT'};
		my $killsum = $sum{$replay}{$name}{'KILLSUM'};
		my $duration = $sum{$replay}{$name}{'DURATION'};
		my $gametime = $sum{$replay}{$name}{'GAMETIME'};
		my $id = $sum{$replay}{$name}{'PLAYERID'};
		my $income = $sum{$replay}{$name}{'INCOME'};
		my $army = $sum{$replay}{$name}{'ARMY'};
		
		next if &Skip($replay);

		if ($player_stats == 1) {
			if ($name ne $player) {
				next;
			}	
		}
		
		my $dpv;		
		if ($basedon eq "army") {
			$dpv = $killsum / $army;
			if ($dpv < 0.3 || $dpv > 3) {
				&Log("Skipping strange things: $replay => $race2 => $dpv", 2);
			}
		} elsif ($basedon eq "income") {
			$dpv = $killsum / $income;
		} elsif ($basedon eq "time") {
			$dpv = $killsum / $sum{$replay}{$player}{'DURATION'};
		}
		
		
		$dmg{$race2}{'GAMES'} ++;
		if (!defined $dmg{$race2}{'VALUE'}) { 
			$dmg{$race2}{'VALUE'} = $dpv;
		} else {
			$dmg{$race2}{'VALUE'} += $dpv;
		}
		
		if ($opp_stats) {
			foreach my $oname (keys %{ $sum{$replay} }) {
				if ($sum{$replay}{$oname}{'PLAYERID'} == $opp[$id]) {
					$race = $race2;
					$orace = $sum{$replay}{$oname}{'RACE'};
				}
			}
			$oppdmg{$race}{$orace}{'GAMES'}++;
			if (!defined $oppdmg{$race}{$orace}{'VALUE'}) { 
				$oppdmg{$race}{$orace}{'VALUE'} = $dpv;
			} else {
				$oppdmg{$race}{$orace}{'VALUE'} += $dpv;
			}
		}
		


	}
}

my @x;
my @y;
my $title;
my $y_label;
my $add = "World";
if ($player_stats == 1) {
	$add = "Player";
}
my $max = 0;

my %opp_sum;
my %dmg_sum;

if ($opp_stats) {
	&Log("Opp stats for $interest:", 1);
	foreach my $r (keys %oppdmg) {
		
		if ($r eq $interest) {
			foreach my $or (keys %{ $oppdmg{$r} }) {
				my $avg = $oppdmg{$r}{$or}{'VALUE'} / $oppdmg{$r}{$or}{'GAMES'};
				$avg = sprintf("%.4f", $avg);
				&Log("vs " . $or . " (" . $oppdmg{$r}{$or}{'GAMES'} . ")" . " => " . $avg, 1);
				$opp_sum{"vs " . $or . " (" . $oppdmg{$r}{$or}{'GAMES'} . ")"} = $avg;
			}
		}
	}
	
	foreach (sort { $opp_sum{$a} <=> $opp_sum{$b} } keys %opp_sum) {
		push(@x, $_);
		push(@y, $opp_sum{$_});
		if ($opp_sum{$_} > $max) {
			$max = $opp_sum{$_};	
		}	
	}
		
	$add .= " $interest ";
		
} else {
	foreach my $r ( keys %dmg) {
		my $avg = $dmg{$r}{'VALUE'} / $dmg{$r}{'GAMES'};
		$avg = sprintf("%.4f", $avg);
		&Log($r . " (" . $dmg{$r}{'GAMES'} . ")" . " => " . $avg, 1);
		$dmg_sum{$r . " (" . $dmg{$r}{'GAMES'} . ")"} = $avg;
	}
	foreach (sort { $dmg_sum{$a} <=> $dmg_sum{$b} } keys %dmg_sum) {
		push(@x, $_);
		push(@y, $dmg_sum{$_});
		if ($dmg_sum{$_} > $max) {
			$max = $dmg_sum{$_};	
		}	
	}
}


my %sklog;
my $skip_total;
my $games_total;

foreach my $r (sort keys %skip) {
	foreach my $s (sort keys %{ $skip{$r} }) {
		$sklog{$s} ++;
		if ($s eq "ARMY") {
			#print "ARMY => $r\n";	
		}
		if ($s eq "INCOME") {
			#print "INCOME => $r\n";	
		}
		if ($s eq "STARTDATE") {
			#print "STARTDATE => $r\n";	
		}
	}
	if ($skip{$r}{'SKIP'} == 1) {
		$skip_total ++;	
	}
	if ($skip{$r}{'SKIP'} == 0) {
		$games_total ++;	
	}
}

$sklog{'TOTAL'} = $skip_total - $sklog{'BETA'} - $sklog{'HOTS'} - $sklog{'STD'};

&Log("Skipped:", 1);
foreach (sort keys %sklog) {
	my $p = $sklog{$_} * 100 / $sklog{'SKIP'};
	$p = sprintf("%.2f", $p);
	&Log($_ . " => " . $sklog{$_} . " ($p%)", 1);	
}

&Log("Total games: $games_total", 1);

my $sd;
my $ed;

if ($start_date =~ /^(\d{8})/) {
	$sd = $1;
}

if ($end_date =~ /^(\d{8})/) {
	$ed = $1;	
}
if ($basedon eq "army") {
	$title = "DPV $add ($sd to $ed)\n";
	$y_label = "DPV (KilledArmyValue / SpawnedArmyValue)";
} elsif ($basedon eq "income") {
	$title = "DPM $add ($sd to $ed)\n";
	$y_label = "DPV (KilledArmyValue / MineralsCollected)";
} elsif ($basedon eq "time") {
	$title = "DPS $add ($sd to $ed)\n";
	$y_label = "DPV (KilledArmyValue / GameDuration)";
}



my $y_max_value = $max * 1.1;
$y_max_value = sprintf("%.1f", $y_max_value);

&PrintGraph($title, "Commanders", $y_label, 1, $y_max_value, $png, \@x, \@y, $alignment);






close(LOG);

sub PrintGraph {

my $title = shift;
my $x_label = shift;
my $y_label = shift;
my $x_tick_number = shift;
my $y_max_value = shift;
my $png = shift;
my $x = shift;
my $y = shift;
my $alignment = shift;

my $graph;
my $x_labels_vertical = 0;
if ($alignment eq "horizontal") {
	$graph = GD::Graph::bars->new(1600, 600);
	$x_labels_vertical = 1;
} elsif ($alignment eq "vertical") {
 	$graph = GD::Graph::hbars->new(1000, 800);
 	$x_labels_vertical = 0;
}
$graph->set(
    x_label             => $x_label . '(generated by https://github.com/ipax77/sc2dsstats)',
    y_label             => $y_label,
    title               => $title,
    
    # shadows
    bar_spacing     => 8,
    shadow_depth    => 4,
    shadowclr       => 'dred',
        
    y_max_value         => $y_max_value,
    y_min_value         => 0,
    y_tick_number       => 1,
    y_label_skip        => 1,
    x_label_skip        => 1,
    x_labels_vertical => $x_labels_vertical,
    
    bar_spacing     => 10,
    accent_treshold => 200,
    transparent     => 0,
    
    transparent         => 0,
    bgclr               => 'white',
    long_ticks          => 1,
) or die $graph->error;




my @data = ($x, $y);


$graph->set_title_font('C:/Windows/Fonts/arial.ttf', 18);
$graph->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
$graph->set_legend_font('C:/Windows/Fonts/arial.ttf', 18);
$graph->set_x_axis_font('C:/Windows/Fonts/arial.ttf', 14);
$graph->set(show_values => 1 );
$graph->set_values_font('C:/Windows/Fonts/arial.ttf', 12);

$graph->set( dclrs => [ qw(blue blue blue blue) ] );
my $gd = $graph->plot(\@data) or die $graph->error;
 
&Log("Printing graph to $png ..", 1); 
 
open(IMG, ">:unix", $png) or die $!;
binmode IMG;
print IMG $gd->png;
close(IMG);
	
	
}


sub Log {
	my $msg = shift;	
	my $debug = shift;
	print LOG $msg . "\n" if $DEBUG >= $debug;
	print $msg . "\n" if $DEBUG >= $debug;
}


sub Skip {
	my $replay = shift;

	if (defined $skip{$replay}{'SKIP'} && $skip{$replay}{'SKIP'} == 1) {
		return 1;
	}
	
	if (defined $skip{$replay}{'SKIP'} && $skip{$replay}{'SKIP'} == 0) {
		return 0;	
	}
	
	$skip{$replay}{'SKIP'} = 0;
	
	my $player = $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'};


	# player
	if (!defined $sum{$replay}{$player}) {
		$skip{$replay}{'PLAYER'} ++;
		$skip{$replay}{'SKIP'} = 1;
		&Log("Skipping $replay due to NO PLAYER", 2);
		return 1;	
	}

	# beta
	if (defined $cfg->{'appSettings'}{'add'}{'BETA'}{'value'} && $cfg->{'appSettings'}{'add'}{'BETA'}{'value'}) {
		if ($replay =~ /Beta/) {
			$skip{$replay}{'BETA'} ++;
			$skip{$replay}{'SKIP'} = 1;
			&Log("Skipping $replay due to BETA", 2);
			return 1;
		}
	}
	
	# hots
	if (defined $cfg->{'appSettings'}{'add'}{'HOTS'}{'value'} && $cfg->{'appSettings'}{'add'}{'HOTS'}{'value'}) {
		if ($replay =~ /HotS/) {
			$skip{$replay}{'HOTS'} ++;
			$skip{$replay}{'SKIP'} = 1;
			&Log("Skipping $replay due to HOTS", 2);
			return 1;
		}
	}
	
	# gametime 
	
	if ($sum{$replay}{$player}{'GAMETIME'} < $start_date) {
		$skip{$replay}{'STARTDATE'} ++;
		$skip{$replay}{'SKIP'} = 1;
		&Log("Skipping $replay due to STARTDATE", 2);
		return 1;
	} elsif ($sum{$replay}{$player}{'GAMETIME'} > $end_date) {
		$skip{$replay}{'ENDDATE'} ++;
		$skip{$replay}{'SKIP'} = 1;
		&Log("Skipping $replay due to ENDDATE", 2);
		return 1;			
	}
	
	# gamemode (3v3)
	
	if (keys %{ $sum{$replay} } != 6) {
		$skip{$replay}{'NOT3v3'} ++;
		$skip{$replay}{'SKIP'} = 1;	
		&Log("Skipping $replay due to NOT3v3", 2);
		return 1;	
	}		

	# duration
	
	if ($sum{$replay}{$player}{'DURATION'} <= $cfg->{'appSettings'}{'add'}{'DURATION'}{'value'}) {
		$skip{$replay}{'DURATION'} ++;
		$skip{$replay}{'SKIP'} = 1;	
		&Log("Skipping $replay due to DURATION", 2);
		return 1;
	}

	foreach my $name (keys %{ $sum{$replay} }) {

		# normal
		if ($skip_std) {
				if ($sum{$replay}{$name}{'RACE'} eq "Zerg" || $sum{$replay}{$name}{'RACE'} eq "Protoss" || $sum{$replay}{$name}{'RACE'} eq "Terran") {
				$skip{$replay}{'STD'} ++;
				$skip{$replay}{'SKIP'} = 1;	
				&Log("Skipping $replay due to STD", 2);
				return 1;
			} 	
		}

		# leaver
		if (($sum{$replay}{$player}{'DURATION'} - $sum{$replay}{$name}{'DURATION'}) >  $cfg->{'appSettings'}{'add'}{'LEAVER'}{'value'}) {
			$skip{$replay}{'LEAVER'} ++;
			$skip{$replay}{'SKIP'} = 1;	
			&Log("Skipping $replay due to LEAVER", 2);
			return 1;
		}
		
		# killsum
		if ($sum{$replay}{$name}{'KILLSUM'} < $cfg->{'appSettings'}{'add'}{'KILLSUM'}{'value'}) {
			$skip{$replay}{'KILLSUM'} ++;
			$skip{$replay}{'SKIP'} = 1;	
			&Log("Skipping $replay due to KILLSUM", 2);
			return 1;
		}

		# army
		if ($sum{$replay}{$name}{'ARMY'} < $cfg->{'appSettings'}{'add'}{'ARMY'}{'value'}) {
			$skip{$replay}{'ARMY'} ++;
			$skip{$replay}{'SKIP'} = 1;	
			&Log("Skipping $replay due to ARMY", 2);
			return 1;
		}

		#income
		if ($sum{$replay}{$name}{'INCOME'} < $cfg->{'appSettings'}{'add'}{'INCOME'}{'value'}) {
			$skip{$replay}{'INCOME'} ++;
			$skip{$replay}{'SKIP'} = 1;	
			&Log("Skipping $replay due to INCOME", 2);
			return 1;
		}
	}		
	
	
	return 0;
	
}


sub ReadCSV {
	my $csv = shift;
	my $sumref = shift;
	
	
	if (-e $csv) {
        open(SUM, "<", $csv) or die "Could not read $csv: $!\n";
        while (<SUM>) {
        	chomp;
			if (/^\d/) {
				my @line = split(/;/, $_);
				my $game = $line[0];
				my $replay = $line[1];
				my $name = $line[2];
				my $race = $line[3];
				my $race2 = $line[4];
				my $team = $line[5]; 
				my $result = $line[6];
				my $killsum = $line[7];
				my $duration = $line[8];
				my $gametime = $line[9];
				my $playerid = $line[10];
				my $income = $line[11];
				my $army = $line[12];
				
				$game =~ s/\s+//g;
				$replay =~ s/^\s+//;
				$name =~ s/\s+//g;
				$race =~ s/\s+//g;
				$race2 =~ s/\s+//g;
				$team =~ s/\s+//g;
				$result =~ s/\s+//g;
				$killsum =~ s/\s+//g;
				$duration =~ s/\s+//g;
				$gametime =~ s/\s+//g;
				$playerid =~ s/\s+//g;
				$income =~ s/\s+//g;
				$army =~ s/\s+//g;
			
				$sumref->{$replay}{$name}{'RACE'} = $race2;
				$sumref->{$replay}{$name}{'TEAM'} = $team;
				$sumref->{$replay}{$name}{'RESULT'} = $result;
				$sumref->{$replay}{$name}{'KILLSUM'} = $killsum;
				$sumref->{$replay}{$name}{'DURATION'} = $duration;
				$sumref->{$replay}{$name}{'GAMETIME'} = $gametime;		        
				$sumref->{$replay}{$name}{'PLAYERID'} = $playerid;
				$sumref->{$replay}{$name}{'INCOME'} = $income;
				$sumref->{$replay}{$name}{'ARMY'} = $army;
			}
        }
	}
	
}
