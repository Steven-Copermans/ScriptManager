using ScriptManager.Attributes;
using ScriptManager.Interfaces;
using Scripts.Scripts.B.Sub;

using ScriptOptions = ScriptManager.Attributes.ScriptAttribute;

namespace Scripts.Scripts.B
{
    [Script(name = "Script B", description = "This is a script for B", index = 1)]
    internal class ScriptB : IScript
    {
        public ScriptB()
        {
            AddSubScript(new DynamicSubScriptB("Jef"), new ScriptOptions { name = "JefScript" });
            AddSubScript(new DynamicSubScriptB("Karel"));
            AddSubScript(new DynamicSubScriptB(), new ScriptOptions { name = "EmptyScript" });
        }
    }
}
