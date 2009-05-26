use File::Path;
use File::Copy;
use File::Spec::Functions qw/splitpath/;
rmtree('mktartmp');
$res=mkpath(['mktartmp/bin','mktartmp/lib/taps-testing','mktartmp/share/doc/taps-testing']);
copy('../tap/tap.exe','mktartmp/lib/taps-testing') or die "can't copy tap: $!";
copy('../tap/tap.exe.mdb','mktartmp/lib/taps-testing') or die "can't copy mdb: $!";
open $fh,'>','mktartmp/bin/tap' or die "can't open tap script: $!";
print $fh <<END;
#!/bin/sh
exec /usr/bin/mono --debug \${0/bin\\/tap/lib\\/taps-testing\\/tap.exe}
END
close $fh;
chmod 0755,'mktartmp/bin/tap' or die "can't chmod tap script: $!";
for(qw{
../COPYING
../COPYING.EXCEPTION
../TODO
../README
../doc/taps.html
../doc/taps.css
}) {
  copy($_,'mktartmp/share/doc/taps-testing') or warn "can't copy doc file $_: $!";
}
mkpath('mktartmp/share/doc/taps-testing/samples/hello');
system('cp -p ../samples/hello/* mktartmp/share/doc/taps-testing/samples/hello')==0 or die "copy hello failed: $!";
mkpath('mktartmp/share/doc/taps-testing/samples/thread');
system('cp -p ../samples/thread/* mktartmp/share/doc/taps-testing/samples/thread')==0 or die "copy thread failed: $!";
for(`cd ../samples ; find simple -name '*.cs' -o -name '*.bat' -o -name '*.csproj'`) {
  chomp;
  mkpath('mktartmp/share/doc/taps-testing/samples/'.(splitpath($_))[1]);
  system("cp -p ../samples/$_ mktartmp/share/doc/taps-testing/samples/$_")==0 or die "tree copy $_ failed: $!";
}

system('cd mktartmp ; tar czf ../taps-bin-unix.tar.gz *');
