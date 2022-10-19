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
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Running default (IScript) implementation of Run() - Override if needed");
            Console.ResetColor();
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
    }
}
