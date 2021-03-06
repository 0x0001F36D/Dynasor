﻿/*
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
    using global::Dynasor.CodeAnalysis;
    using Microsoft.CodeAnalysis;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class CodeSnippet
    {
        internal static string Using(string ns)
        {
            return string.IsNullOrWhiteSpace(ns)
                ? string.Empty
                : $"using {ns};";
        }

        internal static string RandomString()
        {
            return "_" + Guid.NewGuid().ToString().Replace("-", null);
        }

        internal static void AppendRefs(StringBuilder sb, out HashSet<MetadataReference> references)
        {
            var namespaces = new HashSet<string>();
            references = new HashSet<MetadataReference>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                    continue;

                if (!string.IsNullOrWhiteSpace(assembly.Location) && references.Add(MetadataReference.CreateFromFile(assembly.Location)))
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
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="className"></param>
        /// <param name="method">The code of the method.</param>
        /// <param name="references"></param>
        /// <returns></returns>
        public static string GeneratePage(string className, IEnumerable<string> method, out HashSet<MetadataReference> references)
        {
            var sb = new StringBuilder();

            AppendRefs(sb, out references);

            sb.Append($"internal class {className}{{");
            foreach (var m in method)
            {
                if (MethodAutoCorrection.FixMethodModifiers(m, out var r, out var _))
                {
                    sb.AppendLine(r);
                }
                else
                {
                    throw new InvalidOperationException("Syntax error.");
                }
            }
            sb.Append("}");

            return sb.ToString();
        }
        
    }
}
