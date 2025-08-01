﻿using SharedEntities.Models;

namespace MatchMaker.Server.Domain.PairMatchingService
{
    public class PairMatchingService
    {
        private readonly List<Fighter> _fighters;
        private readonly Fighter _instructor = null;
        private readonly int? _howManyDifferentPairs;
        private int _pairCounter = 0;
        private readonly HashSet<Tuple<int, int>> _pairHistory = new();
        public PairMatchingService(List<Fighter> students, Fighter instructor)
        {
            _fighters = students;
            _instructor = instructor;
        }

        public PairMatchingService(List<Fighter> students, Fighter instructor, int howManyDifferentPairs)
        {
            _fighters = students;
            _instructor = instructor;
            _howManyDifferentPairs = howManyDifferentPairs;
        }

        public List<Tuple<Fighter, Fighter>> GenerateNonUniquePairs()
        {
            List<Fighter> remainingFighters = new List<Fighter>(_fighters);
            List<Tuple<Fighter, Fighter>> pairs = new List<Tuple<Fighter, Fighter>>();

            if (_fighters.Count % 2 != 0)
            {
                Tuple<Fighter, Fighter> instructorPair = PairUpOddFighter(_instructor);
                pairs.Add(instructorPair);
                remainingFighters.Remove(instructorPair.Item2);
            }

            while (remainingFighters.Count > 0)
            {
                Tuple<Fighter, Fighter> pair = FindNextMatchingPairWithoutHistory(remainingFighters);
                if (pair != null)
                {
                    pairs.Add(pair);
                    remainingFighters.Remove(pair.Item1);
                    remainingFighters.Remove(pair.Item2);
                }
                else
                {
                    break;
                }
            }

            return pairs;
        }

        public IEnumerable<List<Tuple<Fighter, Fighter>>> GenerateFighterPairsWithUniquePairs()
        {
            while (_howManyDifferentPairs != null || _pairCounter < _howManyDifferentPairs)
            {
                var pairs = GenerateNextFighterPairs();
                if (pairs.Any())
                {
                    _pairCounter++;
                    yield return pairs;
                }
                else
                {
                    break;
                }
            }
        }

        public List<Tuple<Fighter, Fighter>> GenerateNextFighterPairs()
        {
            List<Fighter> remainingFighters = new List<Fighter>(_fighters);
            List<Tuple<Fighter, Fighter>> pairs = new List<Tuple<Fighter, Fighter>>();

            if (_fighters.Count % 2 != 0)
            {
                // If there is an odd number of fighters in the list, create a new fighter of FighterRole.Instructor role and pair up with one of them.
                Tuple<Fighter, Fighter> instructorPair = PairUpOddFighter(_instructor);
                pairs.Add(instructorPair);
                remainingFighters.Remove(instructorPair.Item2);
            }

            while (remainingFighters.Count > 0)
            {
                // Find the closest matching pair among the remaining fighters and add it to the list of pairs.
                Tuple<Fighter, Fighter> pair = FindNextMatchingPair(remainingFighters);
                if (pair != null)
                {
                    pairs.Add(pair);
                    remainingFighters.Remove(pair.Item1);
                    remainingFighters.Remove(pair.Item2);
                }
                else
                {
                    break;
                }
            }

            return pairs;
        }

        public Tuple<Fighter, Fighter> FindNextMatchingPair(List<Fighter> remainingFighters)
        {
            double closestDifference = double.MaxValue;
            Tuple<Fighter, Fighter> bestPair = null;

            for (int i = 0; i < remainingFighters.Count; i++)
            {
                for (int j = i + 1; j < remainingFighters.Count; j++)
                {
                    // check if the pair is not already in the history
                    var f1 = remainingFighters[i];
                    var f2 = remainingFighters[j];
                    if (!_pairHistory.Contains(Tuple.Create(f1.Id, f2.Id)) &&
                        !_pairHistory.Contains(Tuple.Create(f2.Id, f1.Id)))
                    {
                        double currentDifference = ComputeDifference(f1, f2);

                        if (currentDifference < closestDifference)
                        {
                            closestDifference = currentDifference;
                            bestPair = Tuple.Create(f1, f2);
                        }
                    }
                }
            }

            // add the new pair to the history
            if (bestPair != null)
            {
                _pairHistory.Add(Tuple.Create(bestPair.Item1.Id, bestPair.Item2.Id)); // Record the pair in history
            }

            return bestPair;
        }

        private Tuple<Fighter, Fighter> PairUpOddFighter(Fighter instructor)
        {
            var bestFighter = FindTheHighestRankFighter();
            return new Tuple<Fighter, Fighter>(instructor, bestFighter);
        }

        private Fighter FindTheHighestRankFighter()
        {
            if (_fighters == null || !_fighters.Any())
            {
                throw new InvalidOperationException("No fighters available to find the highest rank.");
            }

            Fighter highestRankFighter = _fighters[0];

            for (int i = 1; i < _fighters.Count; i++)
            {
                Fighter currentFighter = _fighters[i];

                int beltComparison = CompareTo(highestRankFighter.BelkRank, currentFighter.BelkRank);

                if (beltComparison < 0)
                {
                    // Current fighter has a higher belt rank than highest ranking fighter. Update highest ranking fighter.
                    highestRankFighter = currentFighter;
                }
                else if (beltComparison == 0)
                {

                    // Current fighter has an equal belt rank as highest ranking fighter. Compare max workout duration and BMI.

                    if (currentFighter.MaxWorkoutDuration > highestRankFighter.MaxWorkoutDuration)
                    {

                        // Current fighter has a higher max workout duration than highest ranking fighter. Update highest ranking fighter.

                        highestRankFighter = currentFighter;

                    }
                    else if (currentFighter.MaxWorkoutDuration == highestRankFighter.MaxWorkoutDuration)
                    {

                        // Current fighter has an equal max workout duration as highest ranking fighter. Compare BMI.

                        if (currentFighter.BMI > highestRankFighter.BMI)
                        {

                            // Current fighter has a higher BMI than highest ranking fighter. Update highest ranking fighter.

                            highestRankFighter = currentFighter;

                        }

                    }

                }

            }

            return highestRankFighter;
        }

        //add a method to compare two belt ranks based on their color and stripe. Return -1 if this rank is lower than the other rank, 
        // return +1 if this rank is higher than the other rank, return zero if they are equal.
        private static double ComputeDifference(Fighter f1, Fighter f2)
        {
            return
                Math.Abs(f1.Weight - f2.Weight) +
                Math.Abs(f1.BMI - f2.BMI) +
                Math.Abs(f1.MaxWorkoutDuration - f2.MaxWorkoutDuration);
        }

        private static int CompareTo(BeltColor source, BeltColor other)
        {
            // Define an order of colors from lowest to highest as white, blue, purple, brown, black.
            string[] colors = { "white", "blue", "purple", "brown", "black" };

            // Find the index of this color and the other color in the array.
            int thisIndex = Array.IndexOf(colors, source.ToString().ToLower());
            int otherIndex = Array.IndexOf(colors, source.ToString().ToLower());

            if (thisIndex < otherIndex)
            {
                // This color is lower than the other color.
                return -1;
            }

            if (thisIndex > otherIndex)
            {
                // This color is higher than the other color.
                return +1;
            }

            // The colors are equal, Compare the stripes if any.
            /*if (source.Stripe < other.Stripe)
            {
                // This stripe is lower than the other stripe.
                return -1;
            }
            if (source.Stripe > other.Stripe)
            {
                // This stripe is higher than the other stripe.
                return +1;
            }*/

            //The belt ranks are equal.
            return 0;
        }


        private Tuple<Fighter, Fighter> FindNextMatchingPairWithoutHistory(List<Fighter> remainingFighters)
        {
            double closestDifference = double.MaxValue;
            Tuple<Fighter, Fighter> bestPair = null;

            for (int i = 0; i < remainingFighters.Count; i++)
            {
                for (int j = i + 1; j < remainingFighters.Count; j++)
                {
                    var f1 = remainingFighters[i];
                    var f2 = remainingFighters[j];
                    double currentDifference = ComputeDifference(f1, f2);

                    if (currentDifference < closestDifference)
                    {
                        closestDifference = currentDifference;
                        bestPair = Tuple.Create(f1, f2);
                    }
                }
            }

            return bestPair;
        }
    }
}
