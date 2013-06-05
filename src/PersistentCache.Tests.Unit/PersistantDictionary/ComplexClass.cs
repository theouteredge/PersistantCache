using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PersistentCache.Tests.Unit.PersistantDictionary
{
    public class ComplexClass
    {
        public string Name { get; set; }
        public string Email { get; set; }

        public SimpleClass Person { get; set; }
        public IList<SimpleClass> ListOfPeople { get; set; }
    }

}
