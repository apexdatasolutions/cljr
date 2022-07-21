using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
using System.IO;
using Clojure = clojure.lang;

namespace cljr.Commands
{
  internal static class RunCommand
  {
    public static Command Get ()
    {
      Argument<string> binaryArgument = new Argument<string> (
        "binary",
        description: "The binary to run.")
      {
        Arity = ArgumentArity.ExactlyOne
      };
      Argument<string> entryPointArgument = new Argument<string> (
        "entry-point",
        description: "Pointer to a static class that contains a static main method.")
      {
        Arity = ArgumentArity.ExactlyOne
      };

      Argument < string []> argsArgument = new Argument<string[]>
        ("args", "The arguments to pass to the program.")
      {
        Arity = ArgumentArity.ZeroOrMore
      };

      Command runCommand
        = new Command ( "run", "Run a program from a main entry point.")
        {
          binaryArgument
        , entryPointArgument
        , argsArgument
        };
      runCommand.SetHandler<string, string, string []> 
        (HandleRunCommand, binaryArgument, entryPointArgument, argsArgument );
      return runCommand;
    }

    public static void HandleRunCommand ( string binary, string entryPoint, string [] args )
    {
      Console.WriteLine ( "Binary: " + binary );
      Console.WriteLine ( "Entry Point: " + entryPoint );
      Console.WriteLine ( "Arguments: " );
      foreach ( string arg in args )
      {
        Console.WriteLine ( "  " + arg );
      }
      Console.WriteLine ( "I would run your program, if only I knew how!" );
    }
  }
}
