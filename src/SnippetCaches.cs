
namespace Dynasor
{
    using Microsoft.CodeAnalysis;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class SnippetCaches
    {
        internal static readonly HashSet<MetadataReference> References;
        public static StringBuilder CreateNewSnippet()
        {
            return new StringBuilder(s_result);
        }
        
        private static readonly string s_result;
        static SnippetCaches()
        {
            AppendRefs(out var result, out var refs);
            s_result = result;
            References = refs;
        }


        private static string Using(string ns)
        {
            return string.IsNullOrWhiteSpace(ns)
                ? string.Empty
                : $"using {ns};\r\n";
        }
        
        private static void AppendRefs(out string result, out HashSet<MetadataReference> references)
        {
            var sb = new StringBuilder();
            var namespaces = new HashSet<string>();
            references = new HashSet<MetadataReference>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                if (!string.IsNullOrWhiteSpace(assembly.Location) && 
                    references.Add(MetadataReference.CreateFromFile(assembly.Location)))
                {
                    var nss = from type in assembly.GetTypes()
                              let
                                ns = type.Namespace
                              where
                                type.IsPublic &&
                                !ns.Contains("Internal", StringComparison.CurrentCultureIgnoreCase) &&
                                namespaces.Add(ns)
                              select ns;

                    foreach (var ns in nss)
                        sb.Append(Using(ns));
                }
            }
            namespaces.Clear();
            result = sb.ToString();
        }

    }
    
    
}
