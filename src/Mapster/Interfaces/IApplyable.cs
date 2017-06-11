namespace Mapster
{
    public interface IApplyable
    {
        void Apply(object other);
    }
    public interface IApplyable<in T> : IApplyable
    {
        void Apply(T other);
    }
}