# run as 'perl make.pl' (not as 'make.pl')
use File::Copy;

my $test=''; #'t/yamlwriter.cs';

sub ret { exit $?>>8 }

system('c:/WINDOWS/Microsoft.NET/Framework/v3.5/csc.exe /out:tap.exe /debug /target:exe '.join(' ',glob('*.cs'))) and ret;
system('tap.exe -s:u -v:1 -d:3 -p:4 '.$test) and ret;
copy('tap.exe',"$ENV{SystemRoot}\\system32") and copy('tap.pdb',"$ENV{SystemRoot}\\system32") or die "copy: $!";

0;
