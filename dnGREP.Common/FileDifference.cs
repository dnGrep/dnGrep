using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.Model;

namespace dnGREP.Common
{
    public class FileDifference
    {
        private static string GetText(string path, Encoding encoding)
        {
            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            using StreamReader readStream = new(fileStream, encoding, false, 4096, true);
            return readStream.ReadToEnd();
        }

        public static DiffPaneModel BuildLineDiff(string oldPath, string newPath, Encoding encoding)
        {
            bool ignoreWhiteSpace = false;
            bool ignoreCase = false;

            DiffPaneModel model = Diff(Differ.Instance, GetText(oldPath, encoding), GetText(newPath, encoding),
                ignoreWhiteSpace, ignoreCase);

            return model;
        }

        public static DiffPaneModel Diff(string oldText, string newText)
        {
            bool ignoreWhiteSpace = false;
            bool ignoreCase = false;

            DiffPaneModel model = Diff(Differ.Instance, oldText, newText,
                ignoreWhiteSpace, ignoreCase);

            return model;
        }

        private static DiffPaneModel Diff(IDiffer differ, string oldText, string newText, bool ignoreWhiteSpace = true, bool ignoreCase = false)
        {
            if (oldText == null) throw new ArgumentNullException(nameof(oldText));
            if (newText == null) throw new ArgumentNullException(nameof(newText));

            var model = new DiffPaneModel();
            var diffResult = (differ ?? Differ.Instance).CreateDiffs(oldText, newText, ignoreWhiteSpace, ignoreCase, LineChunker.Instance);
            BuildDiffPieces(diffResult, model.Lines, ignoreWhiteSpace, ignoreCase);

            return model;
        }

        private static void BuildDiffPieces(DiffResult diffResult, List<DiffPiece> pieces, bool ignoreWhiteSpace, bool ignoreCase)
        {
            int bPos = 0;

            List<DiffPair> insertDeletePairs = [];

            foreach (var diffBlock in diffResult.DiffBlocks)
            {
                for (; bPos < diffBlock.InsertStartB; bPos++)
                {
                    pieces.Add(new DiffPiece(diffResult.PiecesNew[bPos], ChangeType.Unchanged, bPos + 1));
                }

                int i = 0;
                for (; i < Math.Min(diffBlock.DeleteCountA, diffBlock.InsertCountB); i++)
                {
                    var item = new DiffPiece(diffResult.PiecesOld[i + diffBlock.DeleteStartA], ChangeType.Deleted);
                    pieces.Add(item);

                    insertDeletePairs.Add(new DiffPair() { DeletedPiece = item });
                }

                i = 0;
                for (; i < Math.Min(diffBlock.DeleteCountA, diffBlock.InsertCountB); i++)
                {
                    var item = new DiffPiece(diffResult.PiecesNew[i + diffBlock.InsertStartB], ChangeType.Inserted, bPos + 1);
                    pieces.Add(item);
                    if (insertDeletePairs.FirstOrDefault(p => p.InsertedPiece == null) is DiffPair diffPair)
                    {
                        diffPair.InsertedPiece = item;
                    }

                    bPos++;
                }

                if (diffBlock.DeleteCountA > diffBlock.InsertCountB)
                {
                    for (; i < diffBlock.DeleteCountA; i++)
                        pieces.Add(new DiffPiece(diffResult.PiecesOld[i + diffBlock.DeleteStartA], ChangeType.Deleted));
                }
                else
                {
                    for (; i < diffBlock.InsertCountB; i++)
                    {
                        pieces.Add(new DiffPiece(diffResult.PiecesNew[i + diffBlock.InsertStartB], ChangeType.Inserted, bPos + 1));
                        bPos++;
                    }
                }

                foreach (var pair in insertDeletePairs)
                {
                    if (pair.DeletedPiece != null && pair.InsertedPiece != null)
                    {
                        SubPieceBuilder(pair.DeletedPiece.Text, pair.InsertedPiece.Text,
                            pair.DeletedPiece.SubPieces, pair.InsertedPiece.SubPieces,
                            ignoreWhiteSpace, ignoreCase);
                    }
                }
                insertDeletePairs.Clear();
            }

            for (; bPos < diffResult.PiecesNew.Length; bPos++)
                pieces.Add(new DiffPiece(diffResult.PiecesNew[bPos], ChangeType.Unchanged, bPos + 1));
        }

        private static void SubPieceBuilder(string oldText, string newText, List<DiffPiece> oldPieces, List<DiffPiece> newPieces, bool ignoreWhitespace, bool ignoreCase)
        {
            var diffResult = Differ.Instance.CreateDiffs(oldText, newText, ignoreWhitespace, ignoreCase, CharacterChunker.Instance);
            BuildDiffPieces(diffResult, oldPieces, newPieces, ignoreWhitespace, ignoreCase);
        }

        private static void BuildDiffPieces(DiffResult diffResult, List<DiffPiece> oldPieces,
            List<DiffPiece> newPieces, bool ignoreWhiteSpace, bool ignoreCase)
        {
            int aPos = 0;
            int bPos = 0;

            foreach (var diffBlock in diffResult.DiffBlocks)
            {
                while (bPos < diffBlock.InsertStartB && aPos < diffBlock.DeleteStartA)
                {
                    oldPieces.Add(new DiffPiece(diffResult.PiecesOld[aPos], ChangeType.Unchanged, aPos + 1));
                    newPieces.Add(new DiffPiece(diffResult.PiecesNew[bPos], ChangeType.Unchanged, bPos + 1));
                    aPos++;
                    bPos++;
                }

                int i = 0;
                for (; i < Math.Min(diffBlock.DeleteCountA, diffBlock.InsertCountB); i++)
                {
                    var oldPiece = new DiffPiece(diffResult.PiecesOld[i + diffBlock.DeleteStartA], ChangeType.Deleted, aPos + 1);
                    var newPiece = new DiffPiece(diffResult.PiecesNew[i + diffBlock.InsertStartB], ChangeType.Inserted, bPos + 1);

                    oldPieces.Add(oldPiece);
                    newPieces.Add(newPiece);
                    aPos++;
                    bPos++;
                }

                if (diffBlock.DeleteCountA > diffBlock.InsertCountB)
                {
                    for (; i < diffBlock.DeleteCountA; i++)
                    {
                        oldPieces.Add(new DiffPiece(diffResult.PiecesOld[i + diffBlock.DeleteStartA], ChangeType.Deleted, aPos + 1));
                        newPieces.Add(new DiffPiece());
                        aPos++;
                    }
                }
                else
                {
                    for (; i < diffBlock.InsertCountB; i++)
                    {
                        newPieces.Add(new DiffPiece(diffResult.PiecesNew[i + diffBlock.InsertStartB], ChangeType.Inserted, bPos + 1));
                        oldPieces.Add(new DiffPiece());
                        bPos++;
                    }
                }
            }

            while (bPos < diffResult.PiecesNew.Length && aPos < diffResult.PiecesOld.Length)
            {
                oldPieces.Add(new DiffPiece(diffResult.PiecesOld[aPos], ChangeType.Unchanged, aPos + 1));
                newPieces.Add(new DiffPiece(diffResult.PiecesNew[bPos], ChangeType.Unchanged, bPos + 1));
                aPos++;
                bPos++;
            }
        }


        private class DiffPair
        {
            public DiffPiece? DeletedPiece { get; set; }
            public DiffPiece? InsertedPiece { get; set; }
        }

    }
}
