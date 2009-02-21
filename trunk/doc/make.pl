
sub ret { exit $?>>8 }

system("xsltproc --param html.stylesheet \"'taps.css'\" --xinclude --output taps.html c:/docbook-xsl-ns-1.74.0/xhtml/docbook.xsl taps.xml") and ret;

0;
