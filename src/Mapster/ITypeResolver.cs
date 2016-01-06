namespace Mapster
{
    public interface ITypeResolver<in TSource, TDestination>
    {
        TDestination Resolve(TSource source);
        TDestination Resolve(TSource source, TDestination destination);
    }
}