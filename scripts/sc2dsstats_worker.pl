use strict;
use warnings;
use utf8;
use open IN => ":utf8";
use open OUT => ":utf8";
use XML::Simple;
use GD::Graph::bars;
use POSIX qw(strftime);
use File::Basename;

my $DEBUG = 0;

my $main_path = dirname($0);
$main_path = dirname($main_path);
my $csv = $main_path . "/stats.csv";

$csv = "C:/temp/sc2_ds_stats/stats.csv" if $DEBUG > 1; 

my $config_file = $main_path . "/sc2dsstats.exe.Config";

$config_file = "C:/temp/sc2_ds_stats/sc2dsstats.exe.Config" if $DEBUG > 1;

my $cfg = XMLin($config_file);

my $player = $cfg->{'appSettings'}{'add'}{'PLAYER'}{'value'};
my $skip_normal = $cfg->{'appSettings'}{'add'}{'SKIP_NORMAL'}{'value'};

$skip_normal = 0;

my $png = $main_path . "/otf.png";

my $start_date = 19700101000000;
#my $start_date = 20190101000000;
my $end_date = strftime("%Y%m%d%H%M%S", localtime());

my %sum;
my %global;

my @duration;

my @commanders = ('Abathur' , 'Alarak', 'Artanis', 'Dehaka', 'Fenix', 'Tychus', 'Horner', 'Karax', 'Kerrigan' ,'Raynor', 'Stukov', 'Swann', 'Nova', 'Vorazun', 'Zagara');

my @opp = (0, 4, 5, 6, 1, 2, 3);
my %opp; 
my $opp_stats = 0;

my $interest = "Raynor";
my %interest;

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
	$png = $main_path . "/dt.png";
	$opp_stats = 1;	
}

print $player . "\n";
print $skip_normal . "\n";

&ReadCSV($csv, \%sum);

foreach my $replay (keys %sum) {
	
	# Player count
	my $c = keys %{ $sum{$replay} };
	
	if ($c != 6) {
		next;	
	}
	
	foreach my $name (keys %{ $sum{$replay} }) {
		
		my $race2 = $sum{$replay}{$name}{'RACE'};
		my $team = $sum{$replay}{$name}{'TEAM'};
		my $win = $sum{$replay}{$name}{'RESULT'};
		my $killsum = $sum{$replay}{$name}{'KILLSUM'};
		my $duration = $sum{$replay}{$name}{'DURATION'};
		my $gametime = $sum{$replay}{$name}{'GAMETIME'};
		my $id = $sum{$replay}{$name}{'PLAYERID'};
	
		#print $name . "\n";
	
		if ($name eq $player) {

			my $d_skip = 0;
	
			if ($skip_normal) {
				if ($race2 eq "Zerg" || $race2 eq "Terran" || $race2 eq "Protoss") {
					$d_skip = 1;
					print "Skipping stats for $replay due to skip_normal\n" if $DEBUG > 1;	
				}
				
			}
			
	
			if (defined $cfg->{'appSettings'}{'add'}{'SKIP'}{'value'} && $cfg->{'appSettings'}{'add'}{'SKIP'}{'value'}) {
				my $d_min = $duration / 24.4;
				if ($d_min < $cfg->{'appSettings'}{'add'}{'SKIP'}{'value'}) {
					$d_skip = 1;
					print "Skipping stats for $replay due to duration ($d_min)\n" if $DEBUG > 1;
				}
			} 
			
			if ($gametime < $start_date) {
				$d_skip = 1;
				print "Skipping stats for $replay due to start_date: $start_date\n" if $DEBUG > 1;
			}
			
			if ($gametime > $end_date) {
				$d_skip = 1;
				print "Skipping stats for $replay due to end_date: $end_date\n" if $DEBUG > 1;
				
			}
			
			if (! $d_skip) {
				
				if ($opp_stats) {
					if ($race2 eq $interest) {
						push(@duration, $duration);
					}	
				} else {
					push(@duration, $duration);
				}
				
				if ($race2 eq "Zerg" || $race2 eq "Terran" || $race2 eq "Protoss") {
					$global{'STD'}{'GAMES'}++;
				} else {	
					$global{'CMDR'}{'GAMES'}++;
				}
				$global{$race2}{'GAMES'}++;

				if ($opp_stats) {
					foreach my $oname (keys %{ $sum{$replay} }) {
						if ($sum{$replay}{$oname}{'PLAYERID'} == $opp[$id]) {
							$opp{$race2 . " vs " . $sum{$replay}{$oname}{'RACE'}}{'GAMES'} ++;								
						}
					}
				}

				if ($win == 1) {
					if ($race2 eq "Zerg" || $race2 eq "Terran" || $race2 eq "Protoss") {
						$global{'STD'}{'WIN'}++;
					} else {
						$global{'CMDR'}{'WIN'} ++;
					}
					$global{$race2}{'WIN'} ++;
					
					if ($opp_stats) {
						foreach my $oname (keys %{ $sum{$replay} }) {
							if ($sum{$replay}{$oname}{'PLAYERID'} == $opp[$id]) {
								$opp{$race2 . " vs " . $sum{$replay}{$oname}{'RACE'}}{'WIN'} ++;								
							}
						}
					}
						
				}

				if ($race2 eq $interest) {
					if (!defined $global{$race2}{'WIN'}) {
						$global{$race2}{'WIN'} = 0;	
					}
					my $wr = $global{$race2}{'WIN'} * 100 / $global{$race2}{'GAMES'};
					$wr = sprintf("%.2f", $wr); 
					$interest{$gametime} = 	$wr;
				}
							

			}
		}
	}
}


my @x;
my @y;
my $title;

my $sd;
my $ed;
if ($start_date =~ /^(\d{8})/) {
	$sd = $1;
}

if ($end_date =~ /^(\d{8})/) {
	$ed = $1;	
}


	# Average duration
	# 
	
	my $d_sum;
	my $d_min = 100000000;
	my $d_max = 0;
	
	foreach my $d (@duration) {
		my $dm = $d / 24.4;
		$d_sum += $dm;
		
		if ($dm <= $d_min) {
			$d_min = $dm;	
		}
		
		if ($dm > $d_max) {
			$d_max = $dm;
		}
	}
	
	my $d_average = $d_sum / scalar(@duration) / 60;
	$d_average = sprintf("%.2f", $d_average);
	$d_max = $d_max / 60;
	$d_max = sprintf("%.2f", $d_max);
	$d_min = $d_min / 60;
	$d_min = sprintf("%.2f", $d_min);

# Total
#

if ($opp_stats == 0) { 

	my %winrate;
	foreach my $r (keys %global) {
		if (!defined $global{$r}{'WIN'}) {
				$global{$r}{'WIN'} = 0;	
		}
		my $wr = $global{$r}{'WIN'} * 100 / $global{$r}{'GAMES'};
		$wr = sprintf("%.2f", $wr);
		print $r . "(" . $global{$r}{'GAMES'} . ")" . " => " . $wr . "\n";
		$winrate{$r . " (" . $global{$r}{'GAMES'} . ")"} = $wr;	
	}
	

	foreach (sort {$winrate{$a} <=> $winrate{$b}} keys %winrate) {
		push(@x, $_);
		push(@y, $winrate{$_});
	}
	

	
	$title = "Winrate ($sd to $ed - gametime " . "\xC3\x98" . ": " .  $d_average . " min)";

} else {
	# Matchups
	#
	
	my %mu_wr;
	foreach my $mu (keys %opp) {
		if ($mu =~ /^$interest/) {
			if (!defined $opp{$mu}{'WIN'}) {
				$opp{$mu}{'WIN'} = 0;	
			}
			my $wr = $opp{$mu}{'WIN'} * 100 / $opp{$mu}{'GAMES'};
			$wr = sprintf("%.2f", $wr);
			print $mu . "(" . $opp{$mu}{'GAMES'} . ") => " . $wr . "\n";
			$mu_wr{$mu . "(" . $opp{$mu}{'GAMES'} . ")"} = $wr;
		}	
	}
	
	foreach (sort {$mu_wr{$a} <=> $mu_wr{$b}} keys %mu_wr) {
		push(@x, $_);
		push(@y, $mu_wr{$_});	
	}
	
	$title = $interest . " vs the world ($sd to $ed - gametime " . "\xC3\x98" . ": " .  $d_average . " min)";
}

# Interest
#

#my $i = 0;
#foreach (sort {$a <=> $b} keys %interest) {
#	$i ++;
#	if ($i == 10) {
#		push(@x, $_);
#		push(@y, $interest{$_});
#		$i = 0;
#	}
#	
#}




&PrintGraph($title, "Commanders", 1, $png, \@x, \@y);



sub PrintGraph {

my $title = shift;
my $x_label = shift;
my $x_tick_number = shift;
my $png = shift;
my $x = shift;
my $y = shift;


my $graph = GD::Graph::bars->new(1600, 600);
$graph->set(
    x_label             => $x_label . '(generated by https://github.com/ipax77/sc2dsstats)',
    y_label             => 'Winrate',
    title               => $title,
    
    # shadows
    bar_spacing     => 8,
    shadow_depth    => 4,
    shadowclr       => 'dred',
        
    y_max_value         => 110,
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
			
				$sumref->{$replay}{$name}{'RACE'} = $race2;
				$sumref->{$replay}{$name}{'TEAM'} = $team;
				$sumref->{$replay}{$name}{'RESULT'} = $result;
				$sumref->{$replay}{$name}{'KILLSUM'} = $killsum;
				$sumref->{$replay}{$name}{'DURATION'} = $duration;
				$sumref->{$replay}{$name}{'GAMETIME'} = $gametime;		        
				$sumref->{$replay}{$name}{'PLAYERID'} = $playerid;
			}
        }
	}
	
}