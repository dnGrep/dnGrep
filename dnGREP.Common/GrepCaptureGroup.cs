using System;

namespace dnGREP.Common
{
    public class GrepCaptureGroup : IComparable<GrepCaptureGroup>, IComparable, IEquatable<GrepCaptureGroup>
    {
        public GrepCaptureGroup(string name, int startPosition, int length, string value)
        {
            Name = name;
            StartLocation = startPosition;
            Length = length;
            Value = value;
        }

        public string Name { get; } = string.Empty;

        public int StartLocation { get; } = 0;

        public int Length { get; } = 0;

        public string Value { get; }

        public override string ToString()
        {
            return $"Group '{Name}' {StartLocation} +{Length} : {Value}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as GrepCaptureGroup);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 13;
                hashCode = (hashCode * 397) ^ StartLocation;
                hashCode = (hashCode * 397) ^ Length;
                return hashCode;
            }
        }

        public bool Equals(GrepCaptureGroup other)
        {
            if (other == null) return false;

            return StartLocation == other.StartLocation &&
                Length == other.Length;
        }

        public int CompareTo(GrepCaptureGroup other)
        {
            if (other == null)
                return 1;
            else
                return StartLocation.CompareTo(other.StartLocation);
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (obj is GrepMatch)
                return StartLocation.CompareTo(((GrepMatch)obj).StartLocation);
            else
                return 1;
        }
    }
}
