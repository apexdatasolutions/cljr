using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using clojure.lang;
using System.Reflection.Metadata;

namespace cljr.runtime
{
  /// <summary>
  /// Helper class for invoking Clojure code on the fly.
  /// </summary>
  public static class Source
  {
    public static Var REQUIRE = var ( "require" );
    public static Var META = var ( "meta" );
    public static Var EVAL = var ( "eval" );
    public static Var READ_STRING = var ( "read-string" );

    public static Object Require ( string ns )
    {
      return REQUIRE.invoke ( Symbol.intern ( ns ) );
    }

    public static Object ReadString ( string str )
    {
      return READ_STRING.invoke ( str );
    }

    public static Object Eval ( string code )
    {
      return EVAL.invoke ( ReadString ( code ) );
    }

    public static Var var ( string varName )
    {
      return var ( "clojure.core", varName );
    }

    public static Var var ( string ns, string varName )
    {
      return RT.var( ns, varName );
    }

    public static Object EvalAsClojure ( this string src )
    {
      return Eval ( src );
    }
  }
}
