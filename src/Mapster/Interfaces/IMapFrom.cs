namespace Mapster;

public interface IMapFrom<TSource>
{

#if  NET6_0_OR_GREATER
    public void ConfigureMapping(TypeAdapterConfig config)
    {
        config.NewConfig(typeof(TSource), GetType());
    }
#endif

}