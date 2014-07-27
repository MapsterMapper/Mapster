namespace Fpr
{
    public interface ITypeResolver<in TSource, out TDestination>
    {
        TDestination Resolve(TSource source);
    }
}