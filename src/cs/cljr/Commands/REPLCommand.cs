using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.CommandLine;

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
      cljr.runtime.Main.REPL ( args );
    }
  }
}
