# run as 'perl make.pl' (not as 'make.pl')
use File::Copy;

$mono=1 if grep {/^mono$/} @ARGV;

$compiler='c:/WINDOWS/Microsoft.NET/Framework/v3.5/csc.exe';
$runner='';
$compiler='gmcs' if $mono;
$runner='mono --debug ' if $mono;

my $test=''; #'t/yamlwriter.cs';

sub ret { exit $?>>8 }

do 'setrev.pl';
die "setrev: $@" if $@;
system("$compiler /out:tap.exe /debug /target:exe ".join(' ',glob('*.cs'))) and ret;
system("$runner tap.exe -s:u -v:1 -d:3 -p:4 ".$test) and ret;
copy('tap.exe',"$ENV{SystemRoot}\\system32") and copy('tap.pdb',"$ENV{SystemRoot}\\system32") or die "copy: $!" if !$mono;

0;
