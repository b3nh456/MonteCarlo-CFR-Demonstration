using System;
using System.Linq;

namespace MyPokerSolver
{

    public class InformationSet
    {
        public float[] CFRegretSum;
        public float[] StrategySum;

        public InformationSet(int NumberOfActions)
        {
            //Initialise float arrays to required length and set elements of array to zero
            this.CFRegretSum = new float[NumberOfActions];
            this.StrategySum = new float[NumberOfActions];
        }
    }

    public class InformationSetCFRLogic
    {
        public float[] GetStrategy(InformationSet infoSet)
        {
            float[] PosRegrets = infoSet.CFRegretSum.Select(e => Math.Max(0, e)).ToArray();
            return Helpers.Normalise(PosRegrets);
        }

        public void AddToStrategySum(InformationSet infoSet, float[] strategy, float activePlayerReachProbability)
        {
            infoSet.StrategySum = infoSet.StrategySum.Zip(strategy, (x, y) => x + y * activePlayerReachProbability).ToArray();
        }

        public float[] GetFinalStrategy(InformationSet infoSet)
        {
            return Helpers.Normalise(infoSet.StrategySum);
        }

        public void AddToCumulativeRegrets
            (InformationSet infoSet, GameStateNode gameStateNode, float[] actionUtilities, float nodeUtility)
        {
            if (gameStateNode.ActivePlayer == Player.Player1)
            {
                float counterFactualReachProbability = gameStateNode.ReachProbabiltyP2;

                for (int i = 0; i < actionUtilities.Length; i++)
                {
                    infoSet.CFRegretSum[i] = infoSet.CFRegretSum[i] + counterFactualReachProbability * (actionUtilities[i] - nodeUtility);
                }

            }
            else if (gameStateNode.ActivePlayer == Player.Player2)
            {
                float counterFactualReachProbability = gameStateNode.ReachProbabiltyP1;

                for (int i = 0; i < actionUtilities.Length; i++)
                {
                    infoSet.CFRegretSum[i] = infoSet.CFRegretSum[i] + counterFactualReachProbability * (nodeUtility - actionUtilities[i]);
                }
            }
        }
    }
}