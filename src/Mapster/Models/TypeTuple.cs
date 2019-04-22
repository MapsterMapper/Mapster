using System;

namespace Mapster.Models
{
    public class TypeTuple : IEquatable<TypeTuple>
    {
        public bool Equals(TypeTuple other)
        {
            if (ReferenceEquals(null, other)) return false;
            return Source == other.Source && Destination == other.Destination;
        }

        public override bool Equals(object obj)
        {
            return Equals((obj as TypeTuple)!);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source?.GetHashCode() ?? 0) << 16) ^ ((Destination?.GetHashCode() ?? 0) & 65535);
            }
        }

        public static bool operator ==(TypeTuple left, TypeTuple right)
        {
            if (ReferenceEquals(null, left)) return ReferenceEquals(null, right);
            return left.Equals(right);
        }

        public static bool operator !=(TypeTuple left, TypeTuple right)
        {
            if (ReferenceEquals(null, left)) return !ReferenceEquals(null, right);
            return !left.Equals(right);
        }

        public Type Source { get; }
        public Type Destination { get; }

        public TypeTuple(Type source, Type destination)
        {
            this.Source = source;
            this.Destination = destination;
        }
    }
}
