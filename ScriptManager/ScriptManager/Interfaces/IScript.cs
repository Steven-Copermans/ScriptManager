using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ScriptManager.Attributes;
using ScriptManager.Models;
using System.Reflection.Metadata.Ecma335;

namespace ScriptManager.Interfaces
{
    public abstract class IScript<T> : IScript where T : IScript
    {
        public Type Parent { get => typeof(T); }
    }

    public abstract class IScript
    {
        public IEnumerable<Script> SubScripts { get; set; } = Enumerable.Empty<Script>();
        public virtual void Run()
        {
            //Console.Clear();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Running default (IScript) implementation of Run() - Override if needed");
            Console.ResetColor();
        }

        public void RunAll()
        {
            Console.Clear();
            Console.WriteLine($"Running all subscripts: {SubScripts.Count()}");
            foreach (var script in SubScripts)
            {
                script.Instance?.Run();
            }
        }

        public bool AddSubScript(Type subScriptType, params object[] args)
        {
            throw new NotImplementedException();
            return true;
        }

        public bool AddSubScript(IScript subScript)
        {
            return AddSubScript(subScript, new ScriptAttribute
            {
                name = null,
                description = null,
                index = -1,
                version = 1.0
            });
        }
        public bool AddSubScript(IScript subScript, ScriptAttribute scriptAttribute)
        {
            try
            {
                Script script = new Script()
                {
                    name = scriptAttribute.name,
                    index = scriptAttribute.index,
                    description = scriptAttribute.description,
                    type = subScript.GetType(),
                    Instance = subScript
                };

                SubScripts = SubScripts.Append(script);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool AddSubScript(Script subScript)
        {
            try
            {
                SubScripts = SubScripts.Append(subScript);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
