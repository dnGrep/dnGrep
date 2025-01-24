using System;
using System.Collections.Generic;
using System.Linq;

namespace NetDiff
{
    public static class DiffUtil
    {
        public static IEnumerable<DiffResult<T>> Diff<T>(IEnumerable<T> seq1, IEnumerable<T> seq2)
        {
            return Diff(seq1, seq2, new DiffOption<T>());
        }

        public static IEnumerable<DiffResult<T>> Diff<T>(IEnumerable<T> seq1, IEnumerable<T> seq2, DiffOption<T> option)
        {
            if (seq1 == null || seq2 == null || (!seq1.Any() && !seq2.Any()))
                return [];

            var editGraph = new EditGraph<T>(seq1, seq2);
            var waypoints = editGraph.CalculatePath(option);

            return MakeResults<T>(waypoints, seq1, seq2);
        }

        public static IEnumerable<T> CreateSrc<T>(IEnumerable<DiffResult<T>> diffResults)
        {
            return diffResults.Where(r => r.Status != DiffStatus.Inserted).Select(r => r.Obj1);
        }

        public static IEnumerable<T> CreateDst<T>(IEnumerable<DiffResult<T>> diffResults)
        {
            return diffResults.Where(r => r.Status != DiffStatus.Deleted).Select(r => r.Obj2);
        }

        public static IEnumerable<DiffResult<T>> OptimizeCaseDeletedFirst<T>(IEnumerable<DiffResult<T>> diffResults)
        {
            return Optimize(diffResults, true);
        }

        public static IEnumerable<DiffResult<T>> OptimizeCaseInsertedFirst<T>(IEnumerable<DiffResult<T>> diffResults)
        {
            return Optimize(diffResults, false);
        }

        private static IEnumerable<DiffResult<T>> Optimize<T>(IEnumerable<DiffResult<T>> diffResults, bool deleteFirst = true)
        {
            var currentStatus = deleteFirst ? DiffStatus.Deleted : DiffStatus.Inserted;
            var nextStatus = deleteFirst ? DiffStatus.Inserted : DiffStatus.Deleted;

            var queue = new Queue<DiffResult<T>>(diffResults);
            while (queue.Count != 0)
            {
                var result = queue.Dequeue();
                if (result.Status == currentStatus)
                {
                    if (queue.Count > 0 && queue.Peek().Status == nextStatus)
                    {
                        var obj1 = deleteFirst ? result.Obj1 : queue.Dequeue().Obj1;
                        var obj2 = deleteFirst ? queue.Dequeue().Obj2 : result.Obj2;
                        yield return new DiffResult<T>(obj1, obj2, DiffStatus.Modified);
                    }
                    else
                        yield return result;

                    continue;
                }

                yield return result;
            }
        }

        private static IEnumerable<DiffResult<T>> MakeResults<T>(IEnumerable<Point> waypoints, IEnumerable<T> seq1, IEnumerable<T> seq2)
        {
            var array1 = seq1.ToArray();
            var array2 = seq2.ToArray();

            foreach (var pair in waypoints.MakePairsWithNext())
            {
                var status = GetStatus(pair.Item1, pair.Item2);
                T obj1 = default;
                T obj2 = default;
                switch (status)
                {
                    case DiffStatus.Equal:
                        obj1 = array1[pair.Item2.X - 1];
                        obj2 = array2[pair.Item2.Y - 1];
                        break;
                    case DiffStatus.Inserted:
                        obj2 = array2[pair.Item2.Y - 1];
                        break;
                    case DiffStatus.Deleted:
                        obj1 = array1[pair.Item2.X - 1];
                        break;
                }

                yield return new DiffResult<T>(obj1, obj2, status);
            }
        }

        private static DiffStatus GetStatus(Point current, Point prev)
        {
            if (current.X != prev.X && current.Y != prev.Y)
                return DiffStatus.Equal;
            else if (current.X != prev.X)
                return DiffStatus.Deleted;
            else if (current.Y != prev.Y)
                return DiffStatus.Inserted;
            else
                throw new Exception();
        }

        public static IEnumerable<DiffResult<T>> Order<T>(IEnumerable<DiffResult<T>> results, DiffOrderType orderType)
        {
            var resultArray = results.ToArray();

            for (int i = 0; i < resultArray.Length; i++)
            {
                if (resultArray[i].Status == DiffStatus.Deleted)
                {
                    // Move any deleted items up in the array if they match the previous unchanged item.
                    // This ensures that deletions are placed before their matching unchanged items.
                    while (i - 1 >= 0)
                    {
                        if (resultArray[i - 1].Status == DiffStatus.Equal && resultArray[i].Obj1.Equals(resultArray[i - 1].Obj1))
                        {
                            (resultArray[i - 1], resultArray[i]) = (resultArray[i], resultArray[i - 1]);
                            i--;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Move inserted lines above any matching unchanged lines
                    while (i + 2 < resultArray.Length)
                    {
                        if (resultArray[i + 1].Status == DiffStatus.Equal &&
                            resultArray[i + 2].Status == DiffStatus.Inserted &&
                            resultArray[i + 1].Obj1 != null &&
                            resultArray[i + 1].Obj1.Equals( resultArray[i + 2].Obj2))
                        {
                            (resultArray[i + 2], resultArray[i + 1]) = (resultArray[i + 1], resultArray[i + 2]);
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            var resultQueue = new Queue<DiffResult<T>>(resultArray);
            var additionQueue = new Queue<DiffResult<T>>();
            var deletionQueue = new Queue<DiffResult<T>>();

            while (resultQueue.Count != 0)
            {
                if (resultQueue.Peek().Status == DiffStatus.Equal)
                {
                    yield return resultQueue.Dequeue();
                    continue;
                }

                while (resultQueue.Count != 0 && resultQueue.Peek().Status != DiffStatus.Equal)
                {
                    while (resultQueue.Count != 0 && resultQueue.Peek().Status == DiffStatus.Inserted)
                    {
                        additionQueue.Enqueue(resultQueue.Dequeue());
                    }

                    while (resultQueue.Count != 0 && resultQueue.Peek().Status == DiffStatus.Deleted)
                    {
                        deletionQueue.Enqueue(resultQueue.Dequeue());
                    }
                }

                var latestReturnStatus = DiffStatus.Equal;
                while (true)
                {
                    if (additionQueue.Count != 0 && deletionQueue.Count == 0)
                    {
                        yield return additionQueue.Dequeue();
                    }
                    else if (additionQueue.Count == 0 && deletionQueue.Count != 0)
                    {
                        yield return deletionQueue.Dequeue();
                    }
                    else if (additionQueue.Count != 0 && deletionQueue.Count != 0)
                    {
                        switch (orderType)
                        {
                            case DiffOrderType.GreedyDeleteFirst:
                                yield return deletionQueue.Dequeue();
                                latestReturnStatus = DiffStatus.Deleted;
                                break;
                            case DiffOrderType.GreedyInsertFirst:
                                yield return additionQueue.Dequeue();
                                latestReturnStatus = DiffStatus.Inserted;
                                break;
                            case DiffOrderType.LazyDeleteFirst:
                                if (latestReturnStatus != DiffStatus.Deleted)
                                {
                                    yield return deletionQueue.Dequeue();
                                    latestReturnStatus = DiffStatus.Deleted;
                                }
                                else
                                {
                                    yield return additionQueue.Dequeue();
                                    latestReturnStatus = DiffStatus.Inserted;
                                }
                                break;
                            case DiffOrderType.LazyInsertFirst:
                                if (latestReturnStatus != DiffStatus.Inserted)
                                {
                                    yield return additionQueue.Dequeue();
                                    latestReturnStatus = DiffStatus.Inserted;
                                }
                                else
                                {
                                    yield return deletionQueue.Dequeue();
                                    latestReturnStatus = DiffStatus.Deleted;
                                }
                                break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }
}
