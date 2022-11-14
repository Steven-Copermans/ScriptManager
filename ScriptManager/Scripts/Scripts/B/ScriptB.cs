using ScriptManager.Attributes;
using ScriptManager.Interfaces;
using Scripts.Scripts.B.Sub;

namespace Scripts.Scripts.B
{
    [Script(name = "Script B", description = "This is a script for B", index = 1)]
    internal class ScriptB : IScript
    {
        public ScriptB()
        {
            AddSubScript(new DynamicSubScriptB("Jef"), new() { name = "JefScript" });
            AddSubScript(new DynamicSubScriptB("Karel"));
            AddSubScript(new DynamicSubScriptB(), new ScriptAttribute { name = "EmptyScript" });
        }
    }
}
