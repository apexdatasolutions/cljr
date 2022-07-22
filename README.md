# cljr

A build tool for Clojure on the CLR that plays nice with .NET tooling
but remains familiar if not outright friendly to mainline Clojurians.

That is, it attempts to behave compatibly with mainline Clojure's
Deps/CLI tooling, while at the same time working with .NET tooling
behind the schenes to manage the build process and loading dependencies
more or less ``the .NET way.''

As a bridge betweent these two worlds, cljr hopes to make Clojure a
first-class, highly productive alternative to other languages on the CLR
while remaining inviting to mainline Clojure developers as well, for 
whom .NET may be _terra incognita_.
