using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScriptManager.Attributes;
using ScriptManager.Extensions;
using ScriptManager.Interfaces;
using System.Reflection;
using Script = ScriptManager.Models.Script;

namespace ScriptManager.Services
{
    public class Manager : BackgroundService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _services;

        private static IEnumerable<Script>? scripts;
        public Manager(IHostApplicationLifetime lifetime, IServiceProvider services)
        {
            _lifetime = lifetime;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (scripts == null)
            {
                throw new Exception("Failed to fetch scripts - Did you run Manager.RegisterScripts(IServiceCollection services) ?");
            }

            bool shouldExit = false;
            while (!stoppingToken.IsCancellationRequested && !shouldExit)
            {
                Console.Clear();
                Console.WriteLine(string.Format("{0," + ((Console.WindowWidth / 2) + (ProgramTitle.Length / 2)) + "}", ProgramTitle));
                foreach(var (script, index) in scripts.WithIndex())
                {
                    Console.WriteLine($"[{index}]: {script.name}");
                }

                Console.WriteLine();
                Console.WriteLine("[A]: Run All");
                Console.WriteLine("[X]: Exit");

                Console.WriteLine();
                Console.Write("Select your option: ");

                string? input = Console.ReadLine();

                if (input == null)
                {
                    continue;
                }

                if (input.ToLower() == "x")
                {
                    shouldExit = true;
                }

                if (int.TryParse(input, out int id))
                {
                    Script? selectedScript = scripts.ElementAtOrDefault(id);
                    if (selectedScript == null)
                    {
                        continue;
                    }

                    IScript? scriptService = (IScript?)_services.GetService(selectedScript.type);

                    if (scriptService == null && selectedScript.Instance == null)
                    {
                        //TODO: Log service not found
                        continue;
                    }

                    if (scriptService != null)
                    {
                        selectedScript.subScripts.AddRange(scriptService.SubScripts);
                        selectedScript.subScripts = selectedScript.subScripts.Distinct().ToList();
                    }

                    if (selectedScript.subScripts.Any())
                    {
                        DrawMenu(selectedScript);
                    } else
                    {
                        Console.Clear();
                        if (scriptService != null)
                        {
                            scriptService.Run();
                        } else if (selectedScript.Instance != null)
                        {
                            selectedScript.Instance.Run();
                        }
                        
                        Console.ReadLine();
                    }
                }
            }
            Console.WriteLine("Worker: ExecuteAsync is terminating...");
            _lifetime.StopApplication();
        }

        public bool DrawMenu(Script? script)
        {
            Console.Clear();
            Console.WriteLine(string.Format("{0," + ((Console.WindowWidth / 2) + ((script?.name?.Length ?? ProgramTitle.Length) / 2)) + "}", script?.name ?? ProgramTitle));

            
            foreach (var (scr, index) in script?.subScripts.WithIndex() ?? scripts?.WithIndex() ?? Enumerable.Empty<Script>().WithIndex())
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"[{index}]");
                Console.ResetColor();

                if (scr.name == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.WriteLine($": {scr.name ?? "No name defined for script"}");

                Console.ResetColor();
            }

            Console.WriteLine();
            Console.WriteLine("[A]: Run All");
            Console.WriteLine("[B]: Back");

            Console.WriteLine();
            Console.Write("Select your option: ");

            string? input = Console.ReadLine();

            if (input == null)
            {
                return DrawMenu(script);
            }

            if (input.ToLower() == "b")
            {
                return true;
            }

            if (int.TryParse(input, out int id))
            {
                Script? selectedScript = script?.subScripts.ElementAtOrDefault(id);
                if (selectedScript == null)
                {
                    return DrawMenu(script);
                }

                IScript? scriptService = (IScript?)_services.GetService(selectedScript.type);

                if (scriptService == null && selectedScript.Instance == null)
                {
                    //TODO: Log service not found
                    return DrawMenu(script);
                }

                if (scriptService != null)
                {
                    selectedScript.subScripts.AddRange(scriptService.SubScripts);
                    selectedScript.subScripts = selectedScript.subScripts.Distinct().ToList();
                }

                if (selectedScript.subScripts.Any())
                {
                    DrawMenu(selectedScript);
                }
                else
                {
                    Console.Clear();
                    if (scriptService != null)
                    {
                        scriptService.Run();
                    }
                    else if (selectedScript.Instance != null)
                    {
                        selectedScript.Instance.Run();
                    }
                    Console.ReadLine();
                }

                return true;
            } else
            {
                return DrawMenu(script);
            }
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Worker: StartAsync called...");
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Worker: StopAsync called...");
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            Console.WriteLine("Worker: Dispose called...");
            base.Dispose();
        }

        public static IEnumerable<Script>? GetScriptItems()
        {
            return scripts;
        }

        public static IEnumerable<Script> RegisterScripts(IServiceCollection services)
        {
            List<Type> scriptItemTypes = Assembly.GetEntryAssembly()?.GetTypes().Where(t => !t.IsAbstract && t.BaseType == typeof(IScript)).ToList() ?? new();

            List<Script> scriptItems = new();

            Console.Clear();
            foreach (Type type in scriptItemTypes)
            {
                services.AddScoped(type);
                scriptItems.Add(GetScript(type, ref services));
            }

            scriptItems = scriptItems.OrderBy(x => x.index ?? int.MaxValue).ThenBy(x => x.name).ToList();

            scripts = scriptItems;
            return scriptItems;
        }

        private static Script GetScript(Type type, ref IServiceCollection serviceCollection)
        {
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            List<Type> subScriptTypes = Assembly.GetEntryAssembly()?.GetTypes().Where(t => !t.IsAbstract && t.BaseType?.GenericTypeArguments.FirstOrDefault(y => y == type) != null).ToList() ?? new();

            List<Script> subScripts = new();
            foreach (Type subScriptType in subScriptTypes)
            {
                bool hasPublicDIConstructor = false;
                var constructors = subScriptType.GetConstructors();
                foreach(var constructor in constructors)
                {
                    bool isDIConstructor = true;
                    var parameters = constructor.GetParameters();
                    foreach(var parameter in parameters)
                    {
                        isDIConstructor &= (serviceProvider.GetService(parameter.ParameterType) != null);
                    }

                    hasPublicDIConstructor |= isDIConstructor;
                }

                if ((subScriptType.GetConstructor(Type.EmptyTypes) != null || hasPublicDIConstructor) && !subScriptType.IsAbstract )
                {
                    Console.WriteLine($"{subScriptType.Name} - Can be constructed with DI or Parameterless");
                    serviceCollection.AddScoped(subScriptType);
                    subScripts.Add(GetScript(subScriptType, ref serviceCollection));
                }                
            }

            bool hasScriptAttribute = Attribute.IsDefined(type, typeof(ScriptAttribute));

            int? index = null;
            string name = type.Name;
            string? description = null;

            if (hasScriptAttribute)
            {
                ScriptAttribute attribute = (ScriptAttribute)type.GetCustomAttribute(typeof(ScriptAttribute), false)!;

                index = attribute.index != -1 ? attribute.index : null;
                name = attribute.name ?? type.Name;
                description = attribute.description;
            }

            return new Script
            {
                index = index,
                name = name,
                description = description,
                subScripts = subScripts,
                type = type
            };
        }
        private static string _name { get; set; } = "ScriptManager";
        private static string? _version { get; set; }
        public static string ProgramTitle { get => $"{_name}{(_version != null ? $" v_{_version}" : "")}"; }
        public static string ProgramName { get => _name; }
        public static string? ProgramVersion { get => _version; }

        public static string SetProgramName(string name, IConfiguration configuration)
        {
            _name = name;
            _version = (string)configuration.GetValue<object>("version");

            return $"{_name} v_{_version}";
        }

        public static string SetProgramName(string name, string version)
        {
            _name = name;
            _version = version;

            return $"{_name} v_{_version}";
        }

        public static string SetProgramName(string name)
        {
            _name = name;

            return $"{_name}";
        }
    }
}
