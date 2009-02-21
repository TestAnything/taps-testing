# to run tests with Test-Harness 3.14
use TAP::Harness;
my @switches=grep {/^-/} @ARGV;
my @scripts=grep {/^[^-]/} @ARGV;
my $harness = TAP::Harness->new( {exec=>['tap.exe','-z',@switches],
                                  jobs=>4,
                                  #failures=>1
                                  verbosity=>1
                                 } );
$harness->runtests(map {glob} @scripts);
