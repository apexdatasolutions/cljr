using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using clojure.clr.api;
using CljLang = clojure.lang;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace cljr.runtime
{
  public static class Main
  {
    // assuming we're working from the root where deps.edn is...
    public static string DefaultBinaryPath = 
      Directory.GetCurrentDirectory() + "\\target\\assemblies\\";
    public static string LOAD_PROP = "CLOJURE_LOAD_PATH";

    public static string LoadPath = Environment.GetEnvironmentVariable(LOAD_PROP);

    public static void SetClojureLoadPath ()
    {
      if (LoadPath == null)
      {
        LoadPath = "";
      }
      LoadPath = Deps.AddLocalLoadPaths(LoadPath);
      if (LoadPath.Length > 0)
      {
        Environment.SetEnvironmentVariable(LOAD_PROP, LoadPath);
      }
    }

    public static void Run ( string entryPoint, string [] args )
    {
      string originalDirectory = Directory.GetCurrentDirectory();

      if (Deps.Check())
      {
        Deps.LoadDeps();
      }
      SetClojureLoadPath();

      CljLang.Symbol CLOJURE_MAIN = CljLang.Symbol.intern( "clojure.main" );
      CljLang.Var REQUIRE = CljLang.RT.var( "clojure.core", "require" );
      CljLang.Var MAIN = CljLang.RT.var( "clojure.main", "main" );
      CljLang.RT.Init ();
      REQUIRE.invoke ( CLOJURE_MAIN );
      
      List<String> actualArgs = new List<String> ();
      actualArgs.Add ( "-m" );
      actualArgs.Add ( entryPoint );
      actualArgs.AddRange ( args );
      MAIN.applyTo ( CljLang.RT.seq ( actualArgs.ToArray () ) );

      Directory.SetCurrentDirectory(originalDirectory);
    }

    public static void REPL ( string [] args )
    {
      string originalDirectory = Directory.GetCurrentDirectory();

      if (Deps.Check())
      {
        Deps.LoadDeps();
      }
      SetClojureLoadPath();
      CljLang.Symbol CLOJURE_MAIN = CljLang.Symbol.intern( "clojure.main" );
      CljLang.Var REQUIRE = CljLang.RT.var( "clojure.core", "require" );
      CljLang.Var MAIN = CljLang.RT.var( "clojure.main", "main" );
    restart:
      try
      {
        CljLang.RT.Init ();
        
        REQUIRE.invoke ( CLOJURE_MAIN );
        MAIN.applyTo ( CljLang.RT.seq ( args ) );
      }
      catch ( Exception ex )
      {
        Console.WriteLine ( ex.ToString () );
        Console.WriteLine ( "Restarting the REPL..." );
        goto restart;
      }
      Directory.SetCurrentDirectory (originalDirectory);
    }

    public static void Compile ( string [] libs )
    {
#if NET5_0_OR_GREATER
      Console.WriteLine ( "Compiling is not supported on .NET 5.0 or greater ...yet." );
#else
      if ( !Directory.Exists (DefaultBinaryPath) )
      {
        Directory.CreateDirectory(DefaultBinaryPath);
      }
      string originalDirectory = Directory.GetCurrentDirectory ();

      
      const string PATH_PROP = "CLOJURE_COMPILE_PATH";
      const string REFLECTION_WARNING_PROP = "CLOJURE_COMPILE_WARN_ON_REFLECTION";
      const string UNCHECKED_MATH_PROP = "CLOJURE_COMPILE_UNCHECKED_MATH";

      if (Deps.Check())
      {
        Deps.LoadDeps();
      }
      SetClojureLoadPath();

      CljLang.RT.Init ();

      TextWriter outTW = (TextWriter)CljLang.RT.OutVar.deref();
      TextWriter errTW = CljLang.RT.errPrintWriter();

      string compilePath = Environment.GetEnvironmentVariable(PATH_PROP);

      compilePath = compilePath ?? DefaultBinaryPath;

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
            CljLang.Compiler.CompilePathVar, compilePath,
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
            //TODO: resolve the full path of the lib after frisking source paths and
            // set current directory to the root of the first matching source path
            outTW.Write ( "Compiling {0} to {1}", lib, compilePath );
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
      Directory.SetCurrentDirectory(originalDirectory);

#endif 


    }
  }
}
