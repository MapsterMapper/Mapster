namespace Mapster
{
    public interface ITypeResolver<in TSource, out TDestination>
    {
        TDestination Resolve(TSource source);
    }
}