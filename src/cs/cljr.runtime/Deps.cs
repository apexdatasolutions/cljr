using System;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Clojure = clojure.lang;
using clojure.clr.api;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.ComponentModel.Design.Serialization;

namespace cljr.runtime
{
  public static class Deps
  {
    private static string _dirSep = Path.DirectorySeparatorChar.ToString();
    private static bool _isInitialized = false;

    public static List<String> SourcePaths = new List<String>();
    public static List<String> LocalDepsPaths = new List<String>();
    public static AssemblyName [] ReferencedAssemblies =
        Assembly.GetExecutingAssembly().GetReferencedAssemblies();

    public static Clojure.Keyword EOF = Clojure.Keyword.intern ("eof");

    // toplevel keywords
    public static Clojure.Keyword PathsKeyword = 
      Clojure.Keyword.intern ( "paths" );
    public static Clojure.Keyword ClrDepsKeyword = 
      Clojure.Keyword.intern ( "clr-deps" );
    public static Clojure.Keyword ClrAliasesKeyword = 
      Clojure.Keyword.intern ( "clr-aliases" );
    public static Clojure.Keyword NugetReposKeyword = 
      Clojure.Keyword.intern ( "nuget/repos" );

    public static Dictionary<String,Clojure.Keyword> toplevelKeywords = 
      new Dictionary<String,Clojure.Keyword> ()
    {
      { "paths", PathsKeyword }
    , { "clr-deps", ClrDepsKeyword }
    , { "clr-aliases", ClrAliasesKeyword }
    , { "nuget-repos", NugetReposKeyword }
    };

    // deps keywords
    public static Clojure.Keyword LocalRootKeyword = 
      Clojure.Keyword.intern ( "local/root" );
    public static Clojure.Keyword NugetVersionKeyword = 
      Clojure.Keyword.intern ( "nuget/version" );
    public static Clojure.Keyword AppDirKeyword = 
      Clojure.Keyword.intern ( "cljr-appdir" );

    public static string AppDirPath = AppDomain.CurrentDomain.BaseDirectory;
    public static string DepsFileName = "deps.edn";

    private static void AddRepos ( Clojure.PersistentArrayMap deps )
    {

    }

    private static void AddAliases ( Clojure.PersistentArrayMap deps )
    {

    }

    private static void AddPaths ( Clojure.PersistentArrayMap deps )
    {

    }

    private static void AddDependencies ( Clojure.PersistentArrayMap deps )
    {

    }

    public static bool EvaluateDepsFileOrSource ( string fileNameOrSource )
    {
      bool result = true;
      try
      {
        string ednText = File.Exists ( fileNameOrSource ) ? System.IO.File.ReadAllText ( fileNameOrSource ) 
                                                          : fileNameOrSource;
        if ( !String.IsNullOrEmpty ( ednText ) )
        {
          Clojure.PersistentHashMap opts =
            Clojure.PersistentHashMap.create ( new object[] { EOF, null} );

          Clojure.PersistentArrayMap deps =
            (Clojure.PersistentArrayMap) Clojure.EdnReader.readString ( ednText, opts );
          
          // load repos
          if ( deps.ContainsKey ( NugetReposKeyword ) )
          {
            AddRepos ( deps );
          }

          // load aliases
          if ( deps.ContainsKey ( ClrAliasesKeyword ) )
          {
            AddAliases( deps );
          }
          
          // load paths
          if ( deps.ContainsKey ( PathsKeyword ) )
          {
            AddPaths( deps );
          }

          // load dependencies
          if ( deps.ContainsKey ( ClrDepsKeyword ) )
          {
            AddDependencies( deps );
          }
        }
        
      }
      catch ( Exception e )
      {
        result = false;
      }
      return result;
    }

    /// <summary>
    /// Checks for the existence of deps.edn in the local directory. Also
    /// searches in the cljr-appdir directory and current user home config,
    /// per the behavior of the clj equivalent on the JVM.
    /// </summary>
    /// <returns>True if a local deps.edn was found, indicating the current
    /// directory is the root of a clojure project. Any root or user deps found
    /// will be silently loaded.</returns>
    public static bool Check ()
    {
      bool result = false;
      AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyHandler;

      // root deps file
      string rootDeps = AppDirPath + _dirSep + DepsFileName;
      if ( File.Exists ( rootDeps ) )
      {
        EvaluateDepsFileOrSource ( rootDeps );
      }
      // user deps file
      string? cljConfig = Environment.GetEnvironmentVariable ( "CLJ_CONFIG" );
      string? xdgConfig = Environment.GetEnvironmentVariable ( "XDG_CONFIG_HOME" );
      string userDirPath = Environment.GetFolderPath (Environment.SpecialFolder.UserProfile);
      string userDeps = userDirPath + _dirSep + ".clojure" + _dirSep + DepsFileName;
      bool userDepsFound = false;
      if ( !string.IsNullOrEmpty ( cljConfig ) )
      {
        string cljConfigFile = cljConfig + _dirSep + DepsFileName;
        if ( File.Exists ( cljConfigFile ) )
        {
          userDepsFound = true;
          EvaluateDepsFileOrSource ( cljConfigFile );
        }
      }
      else if ( !string.IsNullOrEmpty ( xdgConfig ) )
      {
        string xdgConfigFile = xdgConfig + _dirSep + "clojure" + DepsFileName;
        if ( File.Exists ( xdgConfigFile ) )
        {
          userDepsFound = true;
          EvaluateDepsFileOrSource ( xdgConfigFile );
        }
      }
      else if ( File.Exists ( userDeps ) && !userDepsFound )
      {
        EvaluateDepsFileOrSource ( userDeps );
      }

      // local deps file
      if ( System.IO.File.Exists ( DepsFileName ) )
      {
        result = true;
        EvaluateDepsFileOrSource( DepsFileName );
      }
      return result;
    }

    /// <summary>
    /// Loads required assemblies based on the source path.
    /// </summary>
    public static void Load ()
    { 
      AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyHandler;

    }

    /// <summary>
    /// Handler to help resolve assemblies.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    /// <returns></returns>
#if NET6_0_OR_GREATER
    public static Assembly? ResolveAssemblyHandler ( object? sender, ResolveEventArgs args )
#else
    public static Assembly ResolveAssemblyHandler (object sender, ResolveEventArgs args )
#endif
    {
      foreach ( AssemblyName referencedAssembly in ReferencedAssemblies )
      {
        if ( referencedAssembly.FullName.StartsWith ( args.Name ) )
        {
          return Assembly.Load ( referencedAssembly.FullName );
        }
      }
      string path = File.Exists (args.Name) ? Path.GetFullPath ( args.Name ) : String.Empty;
      string shortName = args.Name;
      
      if ( String.Empty == path )
      {
        return Assembly.Load ( shortName );
      }
      else
      {
        return Assembly.LoadFrom ( path );
      }
      
    }
  }
}