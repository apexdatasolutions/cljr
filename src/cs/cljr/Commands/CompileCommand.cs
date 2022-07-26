using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using Clojure = clojure.lang;
using System.IO;
using System.Diagnostics;

namespace cljr.Commands
{
  internal static class CompileCommand
  {
    public static Command Get ()
    {
      Argument <string []> namespacesArgument
        = new Argument<string[]> ( "nss", "One or more namespaces to be compiled." )
        {
          Arity = ArgumentArity.ZeroOrMore
        };
      Command compileCommand
        = new Command ( "compile" , "Compile one or more namespaces.")
        {
          namespacesArgument
        };
      compileCommand.SetHandler<string []> ( HandleCompileRequest, namespacesArgument );
      return compileCommand;
    }

    public static void HandleCompileRequest ( string [] libs )
    {
#if NET6_0
      Console.WriteLine ( "Compiling is not supported on .NET 6.0 yet." );
#else
      const string PATH_PROP = "CLOJURE_COMPILE_PATH";
      const string REFLECTION_WARNING_PROP = "CLOJURE_COMPILE_WARN_ON_REFLECTION";
      const string UNCHECKED_MATH_PROP = "CLOJURE_COMPILE_UNCHECKED_MATH";

      Clojure.RT.Init ();

      TextWriter outTW = (TextWriter)Clojure.RT.OutVar.deref();
      TextWriter errTW = Clojure.RT.errPrintWriter();

      string path = Environment.GetEnvironmentVariable(PATH_PROP);

      path = path ?? ".";

      string warnVal =  Environment.GetEnvironmentVariable(REFLECTION_WARNING_PROP);
      bool warnOnReflection = warnVal == null ? false : warnVal.Equals("true");
      string mathVal = Environment.GetEnvironmentVariable(UNCHECKED_MATH_PROP);
      object uncheckedMath = false;

      if ( "true".Equals ( mathVal ) )
        uncheckedMath = true;
      else if ( "warn-on-boxed".Equals ( mathVal ) )
        uncheckedMath = Clojure.Keyword.intern ( "warn-on-boxed" );


      // Force load to avoid transitive compilation during lazy load
      Clojure.Compiler.EnsureMacroCheck ();

      try
      {
        Clojure.Var.pushThreadBindings ( Clojure.RT.map (
            Clojure.Compiler.CompilePathVar, path,
            Clojure.RT.WarnOnReflectionVar, warnOnReflection,
            Clojure.RT.UncheckedMathVar, uncheckedMath
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
            Clojure.Compiler.CompileVar.invoke ( Clojure.Symbol.intern ( lib ) );
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
        Environment.Exit ( 1 );
      }
      finally
      {
        Clojure.Var.popThreadBindings ();
        try
        {
          outTW.Flush ();
        }
        catch ( IOException e )
        {
          errTW.WriteLine ( e.StackTrace );
        }
      }

#endif 


    }
  }
}
