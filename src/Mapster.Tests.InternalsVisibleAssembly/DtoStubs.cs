using System;
using System.Runtime.CompilerServices;

//Expose this to Mapster.Tests
[assembly: InternalsVisibleTo("Mapster.Tests")]

namespace Mapster.Tests.InternalsVisibleAssembly
{
    public class DtoInternal
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; } = -1;
        public string Prop { get; set; }
        public string OtherProp { get; set; }

        internal DtoInternal() { }
    }    

    public class DtoPrivate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Age { get; } = -1;
        public string Prop { get; set; }

        private DtoPrivate() { }
    }
}

