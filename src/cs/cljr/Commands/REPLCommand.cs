using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.CommandLine;

using Clojure = clojure.lang;

namespace cljr.Commands
{
  internal static class REPLCommand
  {

    public static Command Get ()
    {
      Option <string []> argsOption
        = new Option<string []> ( "args", "The args to provide to the clojure main REPL." )
        {
          IsRequired = false
        , AllowMultipleArgumentsPerToken = true
        , Arity = ArgumentArity.ZeroOrMore
        };
      Command replCommand
        = new Command ( "repl", "Fire up an instance of the Clojure command line REPL." )
        {
          argsOption
        };
      replCommand.SetHandler<string []> ( HandleREPLRequest, argsOption );
      return replCommand;
    }

    public static void HandleREPLRequest ( string [] args )
    {
      Clojure.Symbol CLOJURE_MAIN = Clojure.Symbol.intern( "clojure.main" );
      Clojure.Var REQUIRE = Clojure.RT.var( "clojure.core", "require" );
      Clojure.Var MAIN = Clojure.RT.var( "clojure.main", "main" );
      Clojure.RT.Init ();
      REQUIRE.invoke ( CLOJURE_MAIN );
      MAIN.applyTo ( Clojure.RT.seq ( args ) );
    }
  }
}
