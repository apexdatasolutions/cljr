# cljr

A build tool for Clojure on the CLR that plays nice with .NET tooling but remains familiar if not outright friendly to mainline Clojurians.

That is, it attempts to behave in a manner compatible with  (though not identically to) mainline Clojure's Deps/CLI tooling, while at the same time working with .NET tooling behind the scenes to manage the build process and load dependencies more or less "the .NET way."

As a bridge between these two worlds, cljr hopes to make Clojure a first-class, highly productive alternative to other languages on the CLR while remaining inviting to mainline Clojure developers as well, for whom .NET may be _terra incognita_.

## What's in the box

Two projects: 

1. **cljr** - A command line tool that behaves similarly to `clj` but adhering more to .NET standards in terms of command line options.
2. **cljr.runtime.dll** - An embeddable library that does the heavy lifting, used by cljr.exe but useful to any .NET application that wants to compile or run Clojure code.

There are also two solutions - one for .NET Framework (Windows only), and one for .NET Core. Which leads us to...

## An important note about building cljr.exe and cljr.runtime.dll

There are two separate solutions, one for .NET framework, and one for .NET Core, in the `projects` directory. They both refer to the same source files in the `src` directory.

This is not a standard layout for .NET projects. But because the .NET Runtime is fragmented, and target-specific project file formats exist, this approach preserves the ability to give the binaries targeting both platforms the same name. It just means that they need to be compiled separately, not in the same solution. Obviously, the IDE makes laying things out like this harder. So when adding a new source file to the project, we:

1. Add it first to the .NET framework solution. Or in any case, to whichever one we happen to be working in at the moment.
2. Save it to the `src/cs/<project>/` directory, as appropriate.
3. Delete any existing copy of the file in the local project directory.
4. Add it to the "other" solution by modifying the appropriate project file directly to point to the relative path of the source file to be added. If you simply try to "add existing file" the IDE will copy it to the local project file directory.
5. Double check that both projects are using the relative path to point to the files.

For example, here is the section with the relevant references to the source files in `cljr.csproj` located at `projects/net6.0/cljr.Net60/cljr.csproj` at the time of this writing: 

```
  <ItemGroup>
    <Compile Include="..\..\..\..\src\cs\cljr\Program.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Commands\REPLCommand.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Commands\RunCommand.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Commands\CompileCommand.cs" />
  </ItemGroup>

```

and here it is in `cljr.csproj` located at `projects/netframework/cljr/cljr.csproj`:

```
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Properties\AssemblyInfo.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Program.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Commands\REPLCommand.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Commands\RunCommand.cs" />
    <Compile Include="..\..\..\..\src\cs\cljr\Commands\CompileCommand.cs" />
  </ItemGroup>
```

`cljr.runtime.dll` projects in both solutions follow the same pattern.

When the solutions are built, the output directories for the .NET Framework and .NET Core versions of `cljr.exe` can be put on the `PATH` in whatever order of preference one wishes for local testing purposes. Alternatively, separate command prompt shortcuts can be created referring to batch files that set the `PATH` to the appropriate directory, depending on whether one wants to run the .NET Framework or .NET Core versions. Obviously, this problem exists only on Windows (unless one is using Mono to run the .NET Framework version on non-Windows platforms, of course).

Anyway, given the non-conventional layout of both solutions, it seemed wise to document this approach upfront. Criticisms and suggestions for a more elegant structure are welcome. (Yes, we will change the name of `net6.0` folder and solution to `netcore` at some point. But we mean here that suggestions for a fundamentally better approach are also welcome.)

## Using cljr.exe

`cljr.exe` works like any well-behaved .NET development tool, with a command structure similar to other tools (e.g., `dotnet.exe`, `nuget.exe`, etc.).

```
PS C:\projects\dotnet\cljr> cljr --help
Description:
  An integrated command line build tool for Clojure on the CLR.

Usage:
  cljr [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  repl <args>    Fire up an instance of the Clojure command line REPL.
  compile <nss>  Compile one or more namespaces.
  run <args>     Run a program from a main entry point.

PS C:\projects\dotnet\cljr> 
```

Currently there are three commands:

1. **repl** - which does what you expect

```
PS C:\projects\dotnet\cljr> cljr repl  
Clojure 1.12.0-alpha5
user=> 
```

It also happens to be the default command if you type only `cljr` at the command line.

2. **compile** - which also does what you expect

```
PS C:\Users\bcalc\scripts> cljr compile utils.tstamp
Compiling utils.tstamp to C:\Users\Bob\scripts\target\assemblies\ -- 413 milliseconds.
PS C:\Users\Bob\scripts>
```

3. **run** - You get the idea...

```
PS C:\Users\bcalc\scripts> cljr run -m utils.hello-world
Hello, world!
PS C:\Users\Bob\scripts> 
```

## Using cljr.runtime.dll

If you intend to embed this functionality in your command line or GUI application, then you'll want a reference to this DLL appropriate to the version of .NET you're targeting. The surface APIs are simple:

REPL:

```csharp
    public static void HandleREPLRequest ( string [] args )
    {
      cljr.runtime.Main.REPL ( args );
    }
```

Compile:

```csharp
    public static void HandleCompileRequest ( string [] namespaces )
    {
      cljr.runtime.Main.Compile ( namespaces );
    }
```

Run:

```csharp
    public static void HandleRunCommand ( string entryPoint, string [] args )
    {
      cljr.runtime.Main.Run ( entryPoint, args );
    }
```

These entry points all call `Deps` under the hood to interpret a `deps.edn` file if one exists in the current working directory. The behavior here is presently coded in C# to solve the chicken-egg conundrum of not being able to port deps tooling until cljr.exe exists. More on that and other issues in the next section.

## Issues

1. cljr is presently tightly coupled with the latest version of Clojure CLR, which as of this writing is 1.12.0-alpha5. `cljr` does not yet support indicating a different version in deps.edn. Partly this is because we wish to coordinate with the clojure-clr project to work out a path to closer alignment to `clj` feature-completeness, and partly it was because that's the best version for what cljr is trying to do, and we don't yet have legacy support issues.
2. The fragmentation of .NET is a pain. We still need for our commercial projects support for .NET Framework but it's clear MS and many NuGet dependencies are moving toward .NET Core for the ultimate unification of the disparate runtimes. So things are a bit messy till the Big Convergence.
3. .NET Framework runs faster than .NET Core because compilation at present cannot be done on .NET Core. This is because since .NET Core 3.1 or so MS removed the ability to save a compiled module to hard disk, for some reason. This is rumored to be returning in .NET Core 8, but presently if you want fast start up, you have to run on .NET Framework on Windows (or via Mono on other platforms).
4. The current version only works with local assemblies on the harddrive via `:local/root`.
5. Aliases are presently only useful for local path references.
6. .NET has a problem the JVM world does not: namely, multiple divergent runtimes and versions. Clojure CLR needs to support reader conditionals specific to .NET that can be used in `*.cljr` files. Until then, you're stuck with the version of the runtime of the runtime set up in your environment (relative to the target edition). There are many ways to approach this problem, and any constructive thoughts toward that end are welcome in the discussions section.

## deps.edn customization for CLR usage

Coming soon!

## Contributing

Feel free to work on the following features (or others you find useful). Just be sure to submit PR requests if you want us to consider incorporating your changes.

1. ~~git support (in progress!)~~ (we are working on this one)
2. NuGet support
3. Shell tool integration via aliases
4. Continuous compilation/error reporting at the command line for live coding feedback

## License

```
   Copyright 2023. Apex Data Solutions, LLC.

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
```