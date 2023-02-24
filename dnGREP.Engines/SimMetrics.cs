/*
 * The original .NET implementation of the SimMetrics library is taken from the Java
 * source and converted to NET using the Microsoft Java converter.
 * It is notclear who made the initial convertion to .NET.
 * 
 * This updated version has started with the 1.0 .NET release of SimMetrics and used
 * FxCop (http://www.gotdotnet.com/team/fxcop/) to highlight areas where changes needed 
 * to be made to the converted code.
 * 
 * this version with updates Copyright (c) 2006 Chris Parkinson.
 * 
 * For any queries on the .NET version please contact me through the 
 * sourceforge web address.
 * 
 * SimMetrics - SimMetrics is a java library of Similarity or Distance
 * Metrics, e.g. Levenshtein Distance, that provide float based similarity
 * measures between string Data. All metrics return consistant measures
 * rather than unbounded similarity scores.
 *
 * Copyright (C) 2005 Sam Chapman - Open Source Release v1.1
 *
 * Please Feel free to contact me about this library, I would appreciate
 * knowing quickly what you wish to use it for and any criticisms/comments
 * upon the SimMetric library.
 *
 * email:       s.chapman@dcs.shef.ac.uk
 * www:         http://www.dcs.shef.ac.uk/~sam/
 * www:         http://www.dcs.shef.ac.uk/~sam/stringmetrics.html
 *
 * address:     Sam Chapman,
 *              Department of Computer Science,
 *              University of Sheffield,
 *              Sheffield,
 *              S. Yorks,
 *              S1 4DP
 *              United Kingdom,
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the
 * Free Software Foundation; either version 2 of the License, or (at your
 * option) any later version.
 *
 * This program is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License
 * for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */

using System;

namespace dnGREP.Engines
{
    /// <summary>
    /// needlemanwunch implements an edit distance function
    /// </summary>
    [Serializable]
    sealed public class NeedlemanWunch : AbstractStringMetric
    {
        const double defaultGapCost = 2.0;
        const double defaultMismatchScore = 0.0;
        const double defaultPerfectMatchScore = 1.0;

        /// <summary>
        /// constructor
        /// </summary>
        public NeedlemanWunch() : this(defaultGapCost, new SubCostRange0To1()) { }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="costG">the cost of a gap</param>
        public NeedlemanWunch(double costG) : this(costG, new SubCostRange0To1()) { }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="costG">the cost of a gap</param>
        /// <param name="costFunction">the cost function to use</param>
        public NeedlemanWunch(double costG, AbstractSubstitutionCost costFunction)
        {
            gapCost = costG;
            dCostFunction = costFunction;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="costFunction">the cost function to use</param>
        public NeedlemanWunch(AbstractSubstitutionCost costFunction) : this(defaultGapCost, costFunction) { }

        /// <summary>
        /// the private cost function used in the levenstein distance.
        /// </summary>
        AbstractSubstitutionCost dCostFunction;

        /// <summary>
        /// a constant for calculating the estimated timing cost.
        /// </summary>
        readonly double estimatedTimingConstant = 0.0001842F;
        /// <summary>
        /// the cost of a gap.
        /// </summary>
        double gapCost;

        /// <summary>
        /// gets the similarity of the two strings using Needleman Wunch distance.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>a value between 0-1 of the similarity</returns>
        public override double GetSimilarity(string firstWord, string secondWord)
        {
            if ((firstWord != null) && (secondWord != null))
            {
                double needlemanWunch = GetUnnormalisedSimilarity(firstWord, secondWord);
                double maxValue = Math.Max(firstWord.Length, secondWord.Length);
                double minValue = maxValue;
                if (dCostFunction.MaxCost > gapCost)
                {
                    maxValue *= dCostFunction.MaxCost;
                }
                else
                {
                    maxValue *= gapCost;
                }
                if (dCostFunction.MinCost < gapCost)
                {
                    minValue *= dCostFunction.MinCost;
                }
                else
                {
                    minValue *= gapCost;
                }
                if (minValue < defaultMismatchScore)
                {
                    maxValue -= minValue;
                    needlemanWunch -= minValue;
                }
                if (maxValue == defaultMismatchScore)
                {
                    return defaultPerfectMatchScore;
                }
                else
                {
                    return defaultPerfectMatchScore - needlemanWunch / maxValue;
                }
            }
            return defaultMismatchScore;
        }

        /// <summary> gets a div class xhtml similarity explaining the operation of the metric.</summary>
        /// <param name="firstWord">string 1</param>
        /// <param name="secondWord">string 2</param>
        /// <returns> a div class html section detailing the metric operation.</returns>
        public override string GetSimilarityExplained(string firstWord, string secondWord)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// gets the estimated time in milliseconds it takes to perform a similarity timing.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>the estimated time in milliseconds taken to perform the similarity measure</returns>
        public override double GetSimilarityTimingEstimated(string firstWord, string secondWord)
        {
            if ((firstWord != null) && (secondWord != null))
            {
                double firstLength = firstWord.Length;
                double secondLength = secondWord.Length;
                return firstLength * secondLength * estimatedTimingConstant;
            }
            return defaultMismatchScore;
        }

        /// <summary> 
        /// gets the un-normalised similarity measure of the metric for the given strings.</summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns> returns the score of the similarity measure (un-normalised)</returns>
        public override double GetUnnormalisedSimilarity(string firstWord, string secondWord)
        {
            if ((firstWord != null) && (secondWord != null))
            {
                int n = firstWord.Length;
                int m = secondWord.Length;
                if (n == 0)
                {
                    return m;
                }
                if (m == 0)
                {
                    return n;
                }
                double[][] d = new double[n + 1][];
                for (int i = 0; i < n + 1; i++)
                {
                    d[i] = new double[m + 1];
                }
                for (int i = 0; i <= n; i++)
                {
                    d[i][0] = i;
                }

                for (int j = 0; j <= m; j++)
                {
                    d[0][j] = j;
                }

                for (int i = 1; i <= n; i++)
                {
                    for (int j = 1; j <= m; j++)
                    {
                        double cost = dCostFunction.GetCost(firstWord, i - 1, secondWord, j - 1);
                        d[i][j] = MathFunctions.MinOf3(d[i - 1][j] + gapCost, d[i][j - 1] + gapCost, d[i - 1][j - 1] + cost);
                    }
                }

                return d[n][m];
            }
            return 0.0;
        }

        /// <summary>
        /// set/get the d(i,j) cost function.
        /// </summary>
        public AbstractSubstitutionCost DCostFunction { get { return dCostFunction; } set { dCostFunction = value; } }

        /// <summary>
        /// sets/gets the gap cost for the distance function.
        /// </summary>
        public double GapCost { get { return gapCost; } set { gapCost = value; } }

        /// <summary>
        /// returns the long string identifier for the metric.
        /// </summary>
        public override string LongDescriptionString
        {
            get
            {
                return
                    "Implements the Needleman-Wunch algorithm providing an edit distance based similarity measure between two strings";
            }
        }

        /// <summary>
        /// returns the string identifier for the metric.
        /// </summary>
        public override string ShortDescriptionString { get { return "NeedlemanWunch"; } }
    }

    /// <summary>
    /// base class which all metrics inherit from.
    /// </summary>
    /// <remarks>This class implemented a few basic methods and then leaves the others to
    /// be implemented by the similarity metric itself.</remarks>
    [Serializable]
    abstract public class AbstractStringMetric : IStringMetric
    {
        /// <summary>
        /// does a batch comparison of the set of strings with the given
        /// comparator string returning an array of results equal in length
        /// to the size of the given set of strings to test.
        /// </summary>
        /// <param name="setRenamed">an array of strings to test against the comparator string</param>
        /// <param name="comparator">the comparator string to test the array against</param>
        /// <returns>an array of results equal in length to the size of the given set of strings to test.</returns>
        public double[]? BatchCompareSet(string[] setRenamed, string comparator)
        {
            if ((setRenamed != null) && (comparator != null))
            {
                double[] results = new double[setRenamed.Length];
                for (int strNum = 0; strNum < setRenamed.Length; strNum++)
                {
                    results[strNum] = GetSimilarity(setRenamed[strNum], comparator);
                }
                return results;
            }
            return null;
        }

        /// <summary>
        /// does a batch comparison of one set of strings against another set
        /// of strings returning an array of results equal in length
        /// to the minimum size of the given sets of strings to test.
        /// </summary>
        /// <param name="firstSet">an array of strings to test</param>
        /// <param name="secondSet">an array of strings to test the first array against</param>
        /// <returns>an array of results equal in length to the minimum size of the given sets of strings to test.</returns>
        public double[]? BatchCompareSets(string[] firstSet, string[] secondSet)
        {
            if ((firstSet != null) && (secondSet != null))
            {
                double[] results;
                if (firstSet.Length <= secondSet.Length)
                {
                    results = new double[firstSet.Length];
                }
                else
                {
                    results = new double[secondSet.Length];
                }
                for (int strNum = 0; strNum < results.Length; strNum++)
                {
                    results[strNum] = GetSimilarity(firstSet[strNum], secondSet[strNum]);
                }
                return results;
            }
            return null;
        }

        /// <summary>
        /// gets the similarity measure of the metric for the given strings.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>implemented version will return score between 0 and 1</returns>
        abstract public double GetSimilarity(string firstWord, string secondWord);

        /// <summary> gets a div class xhtml similarity explaining the operation of the metric.</summary>
        /// <param name="firstWord">string 1</param>
        /// <param name="secondWord">string 2</param>
        /// <returns> a div class html section detailing the metric operation.</returns>
        abstract public string GetSimilarityExplained(string firstWord, string secondWord);

        /// <summary>
        /// gets the actual time in milliseconds it takes to perform a similarity timing.
        /// This call takes as long as the similarity metric to perform so should not be done in normal circumstances.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>the actual time in milliseconds taken to perform the similarity measure</returns>
        public long GetSimilarityTimingActual(string firstWord, string secondWord)
        {
            long timeBefore = (DateTime.Now.Ticks - 621355968000000000) / 10000;
            GetSimilarity(firstWord, secondWord);
            long timeAfter = (DateTime.Now.Ticks - 621355968000000000) / 10000;
            return timeAfter - timeBefore;
        }

        /// <summary>
        /// gets the estimated time in milliseconds it takes to perform a similarity timing.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>the estimated time in milliseconds taken to perform the similarity measure</returns>
        abstract public double GetSimilarityTimingEstimated(string firstWord, string secondWord);

        /// <summary> 
        /// gets the un-normalised similarity measure of the metric for the given strings.</summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns> returns the score of the similarity measure (un-normalised)</returns>
        abstract public double GetUnnormalisedSimilarity(string firstWord, string secondWord);

        /// <summary>
        /// reports the metric type.
        /// </summary>
        abstract public string LongDescriptionString { get; }

        /// <summary>
        /// reports the metric type.
        /// </summary>
        abstract public string ShortDescriptionString { get; }
    }

    /// <summary>
    /// implements an interface for the string metrics
    /// </summary>
    public interface IStringMetric
    {
        /// <summary>
        /// returns a similarity measure of the string comparison.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>a double between zero to one (zero = no similarity, one = matching strings)</returns>
        double GetSimilarity(string firstWord, string secondWord);

        /// <summary> gets a div class xhtml similarity explaining the operation of the metric.
        /// 
        /// </summary>
        /// <param name="firstWord">string 1
        /// </param>
        /// <param name="secondWord">string 2
        /// 
        /// </param>
        /// <returns> a div class html section detailing the metric operation.
        /// </returns>
        string GetSimilarityExplained(string firstWord, string secondWord);

        /// <summary>
        /// gets the actual time in milliseconds it takes to perform a similarity timing.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>the actual time in milliseconds taken to perform the similarity measure</returns>
        /// <remarks>This call takes as long as the similarity metric to perform so should not be done in normal cercumstances.</remarks>
        long GetSimilarityTimingActual(string firstWord, string secondWord);

        /// <summary>
        /// gets the estimated time in milliseconds it takes to perform a similarity timing.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns>the estimated time in milliseconds taken to perform the similarity measure</returns>
        double GetSimilarityTimingEstimated(string firstWord, string secondWord);

        /// <summary> 
        /// gets the un-normalised similarity measure of the metric for the given strings.
        /// </summary>
        /// <param name="firstWord"></param>
        /// <param name="secondWord"></param>
        /// <returns> returns the score of the similarity measure (un-normalised)</returns>
        double GetUnnormalisedSimilarity(string firstWord, string secondWord);

        /// <summary>
        /// returns a long string of the string metric description.
        /// </summary>
        string LongDescriptionString { get; }

        /// <summary>
        /// returns a string of the string metric name.
        /// </summary>
        string ShortDescriptionString { get; }
    }

    static public class MathFunctions
    {
        /// <summary>
        /// returns the max of three numbers.
        /// </summary>
        /// <param name="firstNumber">first number to test</param>
        /// <param name="secondNumber">second number to test</param>
        /// <param name="thirdNumber">third number to test</param>
        /// <returns>the max of three numbers.</returns>
        static public double MaxOf3(double firstNumber, double secondNumber, double thirdNumber)
        {
            return Math.Max(firstNumber, Math.Max(secondNumber, thirdNumber));
        }

        /// <summary>
        /// returns the max of three numbers.
        /// </summary>
        /// <param name="firstNumber">first number to test</param>
        /// <param name="secondNumber">second number to test</param>
        /// <param name="thirdNumber">third number to test</param>
        /// <returns>the max of three numbers.</returns>
        static public int MaxOf3(int firstNumber, int secondNumber, int thirdNumber)
        {
            return Math.Max(firstNumber, Math.Max(secondNumber, thirdNumber));
        }

        /// <summary>
        /// returns the max of four numbers.
        /// </summary>
        /// <param name="firstNumber">first number to test</param>
        /// <param name="secondNumber">second number to test</param>
        /// <param name="thirdNumber">third number to test</param>
        /// <param name="fourthNumber">fourth number to test</param>
        /// <returns>the max of four numbers.</returns>
        static public double MaxOf4(double firstNumber, double secondNumber, double thirdNumber, double fourthNumber)
        {
            return Math.Max(Math.Max(firstNumber, secondNumber), Math.Max(thirdNumber, fourthNumber));
        }

        /// <summary>
        /// returns the min of three numbers.
        /// </summary>
        /// <param name="firstNumber">first number to test</param>
        /// <param name="secondNumber">second number to test</param>
        /// <param name="thirdNumber">third number to test</param>
        /// <returns>the min of three numbers.</returns>
        static public double MinOf3(double firstNumber, double secondNumber, double thirdNumber)
        {
            return Math.Min(firstNumber, Math.Min(secondNumber, thirdNumber));
        }

        /// <summary>
        /// returns the min of three numbers.
        /// </summary>
        /// <param name="firstNumber">first number to test</param>
        /// <param name="secondNumber">second number to test</param>
        /// <param name="thirdNumber">third number to test</param>
        /// <returns>the min of three numbers.</returns>
        static public int MinOf3(int firstNumber, int secondNumber, int thirdNumber)
        {
            return Math.Min(firstNumber, Math.Min(secondNumber, thirdNumber));
        }
    }

    /// <summary>
    /// implements a substitution cost function where d(i,j) = 1 if idoes not equal j, 0 if i equals j.
    /// </summary>
    [Serializable]
    sealed public class SubCostRange0To1 : AbstractSubstitutionCost
    {
        const int charExactMatchScore = 1;
        const int charMismatchMatchScore = 0;

        /// <summary>
        /// get cost between characters where d(i,j) = 1 if i does not equals j, 0 if i equals j.
        /// </summary>
        /// <param name="firstWord">the string1 to evaluate the cost</param>
        /// <param name="firstWordIndex">the index within the string1 to test</param>
        /// <param name="secondWord">the string2 to evaluate the cost</param>
        /// <param name="secondWordIndex">the index within the string2 to test</param>
        /// <returns>the cost of a given subsitution d(i,j) where d(i,j) = 1 if i!=j, 0 if i==j</returns>
        public override double GetCost(string firstWord, int firstWordIndex, string secondWord, int secondWordIndex)
        {
            if ((firstWord != null) && (secondWord != null))
            {
                return firstWord[firstWordIndex] != secondWord[secondWordIndex] ? charExactMatchScore : charMismatchMatchScore;
            }
            return 0.0;
        }

        /// <summary>
        /// returns the maximum possible cost.
        /// </summary>
        public override double MaxCost { get { return charExactMatchScore; } }

        /// <summary>
        /// returns the minimum possible cost.
        /// </summary>
        public override double MinCost { get { return charMismatchMatchScore; } }

        /// <summary>
        /// returns the name of the cost function.
        /// </summary>
        public override string ShortDescriptionString { get { return "SubCostRange0To1"; } }
    }

    /// <summary>
    /// AbstractSubstitutionCost implements a abstract class for substiution costs
    /// </summary>
    [Serializable]
    abstract public class AbstractSubstitutionCost : ISubstitutionCost
    {
        /// <summary>
        /// get cost between characters.
        /// </summary>
        /// <param name="firstWord">the firstWord to evaluate the cost</param>
        /// <param name="firstWordIndex">the index within the firstWord to test</param>
        /// <param name="secondWord">the secondWord to evaluate the cost</param>
        /// <param name="secondWordIndex">the index within the string2 to test</param>
        /// <returns></returns>
        abstract public double GetCost(string firstWord, int firstWordIndex, string secondWord, int secondWordIndex);

        /// <summary>
        /// returns the maximum possible cost.
        /// </summary>
        abstract public double MaxCost { get; }

        /// <summary>
        /// returns the minimum possible cost.
        /// </summary>
        abstract public double MinCost { get; }

        /// <summary>
        /// returns the name of the cost function.
        /// </summary>
        abstract public string ShortDescriptionString { get; }
    }

    /// <summary>
    /// is an interface for a cost function d(i,j).
    /// </summary>
    public interface ISubstitutionCost
    {
        /// <summary>
        /// get cost between characters.
        /// </summary>
        /// <param name="firstWord">the firstWord to evaluate the cost</param>
        /// <param name="firstWordIndex">the index within the firstWord to test</param>
        /// <param name="secondWord">the secondWord to evaluate the cost</param>
        /// <param name="secondWordIndex">the index within the secondWord to test</param>
        /// <returns></returns>
        double GetCost(string firstWord, int firstWordIndex, string secondWord, int secondWordIndex);

        /// <summary>
        /// returns the maximum possible cost.
        /// </summary>
        double MaxCost { get; }

        /// <summary>
        /// returns the minimum possible cost.
        /// </summary>
        double MinCost { get; }

        /// <summary>
        /// returns the name of the cost function.
        /// </summary>
        string ShortDescriptionString { get; }
    }
}
