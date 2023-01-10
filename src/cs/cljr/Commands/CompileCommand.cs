using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;
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
      cljr.runtime.Main.Compile ( libs );
    }
  }
}
