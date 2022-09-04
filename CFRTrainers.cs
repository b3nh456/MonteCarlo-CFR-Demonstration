using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScottPlot;


namespace MyPokerSolver
{
    public class VanillaCFRTrainer
    {
        public Dictionary<string, InformationSet> InfoSetMap { get; set; }
        public BestResponseUtility BestResponseUtility { get; set; }
        public InformationSetCFRLogic InformationSetMethods { get; set; }
        public int Iteration { get; set; }
        public Player UpdatingPlayer { get; set; }
        Random rand = new Random();


        //Constructor Initialises the InfoSetMap
        public VanillaCFRTrainer()
        {
            InfoSetMap = new Dictionary<string, InformationSet>();
        }

        public InformationSet GetInformationSet(GameStateNode gameStateNode)
        {
            List<string> infoSetHistory = new List<string>(gameStateNode.History);

            if (gameStateNode.ActivePlayer == Player.Player1)
            {
                infoSetHistory.InsertRange(0, gameStateNode.Player1Cards);
            }
            else if (gameStateNode.ActivePlayer == Player.Player2)
            {
                infoSetHistory.InsertRange(0, gameStateNode.Player2Cards);
            }

            string key = string.Join("_", infoSetHistory);

            if (InfoSetMap.ContainsKey(key) == false)
            {
                InfoSetMap[key] = new InformationSet(gameStateNode.ActionOptions.Count);
            }

            return InfoSetMap[key];
        }


        public float Train(int numberIterations, List<string> boardArranged, List<string[]> handCombosP1, List<string[]> handCombosP2)
        {
            InformationSetMethods = new InformationSetCFRLogic();
            BestResponseUtility = new BestResponseUtility(InfoSetMap, InformationSetMethods);
            Iteration = 0;
            float P1Util = 0;
            int utilP1Count = 0;
            List<string> startHistory = new List<string>(boardArranged);

            InitialiseInfoSetMap(boardArranged, handCombosP1, handCombosP2);


            for (int i = 0; i < numberIterations; i++)
            {
                Iteration = i;
                UpdatingPlayer = (Player)(i % 2);


SampleCards:
                int indexP1 = rand.Next(0, handCombosP1.Count);
                int indexP2 = rand.Next(0, handCombosP2.Count);

                //Dont include p1 hands that conflict with the board
                if (boardArranged.Contains(handCombosP2[indexP1][0]) || boardArranged.Contains(handCombosP2[indexP1][1]))
                {
                    goto SampleCards;
                }
                //Dont include p2 hands that conflict with curren p1 hands
                if (handCombosP2[indexP2].Contains(handCombosP1[indexP1][0]) || handCombosP2[indexP2].Contains(handCombosP1[indexP1][1]))
                {
                    goto SampleCards;
                }
                //Dont include p2 hands that conflict with the board
                if (boardArranged.Contains(handCombosP2[indexP2][0]) || boardArranged.Contains(handCombosP2[indexP2][1]))
                {
                    goto SampleCards;
                }

                //Initialise startNode
                GameStateNode startNode = GameStateNode.GetStartingNode(startHistory, handCombosP1[indexP1].ToList(), handCombosP2[indexP2].ToList());

                //Begin the CFR Recursion
                P1Util += CalculateNodeUtilityMC(startNode);
                utilP1Count++;

                if (i % 15000 == 0)
                {
                    Console.WriteLine($"Iteration MC {i} complete.");
                    Console.WriteLine($"Strategy Exploitability Percentage MC: {BestResponseUtility.TotalDeviation(boardArranged, handCombosP1, handCombosP2)}");
                    Console.WriteLine();
                }




            }

            //return averge player 1 utility
            return P1Util / utilP1Count;
        }

        //Returns utility from player 1 perspective
        public float CalculateNodeUtilityMC(GameStateNode gameStateNode)
        {

            ///// TERMINAL NODE /////
            if (gameStateNode.ActivePlayer == Player.GameEnd)
            {

                var u = PokerRules.CalculatePayoff(gameStateNode.History, gameStateNode.Player1Cards, gameStateNode.Player2Cards);

                return u;
            }


            float[] actionUtilities = new float[gameStateNode.ActionOptions.Count];
            float nodeUtility;

            ///// CHANCE NODE /////
            if (gameStateNode.ActivePlayer == Player.ChancePublic)
            {
                int choice = rand.Next(gameStateNode.ActionOptions.Count);
                float actionProbability = (float)1 / gameStateNode.ActionOptions.Count;

                GameStateNode childGameState = new GameStateNode(gameStateNode, gameStateNode.ActionOptions[choice], actionProbability);
                nodeUtility = CalculateNodeUtilityMC(childGameState);

                return nodeUtility;
            }

            ///// DECISION NODE /////
            else
            {
                float activePlayerReachProbability;

                if (gameStateNode.ActivePlayer == Player.Player1)
                {
                    activePlayerReachProbability = gameStateNode.ReachProbabiltyP1;
                }
                else //ActivePlayer == Player2
                {
                    activePlayerReachProbability = gameStateNode.ReachProbabiltyP2;
                }

                InformationSet infoSet = GetInformationSet(gameStateNode);
                var strategy = InformationSetMethods.GetStrategy(infoSet);

                //Updating Player Decision Node
                if (gameStateNode.ActivePlayer == UpdatingPlayer)
                {
                    InformationSetMethods.AddToStrategySum(infoSet, strategy, activePlayerReachProbability);

                    //get utility of each action
                    for (int i = 0; i < actionUtilities.Length; i++)
                    {
                        var actionProbability = strategy[i];
                        GameStateNode childGameState = new GameStateNode(gameStateNode, gameStateNode.ActionOptions[i], actionProbability);
                        actionUtilities[i] = CalculateNodeUtilityMC(childGameState);
                    }

                    //average utility for node calculated by Dot product action utilities and action probabilities
                    nodeUtility = actionUtilities.Zip(strategy, (x, y) => x * y).Sum();

                    InformationSetMethods.AddToCumulativeRegrets(infoSet, gameStateNode, actionUtilities, nodeUtility);
                }
                //Opponent Decision Node
                else
                {
                    int strategyOption = RandomActionSelection(strategy);
                    GameStateNode childGameState = new GameStateNode(gameStateNode, gameStateNode.ActionOptions[strategyOption], strategy[strategyOption]);
                    nodeUtility = CalculateNodeUtilityMC(childGameState);
                }

                return nodeUtility;
            }
        }

        //Returns the index of the action, chosen randomly based on its probability of occuring
        int RandomActionSelection(float[] strategy)
        {
            int intMultiplier = 100000;
            int[] actionWeightings = strategy.Select(x => (int)(x * intMultiplier)).ToArray();
            int choice = rand.Next(intMultiplier);
            int nextChoiceCutOff = 0;
            for (int i = 0; i < actionWeightings.Length; i++)
            {
                nextChoiceCutOff += actionWeightings[i];
                if (choice <= nextChoiceCutOff)
                {
                    return i;
                }
            }
            return -1;
        }


        public void InitialiseInfoSetMap(List<string> boardArranged, List<string[]> handCombosP1, List<string[]> handCombosP2)
        {
            //Iterate through all possible hand combinations
            for (int indexP1 = 0; indexP1 < handCombosP1.Count; indexP1++)
            {
                //Dont include p1 hands that conflict with the board
                if (boardArranged.Contains(handCombosP2[indexP1][0]) || boardArranged.Contains(handCombosP2[indexP1][1]))
                {
                    continue;
                }

                for (int indexP2 = 0; indexP2 < handCombosP2.Count; indexP2++)
                {
                    //Dont include p2 hands that conflict with curren p1 hands
                    if (handCombosP2[indexP2].Contains(handCombosP1[indexP1][0]) || handCombosP2[indexP2].Contains(handCombosP1[indexP1][1]))
                    {
                        continue;
                    }
                    //Dont include p2 hands that conflict with the board
                    if (boardArranged.Contains(handCombosP2[indexP2][0]) || boardArranged.Contains(handCombosP2[indexP2][1]))
                    {
                        continue;
                    }

                    //Initialise startNode
                    GameStateNode startNode = GameStateNode.GetStartingNode(boardArranged, handCombosP1[indexP1].ToList(), handCombosP2[indexP2].ToList());
                    InitialiseInfoSetMapPropogation(startNode);

                }
            }
        }
        private void InitialiseInfoSetMapPropogation(GameStateNode gameStateNode)
        {

            ///// TERMINAL NODE /////
            if (gameStateNode.ActivePlayer == Player.GameEnd)
            {
                return;
            }


            ///// CHANCE NODE /////
            if (gameStateNode.ActivePlayer == Player.ChancePublic)
            {
                //get utility of each action
                for (int i = 0; i < gameStateNode.ActionOptions.Count; i++)
                {
                    GameStateNode childGameState = new GameStateNode(gameStateNode, gameStateNode.ActionOptions[i], 1);
                    InitialiseInfoSetMapPropogation(childGameState);
                }

                return;
            }

            ///// DECISION NODE /////
            else
            {
                GetInformationSet(gameStateNode);

                //get utility of each action
                for (int i = 0; i < gameStateNode.ActionOptions.Count; i++)
                {
                    GameStateNode childGameState = new GameStateNode(gameStateNode, gameStateNode.ActionOptions[i], 1);
                    InitialiseInfoSetMapPropogation(childGameState);
                }
                return;
            }
        }


    }
}

