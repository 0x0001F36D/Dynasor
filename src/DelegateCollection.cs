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
    using System;
    using System.Collections.Generic;
    using System.Dynamic;

    internal sealed class DelegateCollection : DynamicObject
    {
        private readonly IDictionary<string, Delegate> _collection;

        internal DelegateCollection(IDictionary<string, Delegate> collection)
        {
            this._collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }
        
        public override IEnumerable<string> GetDynamicMemberNames() => this._collection.Keys;

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (this._collection.TryGetValue(binder.Name, out var d))
            {
                result = d;
                return true;
            }
            return base.TryGetMember(binder, out result);
        }

        public dynamic Invoke(string name)
        {
            if (this._collection.TryGetValue(name, out var d))
            {
                return d;
            }
            throw new InvalidOperationException();
        }
    }
}
