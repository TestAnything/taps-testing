Taps is a test tool for the .NET framework and Mono. It is inspired on Perl's testing facilities and therefore quite different from the likes of tools like NUnit. Taps runs test scripts and expects to see output that conforms to the TAP protocol on their stdouts. Test scripts use the TAP class to run tests that generate the expected output. Here is a minimal test script:

```
using Taps;

class HelloTest: TAP  {

  static void Main() {
    Ok(true,"Hello, world");
  }

}
```

The list below lists some features of Taps.

  * Perl Test::More-like function vocabulary.

  * Runs a single test script, multiple test scripts or a directory tree of test scripts.

  * Can show per-test timings.

  * Outputs diagnostic info of failed tests in human readable, yaml or "visual studio error list" compatible format.

  * Can run multiple test scripts concurrently.

  * Allows a test script to run tests in multiple threads.

  * Does deep comparison of complex data structures and if they are not equal outputs them annotated with a clear path to the differing item.

  * Supports testing of classes and methods that have the internal access modifier.

  * Output readable by TAP consumers.

You can find installation instructions in the [README](http://code.google.com/p/taps-testing/source/browse/trunk/README#).

The documentation is included in the downloads, but you can also read it online [here](http://www.fwvdijk.org/taps/taps.html).

To contact the project owner directly add an _@gmail.com_ to his username.