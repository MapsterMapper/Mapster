using System.Collections.Generic;
using Mapster.Models;

namespace Mapster
{
    public class CompileContext
    {
        public readonly HashSet<TypeTuple> Running = new HashSet<TypeTuple>();
        public readonly TypeAdapterConfig Config;

        public CompileContext(TypeAdapterConfig config)
        {
            this.Config = config;
        }
    }
}