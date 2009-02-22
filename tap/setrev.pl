$ver=`svnversion`;
if($ver=~/^(?:\d+:)?(\d+)/) {
    $rev=$1;
    open $fh,"<version.cs";
    if(!grep {/\.$rev"\)]/} <$fh>) {
        close $fh;
        open $fh,">version.cs" or die "open: $!";
        $verattr=<<END;
using System.Reflection;
[assembly: AssemblyVersion("1.0.0.$rev")]
END
        print $fh $verattr;
        close $fh;
        print "wrote $verattr to version.cs\n";
    } else {
        print STDERR "version.cs up to date\n";
    }
} else {
    print STDERR "not setting rev due to lack of svn\n";
}
