using System;

namespace Mapster.Models
{
    public readonly struct TypeTuple : IEquatable<TypeTuple>
    {
        public bool Equals(TypeTuple other)
        {
            return Source == other.Source && Destination == other.Destination;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TypeTuple))
                return false;
            return Equals((TypeTuple)obj);
        }

        public override int GetHashCode()
        {
            return (Source.GetHashCode() << 16) ^ (Destination.GetHashCode() & 65535);
        }

        public static bool operator ==(TypeTuple left, TypeTuple right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TypeTuple left, TypeTuple right)
        {
            return !left.Equals(right);
        }

        public Type Source { get; }
        public Type Destination { get; }

        public TypeTuple(Type source, Type destination)
        {
            Source = source;
            Destination = destination;
        }
    }

    public class InheritsTypeTuple : IEquatable<InheritsTypeTuple>
    {
        public bool Equals(InheritsTypeTuple other)
        {
            return Source == other.Source && Destination == other.Destination;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is InheritsTypeTuple))
                return false;
            return Equals((InheritsTypeTuple)obj);
        }

        public override int GetHashCode()
        {
            return (Source.GetHashCode() << 16) ^ (Destination.GetHashCode() & 65535);
        }

        public static bool operator ==(InheritsTypeTuple left, InheritsTypeTuple right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InheritsTypeTuple left, InheritsTypeTuple right)
        {
            return !left.Equals(right);
        }

        public Type Source { get; }
        public Type Destination { get; }
        public bool IsLoading { get; private set; }

        public void IsUploaded()
        {
            IsLoading = true;
        }

        public InheritsTypeTuple(Type source, Type destination)
        {
            Source = source;
            Destination = destination;
            IsLoading = false;
        }
    }
}
