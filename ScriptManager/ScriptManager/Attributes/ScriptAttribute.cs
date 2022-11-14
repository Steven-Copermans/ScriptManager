namespace ScriptManager.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter)]
    public class ScriptAttribute : Attribute
    {
        public string? name;
        public string? description;
        public int index = -1;
        public double version = 1.0;
    }
}
