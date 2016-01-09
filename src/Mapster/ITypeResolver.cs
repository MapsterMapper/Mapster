namespace Mapster
{
    public interface ITypeResolver<in TSource, out TDestination>
    {
        TDestination Resolve(TSource source);
    }

    public interface ITypeResolverWithTarget<in TSource, TDestination> : ITypeResolver<TSource, TDestination>
    {
        TDestination Resolve(TSource source, TDestination destination);
    }
}