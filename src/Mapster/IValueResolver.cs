namespace Mapster
{
    public interface IValueResolver<in TSource, out TDestinationMember>
    {
        TDestinationMember Resolve(TSource source);
    }
}