using System;
using System.Collections.Generic;
using System.Linq;

namespace NetDiff
{
    internal enum Direction
    {
        Right,
        Bottom,
        Diagonal,
    }

    internal readonly struct Point(int x, int y) : IEquatable<Point>
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        public override bool Equals(object obj)
        {
            if (obj is Point other)
            {
                return Equals(other);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override string ToString()
        {
            return $"X:{X} Y:{Y}";
        }
    }

    internal record Node(Point Point)
    {
        public Point Point { get; set; } = Point;
        public Node Parent { get; set; }

        public override string ToString()
        {
            return $"X:{Point.X} Y:{Point.Y}";
        }
    }

    internal class EditGraph<T>
    {
        private readonly T[] seq1;
        private readonly T[] seq2;
        private DiffOption<T> option;
        private List<Node> heads = [];
        private readonly Point endpoint;
        private readonly int[] farthestPoints;
        private readonly int offset;
        private bool isEnd;

        public EditGraph(IEnumerable<T> seq1, IEnumerable<T> seq2)
        {
            this.seq1 = seq1.ToArray();
            this.seq2 = seq2.ToArray();
            endpoint = new Point(this.seq1.Length, this.seq2.Length);
            offset = this.seq2.Length;
            farthestPoints = new int[this.seq1.Length + this.seq2.Length + 1];
        }

        public List<Point> CalculatePath(DiffOption<T> option)
        {
            if (seq1.Length == 0)
                return Enumerable.Range(0, seq2.Length + 1).Select(i => new Point(0, i)).ToList();

            if (seq2.Length == 0)
                return Enumerable.Range(0, seq1.Length + 1).Select(i => new Point(i, 0)).ToList();

            this.option = option;

            BeginCalculatePath();

            while (Next()) { }

            return EndCalculatePath();
        }

        private void BeginCalculatePath()
        {
            heads.Add(new Node(new Point(0, 0)));

            Snake();
        }

        private List<Point> EndCalculatePath()
        {
            var waypoint = new List<Point>();

            var current = heads.Where(h => h.Point.Equals(endpoint)).FirstOrDefault();
            while (current != null)
            {
                waypoint.Add(current.Point);

                current = current.Parent;
            }

            waypoint.Reverse();

            return waypoint;
        }

        private bool Next()
        {
            if (isEnd)
                return false;

            UpdateHeads();

            return true;
        }

        private void UpdateHeads()
        {
            if (option.Limit > 0 && heads.Count > option.Limit)
            {
                var tmp = heads.First();
                heads.Clear();

                heads.Add(tmp);
            }

            var updated = new List<Node>();

            foreach (var head in heads)
            {
                if (TryCreateHead(head, Direction.Right, out Node rightHead) && rightHead != null)
                {
                    updated.Add(rightHead);
                }

                if (TryCreateHead(head, Direction.Bottom, out Node bottomHead) && bottomHead != null)
                {
                    updated.Add(bottomHead);
                }
            }

            heads = updated;

            Snake();
        }

        private void Snake()
        {
            var tmp = new List<Node>();
            foreach (var h in heads)
            {
                var newHead = Snake(h);

                if (newHead != null)
                    tmp.Add(newHead);
                else
                    tmp.Add(h);
            }

            heads = tmp;
        }

        private Node Snake(Node head)
        {
            Node newHead = null;
            while (true)
            {
                if (TryCreateHead(newHead ?? head, Direction.Diagonal, out Node tmp))
                    newHead = tmp;
                else
                    break;
            }

            return newHead;
        }

        private bool TryCreateHead(Node head, Direction direction, out Node newHead)
        {
            newHead = null;
            var newPoint = GetPoint(head.Point, direction);

            if (!CanCreateHead(head.Point, direction, newPoint))
                return false;

            newHead = new Node(newPoint)
            {
                Parent = head
            };

            isEnd |= newHead.Point.Equals(endpoint);

            return true;
        }

        private bool CanCreateHead(Point currentPoint, Direction direction, Point nextPoint)
        {
            if (!InRange(nextPoint))
                return false;

            if (direction == Direction.Diagonal)
            {
                var equal = option.EqualityComparer != null
                    ? option.EqualityComparer.Equals(seq1[nextPoint.X - 1], seq2[nextPoint.Y - 1])
                    : seq1[nextPoint.X - 1].Equals(seq2[nextPoint.Y - 1]);

                if (!equal)
                    return false;
            }

            return UpdateFarthestPoint(nextPoint);
        }

        private static Point GetPoint(Point currentPoint, Direction direction)
        {
            return direction switch
            {
                Direction.Right => new Point(currentPoint.X + 1, currentPoint.Y),
                Direction.Bottom => new Point(currentPoint.X, currentPoint.Y + 1),
                Direction.Diagonal => new Point(currentPoint.X + 1, currentPoint.Y + 1),
                _ => throw new Exception("Invalid"),
            };
        }

        private bool InRange(Point point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X <= endpoint.X && point.Y <= endpoint.Y;
        }

        private bool UpdateFarthestPoint(Point point)
        {
            var k = point.X - point.Y;
            var y = farthestPoints[k + offset];

            if (point.Y <= y)
                return false;

            farthestPoints[k + offset] = point.Y;

            return true;
        }
    }
}

