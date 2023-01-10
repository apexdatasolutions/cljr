using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.IO;
using System.Reflection;

namespace cljr.Commands
{
  internal static class RunCommand
  {
    public static Command Get ()
    {
      /*
      Argument<string> binaryArgument = new Argument<string> (
        "binary",
        description: "The binary to run.")
      {
        Arity = ArgumentArity.ExactlyOne
      };
      */
      Option<string> entryPointArgument = new Option<string> (
        "-m",
        description: "Namespace containing a '-main' entry point.")
      {
        Arity = ArgumentArity.ExactlyOne
      , IsRequired = true
      };

      Argument < string []> argsArgument = new Argument<string[]>
        ("args", "The arguments to pass to the program.")
      {
        Arity = ArgumentArity.ZeroOrMore
      };

      Command runCommand
        = new Command ( "run", "Run a program from a main entry point.")
        {
          entryPointArgument
        , argsArgument
        };
      runCommand.SetHandler<string, string []> 
        (HandleRunCommand, entryPointArgument, argsArgument );
      return runCommand;
    }

    public static void HandleRunCommand ( string entryPoint, string [] args )
    {
      cljr.runtime.Main.Run ( entryPoint, args );
    }
  }
}
