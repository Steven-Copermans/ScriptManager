using ScriptManager.Attributes;
using ScriptManager.Interfaces;

namespace Scripts.Scripts.B.Sub
{
    [Script(name = "Sub B", description = "This is a static subscript for Script B", index = 0)]
    internal class StaticSubScriptB : IScript<ScriptB>
    {

    }
}
