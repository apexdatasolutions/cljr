using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using clojure.clr.api;
using CljLang = clojure.lang;
using System.IO;
using System.Diagnostics;

namespace cljr.runtime
{
  public static class Main
  {
    // assuming we're working from the root where deps.edn is...
    public static string DefaultBinaryPath = 
      Directory.GetCurrentDirectory() + "\\target\\assemblies";

    public static void Run ( string entryPoint, string [] args )
    {
      CljLang.Symbol CLOJURE_MAIN = CljLang.Symbol.intern( "clojure.main" );
      CljLang.Var REQUIRE = CljLang.RT.var( "clojure.core", "require" );
      CljLang.Var MAIN = CljLang.RT.var( "clojure.main", "main" );
      CljLang.RT.Init ();
      REQUIRE.invoke ( CLOJURE_MAIN );
      if ( Deps.Check() )
      {
        Deps.LoadDeps();
      }
      List<String> actualArgs = new List<String> ();
      actualArgs.Add ( "-m" );
      actualArgs.Add ( entryPoint );
      actualArgs.AddRange ( args );
      MAIN.applyTo ( CljLang.RT.seq ( actualArgs.ToArray () ) );
    }

    public static void REPL ( string [] args )
    {
      CljLang.Symbol CLOJURE_MAIN = CljLang.Symbol.intern( "clojure.main" );
      CljLang.Var REQUIRE = CljLang.RT.var( "clojure.core", "require" );
      CljLang.Var MAIN = CljLang.RT.var( "clojure.main", "main" );
    restart:
      try
      {
        CljLang.RT.Init ();
        if (Deps.Check())
        {
          Deps.LoadDeps();
        }
        REQUIRE.invoke ( CLOJURE_MAIN );
        MAIN.applyTo ( CljLang.RT.seq ( args ) );
      }
      catch ( Exception ex )
      {
        Console.WriteLine ( ex.ToString () );
        Console.WriteLine ( "Restarting the REPL..." );
        goto restart;
      }
    }

    public static void Compile ( string [] libs )
    {
#if NET5_0_OR_GREATER
      Console.WriteLine ( "Compiling is not supported on .NET 5.0 or greater ...yet." );
#else
      const string PATH_PROP = "CLOJURE_COMPILE_PATH";
      const string REFLECTION_WARNING_PROP = "CLOJURE_COMPILE_WARN_ON_REFLECTION";
      const string UNCHECKED_MATH_PROP = "CLOJURE_COMPILE_UNCHECKED_MATH";

      CljLang.RT.Init ();

      TextWriter outTW = (TextWriter)CljLang.RT.OutVar.deref();
      TextWriter errTW = CljLang.RT.errPrintWriter();

      string path = Environment.GetEnvironmentVariable(PATH_PROP);

      path = path ?? DefaultBinaryPath;

      string warnVal =  Environment.GetEnvironmentVariable(REFLECTION_WARNING_PROP);
      bool warnOnReflection = warnVal == null ? false : warnVal.Equals("true");
      string mathVal = Environment.GetEnvironmentVariable(UNCHECKED_MATH_PROP);
      object uncheckedMath = false;

      if ( "true".Equals ( mathVal ) )
        uncheckedMath = true;
      else if ( "warn-on-boxed".Equals ( mathVal ) )
        uncheckedMath = CljLang.Keyword.intern ( "warn-on-boxed" );


      // Force load to avoid transitive compilation during lazy load
      CljLang.Compiler.EnsureMacroCheck ();

      try
      {
        CljLang.Var.pushThreadBindings ( CljLang.RT.map (
            CljLang.Compiler.CompilePathVar, path,
            CljLang.RT.WarnOnReflectionVar, warnOnReflection,
            CljLang.RT.UncheckedMathVar, uncheckedMath
            ) );

        Stopwatch sw = new Stopwatch();

        if ( libs.Length > 0 )
        {
          foreach ( string lib in libs )
          {
            sw.Reset ();
            sw.Start ();
            outTW.Write ( "Compiling {0} to {1}", lib, path );
            outTW.Flush ();
            CljLang.Compiler.CompileVar.invoke ( CljLang.Symbol.intern ( lib ) );
            sw.Stop ();
            outTW.WriteLine ( " -- {0} milliseconds.", sw.ElapsedMilliseconds );
          }
        }
        else
        {
          Console.WriteLine ( "ERROR: No input provided." );
          // TODO: consult ndeps.edn file if it exists in the current
          // working directory.
        }
      }
      catch ( Exception e )
      {
        errTW.WriteLine ( e.ToString () );
        errTW.Flush ();
        Environment.Exit ( 1 );
      }
      finally
      {
        CljLang.Var.popThreadBindings ();
        try
        {
          outTW.Flush ();
        }
        catch ( IOException e )
        {
          errTW.WriteLine ( e.StackTrace );
          errTW.Flush ();
        }
      }

#endif 


    }
  }
}
