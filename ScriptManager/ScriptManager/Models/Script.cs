using ScriptManager.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptManager.Models
{
    public class Script
    {
        public int? index { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public Type type { get; set; } = typeof(IScript);
        public List<Script> subScripts { get; set; } = new();
        public IScript? Instance { get; set; }
    }
}
