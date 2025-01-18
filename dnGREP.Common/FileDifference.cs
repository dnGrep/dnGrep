using System;
using System.Collections.Generic;
using System.Linq;
using NetDiff;

namespace dnGREP.Common
{
    public class FileDifference
    {
        private static readonly string[] lineSeparators = ["\r\n", "\r", "\n"];
        private static readonly char[] wordSeparators = [ ' ', '\t', '.', ',', ';', ':', '!', '?',
                '\\', '/', '"', '\'', '`', '(', ')', '{', '}', '[', ']', '<', '>' ];

        public static DiffModel GetFileDifferences(string oldText, string newText, bool multiline)
        {
            string[] oldTextLines = oldText.Split(lineSeparators, StringSplitOptions.None);
            string[] newTextLines = newText.Split(lineSeparators, StringSplitOptions.None);

            var results = DiffUtil.Diff(oldTextLines, newTextLines);
            List<DiffResult<string>> ordered = DiffUtil.Order(results,
                multiline ? DiffOrderType.GreedyDeleteFirst : DiffOrderType.LazyDeleteFirst).ToList();

            return GetDiffModel(ordered, multiline);
        }

        private static DiffModel GetDiffModel(List<DiffResult<string>> elements, bool multiline)
        {
            DiffModel model = new();
            List<DiffPair> insertDeletePairs = [];
            int lineNumber = 1;

            foreach (var element in elements)
            {
                switch (element.Status)
                {
                    case DiffStatus.Equal:
                        model.Lines.Add(new DiffPiece(element.Obj1, DiffStatus.Equal, lineNumber++));
                        break;

                    case DiffStatus.Deleted:
                        DiffPiece deleted = new DiffPiece(element.Obj1, DiffStatus.Deleted, null);
                        model.Lines.Add(deleted);
                        if (!multiline)
                        {
                            insertDeletePairs.Add(new DiffPair() { DeletedPiece = deleted });
                        }
                        break;

                    case DiffStatus.Inserted:
                        DiffPiece inserted = new DiffPiece(element.Obj2, DiffStatus.Inserted, lineNumber++);
                        model.Lines.Add(inserted);
                        if (!multiline &&
                            insertDeletePairs.FirstOrDefault(p => p.InsertedPiece == null) is DiffPair diffPair)
                        {
                            diffPair.InsertedPiece = inserted;
                        }
                        break;
                }
            }

            foreach (var pair in insertDeletePairs)
            {
                SplitSubPieces(pair);
            }
            insertDeletePairs.Clear();

            return model;
        }

        private static void SplitSubPieces(DiffPair pair)
        {
            if (pair.DeletedPiece != null && pair.DeletedPiece.Text != null &&
                pair.InsertedPiece != null && pair.InsertedPiece.Text != null)
            {
                string[] oldTextWords = SplitWords(pair.DeletedPiece.Text);
                string[] newTextWords = SplitWords(pair.InsertedPiece.Text);

                var results = DiffUtil.Diff(oldTextWords, newTextWords);
                List<DiffResult<string>> ordered = DiffUtil.Order(results,
                    DiffOrderType.GreedyDeleteFirst).ToList();

                int delPos = 1;
                int insPos = 1;

                foreach (var element in ordered)
                {
                    switch (element.Status)
                    {
                        case DiffStatus.Equal:
                            pair.DeletedPiece.SubPieces.Add(new DiffPiece(element.Obj1, DiffStatus.Equal, delPos++));
                            pair.InsertedPiece.SubPieces.Add(new DiffPiece(element.Obj2, DiffStatus.Equal, insPos++));
                            break;

                        case DiffStatus.Deleted:
                            pair.DeletedPiece.SubPieces.Add(new DiffPiece(element.Obj1, DiffStatus.Deleted, delPos++));
                            break;

                        case DiffStatus.Inserted:
                            pair.InsertedPiece.SubPieces.Add(new DiffPiece(element.Obj2, DiffStatus.Inserted, insPos++));
                            break;
                    }
                }
            }
        }

        private static string[] SplitWords(string str)
        {
            var list = new List<string>();
            int begin = 0;
            bool processingDelim = false;
            int delimBegin = 0;
            for (int i = 0; i < str.Length; i++)
            {
                if (Array.IndexOf(wordSeparators, str[i]) != -1)
                {
                    if (i >= str.Length - 1)
                    {
                        if (processingDelim)
                        {
                            list.Add(str.Substring(delimBegin, (i + 1 - delimBegin)));
                        }
                        else
                        {
                            list.Add(str.Substring(begin, (i - begin)));
                            list.Add(str.Substring(i, 1));
                        }
                    }
                    else
                    {
                        if (!processingDelim)
                        {
                            // Add everything up to this delimiter as the next chunk (if there is anything)
                            if (i - begin > 0)
                            {
                                list.Add(str.Substring(begin, (i - begin)));
                            }

                            processingDelim = true;
                            delimBegin = i;
                        }
                    }

                    begin = i + 1;
                }
                else
                {
                    if (processingDelim)
                    {
                        if (i - delimBegin > 0)
                        {
                            list.Add(str.Substring(delimBegin, (i - delimBegin)));
                        }

                        processingDelim = false;
                    }

                    // If we are at the end, add the remaining as the last chunk
                    if (i >= str.Length - 1)
                    {
                        list.Add(str.Substring(begin, (i + 1 - begin)));
                    }
                }
            }

            return [.. list];
        }

        private class DiffPair
        {
            public DiffPiece? DeletedPiece { get; set; }
            public DiffPiece? InsertedPiece { get; set; }
        }
    }

    public class DiffModel
    {
        public List<DiffPiece> Lines { get; } = [];

        public bool HasDifferences
        {
            get { return Lines.Any(x => x.Operation != DiffStatus.Equal); }
        }
    }

    public class DiffPiece : IEquatable<DiffPiece>
    {
        public DiffStatus Operation { get; private set; }
        public int? Position { get; private set; }
        public string Text { get; private set; }
        public List<DiffPiece> SubPieces { get; set; } = [];

        public DiffPiece(string text, DiffStatus operation, int? position = null)
        {
            Text = text;
            Position = position;
            Operation = operation;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DiffPiece);
        }

        public bool Equals(DiffPiece? other)
        {
            return other != null
                && Operation == other.Operation
                && EqualityComparer<int?>.Default.Equals(Position, other.Position)
                && Text == other.Text
                && SubPieces.SequenceEqual(other.SubPieces);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Operation, Position, Text, SubPieces);
        }
    }
}
