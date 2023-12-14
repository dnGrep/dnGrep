using System;

namespace dnGREP.Common
{
    public class GrepCaptureGroup(string name, int startPosition, int length, string value) : IComparable<GrepCaptureGroup>, IComparable, IEquatable<GrepCaptureGroup>
    {
        public string Name { get; } = name;

        public int StartLocation { get; } = startPosition;

        public int Length { get; } = length;

        public string Value { get; } = value;

        public override string ToString()
        {
            return $"Group '{Name}' {StartLocation} +{Length} : {Value}";
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as GrepCaptureGroup);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartLocation, Length);
        }

        public bool Equals(GrepCaptureGroup? other)
        {
            if (other == null) return false;

            return StartLocation == other.StartLocation &&
                Length == other.Length;
        }

        public int CompareTo(GrepCaptureGroup? other)
        {
            if (other == null)
                return 1;
            else
                return StartLocation.CompareTo(other.StartLocation);
        }

        public int CompareTo(object? obj)
        {
            if (obj == null)
                return 1;
            if (obj is GrepMatch match)
                return StartLocation.CompareTo(match.StartLocation);
            else
                return 1;
        }
    }
}
