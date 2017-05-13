namespace Mapster
{
    public interface IApplyable
    {
        void Apply(object other);
    }
    public interface IApplyable<T> : IApplyable
    {
        void Apply(T other);
    }
}