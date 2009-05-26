
$mono=1 if grep {/^mono$/} @ARGV;

sub ret { exit $?>>8 }

$xslpath='c:/docbook-xsl-ns-1.74.0/xhtml/docbook.xsl';
$xslpath='/opt/local/share/xsl/docbook-xsl/xhtml/docbook.xsl' if $mono;

system("xsltproc --param html.stylesheet \"'taps.css'\" --xinclude --output taps.html $xslpath taps.xml") and ret;

0;
