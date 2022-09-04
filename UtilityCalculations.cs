using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MyPokerSolver
{
    //average utility
    public class AverageUtility : UtilityBase
    {
        public AverageUtility(Dictionary<string, InformationSet> infoSetMap, InformationSetCFRLogic updater) : base(infoSetMap, updater)
        {
        }
    }

    //best response utility, uses average utility except on best response player, where highest utility action is taken
    public class BestResponseUtility : UtilityBase
    {
        public BestResponseUtility(Dictionary<string, InformationSet> infoSetMap, InformationSetCFRLogic updater) : base(infoSetMap, updater)
        {
        }

        protected override float GetNodeUtility(Dictionary<string, InformationSet> infoSetMap, BestResponseNode bestResponseNode)
        {
            //===TERMINAL NODE
            if (bestResponseNode.ActivePlayer == Player.GameEnd)
            {
                return GetAverageTerminalNodeUtility(bestResponseNode);
            }

            //===CHANCE NODE
            //GAME STATES STILL NEED TO BE WEIGHTED BY COUNTERFACTUAL PROBABILITY
            if (bestResponseNode.ActivePlayer == Player.ChancePublic)
            {
                return GetAverageChanceNodeUtility(infoSetMap, bestResponseNode);
            }

            //===BEST RESPONSE PLAYER NODE
            if (bestResponseNode.ActivePlayer == bestResponseNode.BestResponsePlayer)
            {
                return GetMaximumBRPlayerNodeUtility(infoSetMap, bestResponseNode);
            }

            //===OPPONENT PLAYER NODE
            else if (bestResponseNode.ActivePlayer == Player.Player1 || bestResponseNode.ActivePlayer == Player.Player2)
            {
                return GetAverageOpponentNodeUtility(infoSetMap, bestResponseNode);
            }


            throw new Exception("should not be here");
        }
    }
    
    //average utility
    public abstract class UtilityBase
    {
        public Stopwatch chanceSW;
        public Stopwatch brSW;
        public Dictionary<string, InformationSet> InfoSetMap { get; set; }
        public InformationSetCFRLogic UpdateInfoSets { get; set; }

        public UtilityBase(Dictionary<string, InformationSet> infoSetMap, InformationSetCFRLogic updateInfoSets)
        {
            InfoSetMap = infoSetMap;
            UpdateInfoSets = updateInfoSets;
            chanceSW = new Stopwatch();
            brSW = new Stopwatch();
        }

        //NOTE: utilities NOT from player 1 perspective, they are from BR player perspective -> needs to be this way as is exploitative so the (utility(p1) + utility(p2)=100) will NOT hold
        public float TotalDeviation(List<string> boardArranged, List<string[]> handCombosP1, List<string[]> handCombosP2)
        {
            brSW.Start();

            float ExploitabilityP1 = UtilityPerPlayer(Player.Player1, boardArranged, handCombosP1, handCombosP2);
            //NOTE: FROM BR PLAYER PERSPECTIVE (ie player 2 here)
            float ExploitabilityP2 = UtilityPerPlayer(Player.Player2, boardArranged, handCombosP1, handCombosP2);

            brSW.Stop();

            return 100 * ((ExploitabilityP1 + ExploitabilityP2) - PokerRules.StartingPot) / PokerRules.StartingPot;
        }

        public float UtilityPerPlayer(Player BRPlayer, List<string> boardArranged, List<string[]> handCombosP1, List<string[]> handCombosP2)
        {
            List<string[]> BRHandCombos;
            List<string[]> OpponentHandCombos;

            if (BRPlayer == Player.Player1)
            {
                BRHandCombos = handCombosP1;
                OpponentHandCombos = handCombosP2;

            }
            else if (BRPlayer == Player.Player2)
            {
                BRHandCombos = handCombosP2;
                OpponentHandCombos = handCombosP1;

            }
            else
            {
                throw new Exception("should not be here");
            }

            float totalUtil = 0;
            int totalCombos = 0;
            for (int indexBR = 0; indexBR < BRHandCombos.Count; indexBR++)
            {
                List<string[]> avilableOppHandCombos = new List<string[]>();

                for (int indexOpp = 0; indexOpp < OpponentHandCombos.Count; indexOpp++)
                {
                    if (OpponentHandCombos[indexOpp][0] == BRHandCombos[indexBR][0] || OpponentHandCombos[indexOpp][0] == BRHandCombos[indexBR][1])
                    {
                        continue;
                    }
                    if (OpponentHandCombos[indexOpp][1] == BRHandCombos[indexBR][0] || OpponentHandCombos[indexOpp][1] == BRHandCombos[indexBR][1])
                    {
                        continue;
                    }

                    avilableOppHandCombos.Add(OpponentHandCombos[indexOpp]);

                    totalCombos++;


                }

                BestResponseNode startNode = new BestResponseNode(BRPlayer, boardArranged, BRHandCombos[indexBR], avilableOppHandCombos);

                //CHANGE
                float util = GetNodeUtility(this.InfoSetMap, startNode);

                //Weighting used relatvie to the amount of combinations
                totalUtil += util * avilableOppHandCombos.Count;
            }

            float avgUtil = totalUtil / totalCombos;

            return avgUtil;
        }


        protected virtual float GetNodeUtility(Dictionary<string, InformationSet> infoSetMap, BestResponseNode bestResponseNode)
        {
            //===TERMINAL NODE
            if (bestResponseNode.ActivePlayer == Player.GameEnd)
            {
                return GetAverageTerminalNodeUtility(bestResponseNode);
            }

            //===CHANCE NODE
            //GAME STATES STILL NEED TO BE WEIGHTED BY COUNTERFACTUAL PROBABILITY
            if (bestResponseNode.ActivePlayer == Player.ChancePublic)
            {
                return GetAverageChanceNodeUtility(infoSetMap, bestResponseNode);
            }

            //===BEST RESPONSE PLAYER NODE
            if (bestResponseNode.ActivePlayer == bestResponseNode.BestResponsePlayer)
            {
                return GetAverageBRPlayerNodeUtility(infoSetMap, bestResponseNode);
            }

            //===OPPONENT PLAYER NODE
            else if (bestResponseNode.ActivePlayer == Player.Player1 || bestResponseNode.ActivePlayer == Player.Player2)
            {
                return GetAverageOpponentNodeUtility(infoSetMap, bestResponseNode);
            }


            throw new Exception("should not be here");
        }


        //TO DO/NOTE: ATM SET TO CURRENT STRATEGY

        protected float GetAverageTerminalNodeUtility(BestResponseNode bestResponseNode)
        {
            //Normalise the counter factual probability vector, so that it is is terms of probability of each game state
            List<float> GameStateProbabilities = Helpers.Normalise(bestResponseNode.CounterFactualProbabilities);


            float avgUtility = 0;

            for (int i = 0; i < GameStateProbabilities.Count; i++)
            {
                if (bestResponseNode.BestResponsePlayer == Player.Player1)
                {
                    avgUtility += GameStateProbabilities[i] * PokerRules.CalculatePayoff(bestResponseNode.History, bestResponseNode.BRPlayerCards.ToList(), bestResponseNode.OpponentCards[i].ToList());
                }
                else if (bestResponseNode.BestResponsePlayer == Player.Player2)
                {
                    //Can NOT just put br cards in calculatepayoffs player 1 paramater, because payoffs are also calculated by folds which only done for player 1 perspective still.
                    var utilityP1 = PokerRules.CalculatePayoff(bestResponseNode.History, bestResponseNode.OpponentCards[i].ToList(), bestResponseNode.BRPlayerCards.ToList());
                    var utilityP2 = PokerRules.StartingPot - utilityP1;
                    avgUtility += GameStateProbabilities[i] * utilityP2;
                }
            }
            if (float.IsNaN(avgUtility))
            {
                throw new Exception("something done fuked up");
            }
            //return utility averaged across all game states (opponent cards)
            return avgUtility;
        }

        protected float GetAverageChanceNodeUtility(Dictionary<string, InformationSet> infoSetMap, BestResponseNode bestResponseNode)
        {
            float[] actionUtilities = new float[bestResponseNode.ActionOptions.Count];

            float[][] actionProbabilities = new float[bestResponseNode.ActionOptions.Count][];

            int totalActions = 48 - bestResponseNode.History.Where(x => Card.IsStringACard(x) == true).Count();

            for (int i = 0; i < actionUtilities.Length; i++)
            {
                string action = bestResponseNode.ActionOptions[i];
                //action probability for all game states is just 1/number of actions
                //Note: do not need to adjust probabilities for conflicting opponent cards and public chance cards because conflicting opponentcards (game states) are removed in constructor


                //technically this also gives a non zero to chance cards that conflict with opponent cards -> however these are removed in the Node Constructor
                float[] probabilities = new float[bestResponseNode.OpponentCards.Count];

                //THe probability of the chance card for each opponent card is 1 divide the number of chance actions that are available for that opponent card
                //so the actions probability is actually independent of the action itself, but is dependent on the game state(opponent cards)
                for (int j = 0; j < bestResponseNode.OpponentCards.Count; j++)
                {

                    if (bestResponseNode.OpponentCards[j].Contains(action))
                    {
                        probabilities[j] = 0;
                    }
                    else
                    {
                        //can kinda think of this as a strat array
                        //TO DO: can just make this 1 and then normalise average strat
                        //leaving for now for readability
                        probabilities[j] = (float)1/ totalActions;
                    }

                }
                actionProbabilities[i] = probabilities;

                //Within constructor game states are removed that conflict with public chance card
                //NOTE/TO DO: can only get away with not passing action probabilities onto CF reach probability of Node because the constuctor removes the conflicting opponent game states(which is basically same as setting their cf prob to zero)
                BestResponseNode childGameState = new BestResponseNode(bestResponseNode, bestResponseNode.ActionOptions[i]);

                actionUtilities[i] = GetNodeUtility(infoSetMap, childGameState);

            }

            List<float> gameStateProbabilities = Helpers.Normalise(bestResponseNode.CounterFactualProbabilities);
            float[] avgStrat = new float[bestResponseNode.ActionOptions.Count];
            for (int i = 0; i < bestResponseNode.ActionOptions.Count; i++)
            {
                avgStrat[i] = actionProbabilities[i].Zip(gameStateProbabilities, (x, y) => x * y).Sum();
            }


            //avg util = avg strat DOT PRODUCT action utilities
            float avgUtil = avgStrat.Zip(actionUtilities, (x, y) => x * y).Sum();

            if (float.IsNaN(avgUtil))
            {
                throw new Exception("something done fuked up");
            }

            return avgUtil;
        }

        protected float GetAverageBRPlayerNodeUtility(Dictionary<string, InformationSet> infoSetMap, BestResponseNode bestResponseNode)
        {
            float[] actionUtilities = new float[bestResponseNode.ActionOptions.Count];
            int maxIndex = 0;

            for (int i = 0; i < actionUtilities.Length; i++)
            {
                //Action probability will just be 1 for BR active player
                List<float> actionProbabilities = bestResponseNode.OpponentCards.Select(x => (float)1).ToList(); //create list of 1s, because this does not add to CF probabilities

                BestResponseNode childNode = new BestResponseNode(bestResponseNode, bestResponseNode.ActionOptions[i], actionProbabilities);
                actionUtilities[i] = GetNodeUtility(infoSetMap, childNode);

                //max index is at max utility
                if (actionUtilities[i] > actionUtilities[maxIndex])
                {
                    maxIndex = i;
                }

            }

            string key = string.Join("_", bestResponseNode.BRPlayerCards.Concat(bestResponseNode.History));
            //////////CHANGED
            var strat = UpdateInfoSets.GetFinalStrategy(infoSetMap[key]);

            float avgUtil = strat.Zip(actionUtilities, (x, y) => x * y).Sum();


            if (float.IsNaN(actionUtilities[maxIndex]))
            {
                throw new Exception("something done fuked up");
            }
            //return max utility
            return avgUtil;
        }

        protected float GetMaximumBRPlayerNodeUtility(Dictionary<string, InformationSet> infoSetMap, BestResponseNode bestResponseNode)
        {
            //WILL MAKE SURE DOESN'T CHOOSE BEST RESPONSE DOWN 0 PROBABILITY NODES-shouldnt be needed for our own info sets?
            string keyI = string.Join("_", bestResponseNode.BRPlayerCards.Concat(bestResponseNode.History));
            float[] strat = UpdateInfoSets.GetFinalStrategy(infoSetMap[keyI]);

            float[] actionUtilities = new float[bestResponseNode.ActionOptions.Count];
            int maxIndex = 0;

            for (int i = 0; i < actionUtilities.Length; i++)
            {
                //Action probability will just be 1 for BR active player
                List<float> actionProbabilities = bestResponseNode.OpponentCards.Select(x => (float)1).ToList(); //create list of 1s, because this does not add to CF probabilities

                BestResponseNode childNode = new BestResponseNode(bestResponseNode, bestResponseNode.ActionOptions[i], actionProbabilities);
                actionUtilities[i] = GetNodeUtility(infoSetMap, childNode);

                //max index is at max utility
                if (actionUtilities[i] > actionUtilities[maxIndex])
                {
                    maxIndex = i;
                }

            }

            string key = string.Join("_", bestResponseNode.BRPlayerCards.Concat(bestResponseNode.History));


            if (float.IsNaN(actionUtilities[maxIndex]))
            {
                throw new Exception("something done fuked up");
            }
            //return max utility
            return actionUtilities[maxIndex];
        }

        protected float GetAverageOpponentNodeUtility(Dictionary<string, InformationSet> infoSetMap, BestResponseNode bestResponseNode)
        {
            //TO DO

            //strategy array - Get strategies for each game state (opponent cards)
            List<float[]> StratArray = new List<float[]>();

            for(int i = 0; i < bestResponseNode.OpponentCards.Count; i++)
            {
                string key = string.Join("_", bestResponseNode.OpponentCards[i].Concat(bestResponseNode.History));
                StratArray.Add(UpdateInfoSets.GetFinalStrategy(infoSetMap[key]));
            }

            //Need action probabilities across all game states to then work out new CF for each game state

            //Get utilities of children nodes
            //--remembering to update CF probs(done on constructor?)
            float[] actionUtilities = new float[bestResponseNode.ActionOptions.Count];
            for (int i = 0; i < actionUtilities.Length; i++)
            {
                //Action Probability is stratArray but only one of the actions
                List<float> actionProbabilities = StratArray.Select(s => s[i]).ToList();

                //The constructor will create updated CF probabilities for the child node
                BestResponseNode childNode = new BestResponseNode(bestResponseNode, bestResponseNode.ActionOptions[i], actionProbabilities);

                actionUtilities[i] = GetNodeUtility(infoSetMap, childNode);
            }

            //Get gamestate(opponent card) probabilties (normalize CF probs)
            List<float> gameStateProbabilities = Helpers.Normalise(bestResponseNode.CounterFactualProbabilities);

            //avg strat =  strategy array DOT PRODUCT game state probabilities
            float[] avgStrat = new float[bestResponseNode.ActionOptions.Count];
            for (int i = 0; i < bestResponseNode.ActionOptions.Count; i++)
            {
                List<float> StratArrayPerAction = StratArray.Select(s => s[i]).ToList();
                avgStrat[i] = StratArrayPerAction.Zip(gameStateProbabilities, (x, y) => x * y).Sum();
            }

            //NOTE: REDUNDANT JUST SEEING IF WE GOT ROUNDING ERRORS
            //avgStrat = Helpers.Normalise(avgStrat.ToList()).ToArray();

            //avg util = avg strat DOT PRODUCT action utilities
            float avgUtil = avgStrat.Zip(actionUtilities, (x, y) => x * y).Sum();

            if (float.IsNaN(avgUtil))
            {
                throw new Exception("something done fuked up");
            }

            return avgUtil;
        }

    }
}



