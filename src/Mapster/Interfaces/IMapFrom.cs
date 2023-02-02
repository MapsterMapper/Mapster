namespace Mapster;

public interface IMapFrom<TSource>
{
    public void ConfigureMapping(TypeAdapterConfig config)
    {
        config.NewConfig(typeof(TSource), GetType());
    }
}