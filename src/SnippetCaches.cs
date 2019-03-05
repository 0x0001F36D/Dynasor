/*
    Copyright 2019 Viyrex

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/
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
