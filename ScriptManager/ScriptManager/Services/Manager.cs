using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScriptManager.Attributes;
using ScriptManager.Extensions;
using ScriptManager.Interfaces;
using ScriptManager.Models;
using System.Reflection;
using System.Runtime.InteropServices;
using Script = ScriptManager.Models.Script;

namespace ScriptManager.Services
{
    public class Manager : BackgroundService
    {
        private readonly IHostApplicationLifetime _lifetime;
        private readonly IServiceProvider _services;

        private static string _name { get; set; } = "ScriptManager";
        private static string? _version { get; set; } = "1.0";
        public static string Title { get => $"{_name}{(_version != null ? $" {_version}" : "")}"; }

        private static IEnumerable<Script>? _scripts;
        public Manager(IHostApplicationLifetime lifetime, IServiceProvider services, IConfiguration configuration)
        {
            _lifetime = lifetime;
            _services = services;

            _version = (string)configuration.GetValue<object>("ScriptManager:version") ?? _version;
            _name = (string)configuration.GetValue<object>("ScriptManager:title") ?? _name;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_scripts == null)
            {
                throw new Exception("Failed to fetch scripts - Did you run Manager.RegisterScripts(IServiceCollection services) ?");
            }

            bool showMenu = true;
            while (!stoppingToken.IsCancellationRequested && showMenu)
            {
                showMenu = ShowMenu(stoppingToken);
            }
            Console.WriteLine("Worker: ExecuteAsync is terminating...");
            _lifetime.StopApplication();
        }

        private bool ShowMenu(CancellationToken stoppingToken, Script? item = null)
        {
            bool showMenu = true;
            IEnumerable<Script> scripts = item?.subScripts ?? _scripts ?? throw new Exception($"Could not load any scripts for {item?.name ?? "base menu."}");

            IScript? scriptService = null;
            if (item != null)
            {
                scriptService = GetScriptService(item);

                item.subScripts.AddRange(scriptService.SubScripts);
                item.subScripts = item.subScripts.Distinct().ToList();

                if (item.subScripts.Any())
                {
                    int count = item.subScripts.Max(x => x.index ?? -1) + 1;
                    item.subScripts = item.subScripts.OrderBy(x => x.name).Select(x =>
                    {
                        if (x.index == null || x.index == -1)
                        {
                            x.index = count;
                            ++count;
                        }
                        return x;
                    }).OrderBy(x => x.index).ToList();
                }

                scripts = item.subScripts;
            }
            Console.Clear();

            if (!scripts.Any())
            {
                scriptService?.Run();
                Console.ReadLine();
                return false;
            }

            // Print header
            Console.WriteLine(string.Format("{0," + ((Console.WindowWidth / 2) + ((item?.name?.Length ?? Title.Length) / 2)) + "}", item?.name ?? Title));

            foreach (Script script in scripts)
            {
                PrintMenuItem(script);
            }

            PrintMenuOptions(item);
            var selectedOption = SelectMenuOption(item);

            if (selectedOption.script != null)
            {
                if (selectedOption.script == item)
                {
                    foreach (var scr in item.subScripts)
                    {
                        var scrService = GetScriptService(scr);
                        scr.Instance = scrService;

                        scriptService?.AddSubScript(scr);
                    }

                    scriptService.SubScripts = scriptService?.SubScripts.Distinct();

                    scriptService?.RunAll();
                    Console.ReadLine();
                    return false;
                }

                // DO THINGS WITH SCRIPT
                while (!stoppingToken.IsCancellationRequested && showMenu)
                {
                    showMenu = ShowMenu(stoppingToken, selectedOption.script);
                }
            }

            return !selectedOption.shouldExit;
        }

        private IScript GetScriptService(Script item)
        {
            IScript? scriptService = (IScript?)_services.GetService(item.type);

            if (scriptService == null && item.Instance == null)
            {
                //TODO: Log service not found
                throw new Exception("Service not found");
            }

            return scriptService ?? item.Instance!;
        }

        private static void PrintMenuOptions(Script? item)
        {
            Console.WriteLine();
            Console.WriteLine("[A]: Run All");

            if (item == null)
            {
                Console.WriteLine("[X]: Exit");
            } else
            {
                Console.WriteLine("[B]: Back");
            }

            Console.WriteLine();
            Console.Write("Select your option: ");
        }

        private static (Script? script, bool shouldExit) SelectMenuOption(Script? item)
        {
            string? input = Console.ReadLine()?.ToLower();

            if (input == null)
            {
                return (null, false);
            }

            switch (input)
            {
                case "b":
                    return (null, item != null);
                case "x":
                    return (null, item == null);
                case "a":
                    return (item, false);
                default:
                    if (int.TryParse(input, out int index))
                    {
                        IEnumerable<Script> scripts = item?.subScripts ?? _scripts ?? throw new Exception($"Could not load any scripts for {item?.name ?? "base menu."}");
                        return (scripts?.FirstOrDefault(x => x.index == index), false);
                    }
                    return (null, false);
            }
        }

        private static void PrintMenuItem(Script menuItem)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{menuItem.index.ToString() ?? "UNDEFINED"}]");

            Console.ResetColor();
            Console.Write(":");

            if (menuItem.name != null)
            {
                Console.WriteLine($" {menuItem.name}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" UNDEFINED");
            }
        }

        public static IEnumerable<Script>? GetScriptItems()
        {
            return _scripts;
        }

        public static IEnumerable<Script> RegisterScripts(IServiceCollection services)
        {
            Type baseType = typeof(IScript);
            // Get all child types of IScript
            // = NOT abstract
            // = BaseType IScript
            // = NOT an interface
            List<Type> scriptItemTypes = Assembly.GetEntryAssembly()?.GetTypes().Where(t => !t.IsAbstract && t.BaseType == baseType && !t.IsInterface).ToList() ?? new();

            List<Script> scriptItems = new();
            foreach (Type type in scriptItemTypes)
            {
                // Add as scoped service
                services.AddScoped(type);
                scriptItems.Add(GetScript(type, ref services));
            }

            int count = scriptItems.Max(x => x.index ?? -1) + 1;
            scriptItems = scriptItems.OrderBy(x => x.name).Select(x =>
            {
                if (x.index == null || x.index == -1)
                {
                    x.index = count;
                    ++count;
                }
                return x;
            }).OrderBy(x => x.index).ToList();

            _scripts = scriptItems;
            return scriptItems;
        }

        private static Script GetScript(Type type, ref IServiceCollection serviceCollection)
        {
            Console.WriteLine("Type: " + type.FullName);


            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            // Get all child types of the given type 'type'
            // = NOT abstract
            // = BaseType with generic type argument of the give type 'type'
            List<Type> subScriptTypes = Assembly.GetEntryAssembly()?.GetTypes().Where(t => !t.IsAbstract && t.BaseType?.GenericTypeArguments.FirstOrDefault(y => y == type) != null).ToList() ?? new();

            //var testTypes = Assembly.GetEntryAssembly()?.GetTypes().Where(x => x.);

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

                // Type can be constructed with a parameterless constructor OR can be constructed using DI
                if ((subScriptType.GetConstructor(Type.EmptyTypes) != null || hasPublicDIConstructor) && !subScriptType.IsAbstract )
                {
                    // Add type as scoped service
                    serviceCollection.AddScoped(subScriptType);
                    subScripts.Add(GetScript(subScriptType, ref serviceCollection));
                }                
            }

            if (subScripts.Any())
            {
                int count = subScripts.Max(x => x.index ?? -1) + 1;
                subScripts = subScripts.OrderBy(x => x.name).Select(x =>
                {
                    if (x.index == null || x.index == -1)
                    {
                        x.index = count;
                        ++count;
                    }
                    return x;
                }).OrderBy(x => x.index).ToList();
            }

            int? index = null;
            string name = type.Name;
            string? description = null;

            if (Attribute.IsDefined(type, typeof(ScriptAttribute)))
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
    }
}
