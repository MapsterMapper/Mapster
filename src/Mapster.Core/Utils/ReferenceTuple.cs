using System;
using System.Runtime.CompilerServices;

namespace Mapster.Utils
{
    public readonly struct ReferenceTuple : IEquatable<ReferenceTuple>
    {
        public object Reference { get; }
        public Type DestinationType { get; }
        public ReferenceTuple(object reference, Type destinationType)
        {
            this.Reference = reference;
            this.DestinationType = destinationType;
        }

        public override bool Equals(object obj)
        {
            return obj is ReferenceTuple other && Equals(other);
        }

        public bool Equals(ReferenceTuple other)
        {
            return ReferenceEquals(this.Reference, other.Reference) 
                   && this.DestinationType == other.DestinationType;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RuntimeHelpers.GetHashCode(this.Reference) * 397) ^ DestinationType.GetHashCode();
            }
        }

        public static bool operator ==(ReferenceTuple left, ReferenceTuple right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ReferenceTuple left, ReferenceTuple right)
        {
            return !(left == right);
        }
    }
}
