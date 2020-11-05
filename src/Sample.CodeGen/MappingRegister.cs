using Mapster;
using Sample.CodeGen.Models;

namespace Sample.CodeGen
{
    public class MappingRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.ForType<Person, Person>()
                .GenerateMapper(MapType.Map | MapType.MapToTarget);
        }
    }
}
