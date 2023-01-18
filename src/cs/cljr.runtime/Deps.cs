using System;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Clojure = clojure.lang;
using clojure.clr.api;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.ComponentModel.Design.Serialization;
using System.Security.Permissions;
using System.Resources;
using System.IO;

namespace cljr.runtime
{
  public static class Deps
  {
    private static string _dirSep = Path.DirectorySeparatorChar.ToString();
    //private static bool _isInitialized = false;

    public static Dictionary<String,Object> NugetRepos = new Dictionary<String,Object>();
    public static List<String> SourcePaths = new List<String>();
    public static List<String> LocalDepsPaths = new List<String>();
    public static Dictionary<String,Object> Aliases = new Dictionary<String,Object>();  
    public static AssemblyName[] ReferencedAssemblies =
        Assembly.GetExecutingAssembly().GetReferencedAssemblies();

    public static Clojure.Keyword EOF = Clojure.Keyword.intern("eof");

    // toplevel keywords
    public static Clojure.Keyword PathsKeyword =
      Clojure.Keyword.intern("paths");
    public static Clojure.Keyword ClrDepsKeyword =
      Clojure.Keyword.intern("clr-deps");
    public static Clojure.Keyword ClrDepsPrepLibKeyword =
      Clojure.Keyword.intern("clr-deps/prep-lib");
    public static Clojure.Keyword ClrAliasesKeyword =
      Clojure.Keyword.intern("clr-aliases");
    public static Clojure.Keyword ClrToolsUsageKeyword =
      Clojure.Keyword.intern("clr-tools/usage");
    public static Clojure.Keyword NugetReposKeyword =
      Clojure.Keyword.intern("nuget/repos");
    public static Clojure.Keyword NugetLocalRepoKeyword =
      Clojure.Keyword.intern("nuget/local-repo");

    // aliases keywords
    public static Clojure.Keyword MainOptsKeyword =
      Clojure.Keyword.intern("main-opts");

    // deps keywords
    public static Clojure.Keyword LocalRootKeyword =
      Clojure.Keyword.intern("local/root");
    public static Clojure.Keyword GitUrlKeyword =
      Clojure.Keyword.intern("git/url");
    public static Clojure.Keyword GitTagKeyword =
      Clojure.Keyword.intern("git/tag");
    public static Clojure.Keyword GitShaKeyword =
      Clojure.Keyword.intern("git/sha");
    public static Clojure.Keyword NugetVersionKeyword =
      Clojure.Keyword.intern("nuget/version");
    public static Clojure.Keyword ExtraDepsKeyword =
      Clojure.Keyword.intern("extra-deps");
    public static Clojure.Keyword OverrideDepsKeyword =
      Clojure.Keyword.intern("override-deps");
    public static Clojure.Keyword DefaultDepsKeyword =
      Clojure.Keyword.intern("default-deps");
    public static Clojure.Keyword ReplaceDepsKeyword =
      Clojure.Keyword.intern("replace-deps");

    public static Clojure.Keyword AppDirKeyword =
      Clojure.Keyword.intern("cljr-appdir"); // built in alias for location of cljr binaries

    // nuget/repos keywords

    public static string AppDirPath = AppDomain.CurrentDomain.BaseDirectory;
    public static string DepsFileName = "deps.edn";

    private static void AddRepos(Clojure.PersistentArrayMap deps)
    {
      var rs = deps[NugetReposKeyword];
      if (null != rs)
      {
        if (rs is Clojure.APersistentMap aMapOfRepos)
        {
          foreach (var repo in aMapOfRepos)
          {
            string key = repo.Key.ToString();
            var value = repo.Value;
            NugetRepos.Add(key, value);  
          }
        }
      }
    }

    private static void AddAliases(Clojure.PersistentArrayMap deps)
    {
      var aliases = deps[ClrAliasesKeyword];
      if (null != aliases)
      {
        if (aliases is Clojure.APersistentMap aMapOfAliases)
        {
          foreach (var alias in aMapOfAliases)
          {
            string key = alias.Key.ToString();
            var value = alias.Value;
            Aliases.Add(key, value);
          }
        }
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deps"></param>
    private static void AddPaths(Clojure.PersistentArrayMap deps)
    {
      Clojure.PersistentVector ps = (Clojure.PersistentVector)deps[PathsKeyword];
      if (null != ps)
      {
        SourcePaths.Clear(); // to preserve clj semantics that require only the last deps.edn paths to be respected.
        foreach ( var path in ps )
        {
          if (path is String pathString)
          {
            SourcePaths.Add(pathString);
          }
          // else if path is a keyword (pointing presumably to another persistent vector or string)
          else if (path is Clojure.Keyword keyword)
          {
            string key = keyword.ToString();
            if (Aliases.ContainsKey(key))
            {
              object val = Aliases[key];
              if (val is string strVal)
              {
                if (Directory.Exists(strVal))
                {
                  SourcePaths.Add(strVal);
                }
              }
              else if (val is Clojure.PersistentVector vectorVal)
              {
                foreach (var item in vectorVal)
                {
                  if (item is string strPath)
                  {
                    if (Directory.Exists(strPath))
                    {
                      SourcePaths.Add(strPath);
                    }
                  }
                }
              }
            }
          }
        }
      }
    }

    private static void AddDependencies(Clojure.PersistentArrayMap deps)
    {
      var ds = deps[ClrDepsKeyword];
      if (null != ds)
      {
        string dsType = ds.GetType().Name;
        if (ds is Clojure.APersistentMap depsMap)
        {
          foreach ( var depKey in depsMap.Keys )
          {
            string rootDir = "";
            var depVal = depsMap[depKey];
            if (depVal is Clojure.PersistentArrayMap valMap)
            {
              if ( valMap.ContainsKey ( LocalRootKeyword ) )
              {
                var localVal = valMap[ LocalRootKeyword ];
                if (localVal is string path )
                {
                  rootDir = path;
                  string assemblyPath = rootDir + depKey + ".dll";
                  if (File.Exists(assemblyPath))
                  {
                    LocalDepsPaths.Add(assemblyPath);
                  }
                  else
                  {
                    Console.WriteLine("WARNING: Assembly not found: " + assemblyPath);
                  }
                }
                else if ( localVal is Clojure.Keyword keyword )
                {
                  if (keyword == AppDirKeyword)
                  {
                    rootDir = AppDirPath;
                    string assemblyPath = rootDir + depKey + ".dll";
                    if (File.Exists(assemblyPath))
                    {
                      LocalDepsPaths.Add(assemblyPath);
                    }
                    else
                    {
                      Console.WriteLine("WARNING: Assembly not found: " + assemblyPath);
                    }
                  }
                  else if ( Aliases.ContainsKey ( keyword.ToString() ) )
                  {
                    if (Aliases[keyword.ToString()] is String stringValue )
                    {
                      if (Directory.Exists (stringValue ) )
                      {
                        string assemblyPath = stringValue + depKey + ".dll";
                        if (File.Exists (assemblyPath) )
                        {
                          LocalDepsPaths.Add(assemblyPath);
                        }
                      }
                    }
                  }
                  // what else might it be?
                }
                
              }
              else if ( valMap.ContainsKey (GitTagKeyword) )
              {
                // todo: implement :git/url
              }
              else if ( valMap.ContainsKey (NugetVersionKeyword) ) 
              {
                // todo: implement :nuget/version
              }
              else
              {
                // unrecognized keyword warning?
              }
            }
            
          }
        }
      }
    }

    /// <summary>
    /// Evaluates a deps.edn source file (or source in text form, e.g., from
    /// embedded resources).
    /// </summary>
    /// <param name="filePathOrSource"></param>
    /// <returns></returns>
    public static bool EvaluateDepsFileOrSource(string filePathOrSource)
    {
      bool result = true;
      try
      {
        string ednText = File.Exists(filePathOrSource) ? System.IO.File.ReadAllText(filePathOrSource)
                                                          : filePathOrSource;
        if (!String.IsNullOrEmpty(ednText))
        {
          Clojure.PersistentHashMap opts =
            Clojure.PersistentHashMap.create(new object[] { EOF, null });

          Clojure.PersistentArrayMap deps =
            (Clojure.PersistentArrayMap)Clojure.EdnReader.readString(ednText, opts);

          if (deps.ContainsKey(NugetLocalRepoKeyword))
          {
            var localRepo = deps[NugetLocalRepoKeyword];
            if (localRepo != null)
            {
              if (localRepo is Clojure.APersistentMap aMapOfRepos)
              {
                //
              }
            }
          }

          // load repos
          if (deps.ContainsKey(NugetReposKeyword))
          {
            AddRepos(deps);
          }

          // load aliases
          if (deps.ContainsKey(ClrAliasesKeyword))
          {
            AddAliases(deps);
          }

          // load paths
          if (deps.ContainsKey(PathsKeyword))
          {
            AddPaths(deps);
          }

          // load dependencies
          if (deps.ContainsKey(ClrDepsKeyword))
          {
            AddDependencies(deps);
          }
        }

      }
      catch (Exception e)
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
    public static bool Check()
    {
      bool result = false;
      //AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblyHandler;

      // root deps file
      string rootDeps = AppDirPath + _dirSep + DepsFileName;
      if (File.Exists(rootDeps))
      {
        EvaluateDepsFileOrSource(rootDeps);
      }
      else
      {

        String resVal = cljr.runtime.Properties.Resources.deps_edn;
        if (!string.IsNullOrEmpty(resVal))
        {
          EvaluateDepsFileOrSource(resVal);
        }
      }
      // user deps file
      string cljConfig = Environment.GetEnvironmentVariable("CLJ_CONFIG");
      string xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
      string userDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
      string userDeps = userDirPath + _dirSep + ".clojure" + _dirSep + DepsFileName;
      bool userDepsFound = false;
      if (!string.IsNullOrEmpty(cljConfig))
      {
        string cljConfigFile = cljConfig + _dirSep + DepsFileName;
        if (File.Exists(cljConfigFile))
        {
          userDepsFound = true;
          EvaluateDepsFileOrSource(cljConfigFile);
        }
      }
      else if (!string.IsNullOrEmpty(xdgConfig))
      {
        string xdgConfigFile = xdgConfig + _dirSep + "clojure" + DepsFileName;
        if (File.Exists(xdgConfigFile))
        {
          userDepsFound = true;
          EvaluateDepsFileOrSource(xdgConfigFile);
        }
      }
      else if (File.Exists(userDeps) && !userDepsFound)
      {
        EvaluateDepsFileOrSource(userDeps);
      }

      // local deps file
      if (System.IO.File.Exists(DepsFileName))
      {
        result = true;
        EvaluateDepsFileOrSource(DepsFileName);
      }
      return result;
    }

    /// <summary>
    /// Loads required assemblies based on the source path.
    /// </summary>
    public static void LoadDeps()
    {
      // For now, load them where we find them. In the future, perhaps move them to a local
      // directory from which everything can be run.
      foreach (string path in LocalDepsPaths)
      {
        Console.WriteLine("Loading " + path);
        try
        {
          Assembly.LoadFrom(path);
        }
        catch (Exception ex)
        {
          try
          {
            if (path.StartsWith("System."))
            {
              string shortName = path.Replace(".dll", "");
              Assembly.Load(shortName);
            }
            else
            {
              Assembly.Load(path);
            }
          }
          catch (Exception ex1)
          {
            Console.WriteLine("WARNING: " + ex1.Message);
          }
        }
      }
      // TODO: load any locally compiled (i.e., in 'targets/assemblies'), git, or nuget-based assemblies

    }

  }
}