using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.CommandLine;

using CljLang = clojure.lang;
using clojure.clr.api;

namespace cljr.Commands
{
  internal static class REPLCommand
  {

    public static Command Get ()
    {
      Argument <string []> argsArgument
        = new Argument<string []> ( "args", "The args to provide to the clojure main REPL." )
        {
        //  IsRequired = false
        //, AllowMultipleArgumentsPerToken = true
          Arity = ArgumentArity.ZeroOrMore
        };
      Command replCommand
        = new Command ( "repl", "Fire up an instance of the Clojure command line REPL." )
        {
          argsArgument
        };
      replCommand.SetHandler<string []> ( HandleREPLRequest, argsArgument );
      return replCommand;
    }

    public static void HandleREPLRequest ( string [] args )
    {
      CljLang.Symbol CLOJURE_MAIN = CljLang.Symbol.intern( "clojure.main" );
      CljLang.Var REQUIRE = CljLang.RT.var( "clojure.core", "require" );
      CljLang.Var MAIN = CljLang.RT.var( "clojure.main", "main" );
      CljLang.RT.Init ();
      REQUIRE.invoke ( CLOJURE_MAIN );
      MAIN.applyTo ( CljLang.RT.seq ( args ) );
    }
  }
}
