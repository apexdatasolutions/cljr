using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.CommandLine;
using System.CommandLine.Parsing;

using cljr.Commands;

namespace cljr
{
  internal class Program
  {
    
    static void Main ( string [] args )
    {

      RootCommand root = new RootCommand ()
      {
        REPLCommand.Get ()
      , CompileCommand.Get ()
      , RunCommand.Get ()
      };
      root.Description = "An integrated command line build tool for Clojure on the CLR.";
      root.Name = "cljr";
      if ( args.Length > 0 )
      {
        root.Invoke ( args );
      }
      else
      {
        // make "cljr repl" the default command.
        List<string> altArgs = new List<string> ();
        altArgs.Add ( "repl" );
        root.Invoke ( altArgs.ToArray() );
      }

    }
  }
}
