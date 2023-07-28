using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using ATDR.Extension;

namespace ATDR {
    public class Loader {
        public static Loader INSTANCE = new Loader();

        private readonly Assembly[] assemblies;

        public Loader() {
            List<Assembly> assemblies = new List<Assembly>();
            string[] files = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ext"));
            foreach(string file in files) {
                if (file.EndsWith(".dll")) {
                    assemblies.Add(Assembly.LoadFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ext", file)));
                }
            }

            this.assemblies = assemblies.ToArray();
        }
        
        public MethodInfo[] GetTableDefinitions() {
            return assemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsDefined(typeof(XrmInformation)))
                .SelectMany(type => type.GetMethods())
                .Where((method => method.GetCustomAttribute(typeof(XrmTable), false) != null))
                .ToArray();
        }

        public MethodInfo[] GetConverters() {
            return assemblies.SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsDefined(typeof(XrmInformation)))
                .SelectMany(type => type.GetMethods())
                .Where((method => method.GetCustomAttribute(typeof(XrmConversion), false) != null))
                .ToArray();
        }
    }
}