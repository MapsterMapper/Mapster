namespace Mapster.Registration
{
    public interface IRegistry
    {
        string Name { get; }

        void Apply();
    }

    public abstract class Registry : IRegistry
    {
        public virtual string Name { get { return null; } }

        public abstract void Apply();
    }
}
