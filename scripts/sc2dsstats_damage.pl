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

# damage value (maybe vs race depenedt);


#my $csv = "D:/github/sc2dsstats_prod/stats.csv";
#my $png = "D:/github/sc2dsstats_prod/dpv.png";

my $DEBUG = 0;

my $main_path = dirname($0);
$main_path = dirname($main_path);
my $csv = $main_path . "/stats.csv";

$csv = "D:/github/sc2dsstats_debug/stats.csv" if $DEBUG > 1; 

my $config_file = $main_path . "/sc2dsstats.exe.Config";

$config_file = "D:/github/sc2dsstats_debug/sc2dsstats.exe.Config" if $DEBUG > 1;

my $cfg = XMLin($config_file);

my $player = $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'};
my $skip_normal = $cfg->{'appSettings'}{'add'}{'SKIP_NORMAL'}{'value'};

$skip_normal = 0;

my $png = $main_path . "/dpv.png";

my $interest;
my $opp_stats;
my $player_stats;
my $basedon = "army";
my $alignment = "horizontal";

my $start_date = 19700101000000;
#my $start_date = 20190101000000;
my $end_date = strftime("%Y%m%d%H%M%S", localtime());

if (defined $ARGV[0]) {
	$start_date = $ARGV[0];
}
if (defined $ARGV[1]) {
	$end_date = $ARGV[1];
}
if (defined $ARGV[2]) {
	$skip_normal = $ARGV[2];
}
if (defined $ARGV[3]) {
	$interest = $ARGV[3];
	#$png = $main_path . "/dpv_" . $interest . ".png";
	$opp_stats = 1;	
} if (defined $ARGV[4]) {
	$player_stats = $ARGV[4];	
} if (defined $ARGV[5]) {
	$basedon = $ARGV[5];	
}
if (defined $ARGV[6]) {
	$alignment = $ARGV[6];	
}

$end_date += 1000000;
my %sum;
my %global;

my @duration;

my @commanders = ('Abathur' , 'Alarak', 'Artanis', 'Dehaka', 'Fenix', 'Tychus', 'Horner', 'Karax', 'Kerrigan' ,'Raynor', 'Stukov', 'Swann', 'Nova', 'Vorazun', 'Zagara');



&ReadCSV($csv, \%sum);

my %dpv;
my %dpv_sum;
my %l_skip;
my %dmg;
my $leaver;
my $games = keys %sum;

foreach my $replay (keys %sum) {
	
	if ($replay =~ /Beta/) {
		print "Skipping $replay due to beta\n";
		next;
	}
	
	# Player count
	my $c = keys %{ $sum{$replay} };
	
	if ($c != 6) {
		next;	
	}
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

		if (defined $l_skip{$replay} && $l_skip{$replay}) {
			next;	
		}	


		if ($skip_normal) {
			if ($race2 eq "Zerg" || $race2 eq "Terran" || $race2 eq "Protoss") {
				print "Skipping stats for $replay due to skip_normal\n" if $DEBUG > 1;
				$l_skip{$replay} = 1;
				next;	
			}
		}
		
		if ($gametime < $start_date) {
			print "Skipping stats for $replay due to start_date: $start_date\n" if $DEBUG > 1;
			$l_skip{$replay} = 1;
			next;
		}
			
		if ($gametime > $end_date) {
			print "Skipping stats for $replay due to end_date: $end_date\n" if $DEBUG > 1;
			$l_skip{$replay} = 1;
			next;
		}

		if ($sum{$replay}{$player}{'DURATION'} < 5376) {
			print "Skipping $replay due to duration\n";
			$l_skip{$replay} = 1;
			next;	
		}

		my $l_DURATION = $sum{$replay}{$player}{'DURATION'};
		
		foreach my $p (keys %{ $sum{$replay} }) {
			my $diff = $l_DURATION - $sum{$replay}{$p}{'DURATION'};
			
			if ($diff > 1344) {
				$leaver++;			
				$l_skip{$replay} = 1;
				print "Skipping $replay due to leaver\n";			
				last;
			}
		}	

		
		if (defined $l_skip{$replay} && $l_skip{$replay}) {
			next;	
		}	
		
		if (!$army || $army <  1500) {
			print "Skipping $replay due to no army\n";
			$l_skip{$replay} = 1;
			next;	
		}
		
		if (!$killsum || $killsum < 1500) {
			print "Skipping $replay due to nothing killed\n";
			$l_skip{$replay} = 1;
			next;	
		}

		if ($player_stats == 1) {
			if ($name ne $player) {
				next;
			}	
		}

		my $dpv;		
		if ($basedon eq "army") {
			$dpv = $killsum / $army;
			if ($dpv < 0.3 || $dpv > 3) {
				print "Skipping strange things: $replay => $race2 => $dpv\n";
			}
		} elsif ($basedon eq "income") {
			$dpv = $killsum / $income;
		} elsif ($basedon eq "time") {
			$dpv = $killsum / $sum{$replay}{$player}{'DURATION'};
		}
		
		
		
		#$dpv = sprintf("%.2f", $dpv);
		#$dpv{$replay}{$race2} = $dpv;
		$dmg{$name}{$race2} = $dpv;
	}
	
	if (!defined $l_skip{$replay}) {
		foreach my $p (keys %dmg) {
			foreach my $r (keys %{ $dmg{$p} }) {
				$dpv{$r}{'GAMES'} ++;
				if (defined $dpv{$r}{'VALUE'}) { 
					$dpv{$r}{'VALUE'} += $dmg{$p}{$r};
				} else {
					$dpv{$r}{'VALUE'} = $dmg{$p}{$r};	
				}
			}
		} 	
			
	}
	%dmg = ();
}



#my $leaver = keys %l_skip;

my $l_p = $leaver * 100 / $games;
$l_p = sprintf("%.2f", $l_p);
print "$leaver out or $games left the game for no reason ($l_p %)\n"; 

my @x;
my @y;
my $title;
my $y_label;

foreach my $r ( keys %dpv) {
	my $avg = $dpv{$r}{'VALUE'} / $dpv{$r}{'GAMES'};
	$avg = sprintf("%.4f", $avg);
	print $r . " <=> " . $avg . "\n";
	$dpv_sum{$r . " (" . $dpv{$r}{'GAMES'} . ")"} = $avg;
}
my $max = 0;
foreach (sort { $dpv_sum{$a} <=> $dpv_sum{$b} } keys %dpv_sum) {
	push(@x, $_);
	push(@y, $dpv_sum{$_});
	if ($dpv_sum{$_} > $max) {
		$max = $dpv_sum{$_};	
	}	
}

my $sd;
my $ed;
if ($start_date =~ /^(\d{8})/) {
	$sd = $1;
}

if ($end_date =~ /^(\d{8})/) {
	$ed = $1;	
}
my $add = "World";
if ($player_stats == 1) {
	$add = "Player";
}
if ($basedon eq "army") {
	$title = "DPV $add ($sd to $ed) ($l_p% games with leaver skiped)\n";
	$y_label = "DPV (KilledArmyValue / SpawnedArmyValue)";
} elsif ($basedon eq "income") {
	$title = "DPM $add ($sd to $ed) ($l_p% games with leaver skiped)\n";
	$y_label = "DPV (KilledArmyValue / MineralsCollected)";
} elsif ($basedon eq "time") {
	$title = "DPS $add ($sd to $ed) ($l_p% games with leaver skiped)\n";
	$y_label = "DPV (KilledArmyValue / gameduration)";
}



my $y_max_value = $max * 1.1;
$y_max_value = sprintf("%.1f", $y_max_value);

if ($alignment eq "horizontal") {
	&PrintGraph($title, "Commanders", $y_label, 1, $y_max_value, $png, \@x, \@y);
} elsif ($alignment eq "vertical") {
	&PrintHGraph($title, "Commanders", $y_label, 1, $y_max_value, $png, \@x, \@y);	
}	



sub PrintGraph {

my $title = shift;
my $x_label = shift;
my $y_label = shift;
my $x_tick_number = shift;
my $y_max_value = shift;
my $png = shift;
my $x = shift;
my $y = shift;


my $graph = GD::Graph::bars->new(1600, 600);
#my $graph = GD::Graph::hbars->new(600, 1600);
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
    x_labels_vertical => 1,
    
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
 
open(IMG, ">:unix", $png) or die $!;
binmode IMG;
print IMG $gd->png;
close(IMG);
	
	
}

sub PrintHGraph {

my $title = shift;
my $x_label = shift;
my $y_label = shift;
my $x_tick_number = shift;
my $y_max_value = shift;
my $png = shift;
my $x = shift;
my $y = shift;


#my $graph = GD::Graph::bars->new(1600, 600);
my $graph = GD::Graph::hbars->new(600, 1600);
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
    #x_labels_vertical => 1,
    
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
 
open(IMG, ">:unix", $png) or die $!;
binmode IMG;
print IMG $gd->png;
close(IMG);
	
	
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

