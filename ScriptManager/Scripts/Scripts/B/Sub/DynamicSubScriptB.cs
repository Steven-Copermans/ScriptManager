using ScriptManager.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ScriptManager.Attributes;

namespace Scripts.Scripts.B.Sub
{
    internal class DynamicSubScriptB : IScript<ScriptB>
    {
        private string Name { get; set; }
        public DynamicSubScriptB(string? name = null)
        {
            Name = "UNDEFINED";
            if (name != null)
            {
                Name = name;
            }
        }

        public override void Run()
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Running a custom script for PrintScriptSub :D");
            Console.WriteLine(Name);
            Console.ResetColor();
        }
    }
}
