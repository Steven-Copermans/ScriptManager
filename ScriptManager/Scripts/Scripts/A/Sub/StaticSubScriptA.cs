using ScriptManager.Attributes;
using ScriptManager.Interfaces;

namespace Scripts.Scripts.A.Sub
{
    [Script(name = "Sub A", description = "This is a static subscript for Script A", index = 0)]
    internal class StaticSubScriptA : IScript<ScriptA>
    {

    }
}
