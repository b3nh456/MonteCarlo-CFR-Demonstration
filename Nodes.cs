using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPokerSolver
{
    public class GameStateNode
    {
        //Node Properties
        public List<string> History { get; set; }
        public List<string> Player1Cards { get; set; }
        public List<string> Player2Cards { get; set; }
        public float ReachProbabiltyP1 { get; set; }
        public float ReachProbabiltyP2 { get; set; }
        public Player ActivePlayer { get; set; }
        public List<string> ActionOptions { get; set; }

        public GameStateNode() { }

        //Create an initial node
        public static GameStateNode GetStartingNode(List<string> history, List<string> player1Cards, List<string> player2Cards)
        {
            GameStateNode newNode = new GameStateNode();

            //Initiate new node properties
            newNode.History = history;
            newNode.Player1Cards = player1Cards;
            newNode.Player2Cards = player2Cards;
            newNode.ReachProbabiltyP1 = 1;
            newNode.ReachProbabiltyP2 = 1;
            newNode.ActivePlayer = PokerRules.GetActivePlayer(newNode.History);

            //Set Action Options
            if (newNode.ActivePlayer == Player.Player1 || newNode.ActivePlayer == Player.Player2)
            {
                newNode.ActionOptions = PokerRules.AvailablePlayerActions(newNode.History);
            }
            else if (newNode.ActivePlayer == Player.ChancePublic)
            {
                newNode.ActionOptions = PokerRules.AvailableChanceActions(newNode.History, newNode.Player1Cards, newNode.Player2Cards);
            }

            return newNode;
        }

        //Constructor for creating child node from parent node
        public GameStateNode(GameStateNode parentGameState, string action, float actionProbability)
        {
            //Initiate node properties
            this.History = new List<string>(parentGameState.History);
            this.History.Add(action);
            this.Player1Cards = new List<string>(parentGameState.Player1Cards);
            this.Player2Cards = new List<string>(parentGameState.Player2Cards);
            this.ActivePlayer = PokerRules.GetActivePlayer(this.History);
            this.ReachProbabiltyP1 = parentGameState.ReachProbabiltyP1;
            this.ReachProbabiltyP2 = parentGameState.ReachProbabiltyP2;

            //Update Reach Probabilities
            if (parentGameState.ActivePlayer == Player.Player1)
            {
                this.ReachProbabiltyP1 = parentGameState.ReachProbabiltyP1 * actionProbability;
            }
            else if (parentGameState.ActivePlayer == Player.Player2)
            {
                this.ReachProbabiltyP2 = parentGameState.ReachProbabiltyP2 * actionProbability;
            }

            //Set Action Options
            if (this.ActivePlayer == Player.Player1 || this.ActivePlayer == Player.Player2)
            {
                this.ActionOptions = PokerRules.AvailablePlayerActions(History);
            }
            else if (this.ActivePlayer == Player.ChancePublic)
            {
                this.ActionOptions = PokerRules.AvailableChanceActions(History, Player1Cards, Player2Cards);
            }
        }
    }



    //Node type used for Best Response calulations
    public class BestResponseNode
    {
        public List<string> History { get; set; } //This wont include any player cards
        public string[] BRPlayerCards { get; set; }
        public List<string[]> OpponentCards { get; set; } //Opponents cards (game states)


        public Player BestResponsePlayer { get; set; }//Contains node type information
        public Player ActivePlayer { get; set; }//Contains node type information

        public List<float> CounterFactualProbabilities { get; set; } //Counter Factual prob for each game state(opp cards)

        public List<string> ActionOptions { get; set; }

        //Constructor used to create first node
        public BestResponseNode(Player bestResponsePlayer, List<string> history, string[] bRPlayerCards, List<string[]> opponentCards)
        {
            BestResponsePlayer = bestResponsePlayer;
            this.History = new List<string>(history);
            this.BRPlayerCards = (string[])bRPlayerCards.Clone();
            this.OpponentCards = Helpers.DeepCopyListofArray(opponentCards);

            CounterFactualProbabilities = new List<float>(new float[opponentCards.Count]);//create list of correct length filled with zeros
            CounterFactualProbabilities = CounterFactualProbabilities.Select(x => (float)1).ToList(); //make all values equal to 1
            ActivePlayer = PokerRules.GetActivePlayer(History);
            //Set Action Options
            if (this.ActivePlayer == Player.Player1 || this.ActivePlayer == Player.Player2)
            {
                this.ActionOptions = PokerRules.AvailablePlayerActions(History);
            }
            else if (this.ActivePlayer == Player.ChancePublic)
            {
                this.ActionOptions = PokerRules.AvailableChanceActions(History, BRPlayerCards.ToList());
            }
        }

        //constructor used to create child nodes
        public BestResponseNode(BestResponseNode parentNode, string action, List<float> actionProbabilities = null)
        {
            BestResponsePlayer = parentNode.BestResponsePlayer;
            //Set Node History
            this.History = new List<string>(parentNode.History);
            this.History.Add(action);
            this.BRPlayerCards = (string[])parentNode.BRPlayerCards.Clone();
            this.OpponentCards = Helpers.DeepCopyListofArray(parentNode.OpponentCards);

            //Set Node Active Player - if active player is game end dont need to bother setting rest of properties
            this.ActivePlayer = PokerRules.GetActivePlayer(this.History);

            //Set Reach Probabilities

            //Best response player
            if (parentNode.ActivePlayer == parentNode.BestResponsePlayer)
            {
                //CF probabilities remain the same
                this.CounterFactualProbabilities = new List<float>(parentNode.CounterFactualProbabilities);
            }
            //Oppponent player
            else if (parentNode.ActivePlayer == Player.Player2 || parentNode.ActivePlayer == Player.Player1)
            {
                this.CounterFactualProbabilities = new List<float>(parentNode.CounterFactualProbabilities).Zip(actionProbabilities, (x, y) => x * y).ToList();
            }
            else if (parentNode.ActivePlayer == Player.ChancePublic)
            {
                //technically should update CF probabilities, BUT is same probability for all of them, so dont need to
                //(NOTE: CAN ONLY DO THIS ON BR - not with vector form cfr)
                this.CounterFactualProbabilities = new List<float>(parentNode.CounterFactualProbabilities);

                //Remove opponent cards that are no longer possible
                List<int> removeIndexes = new List<int>();
                for (int i = 0; i < this.OpponentCards.Count; i++)
                {
                    if (OpponentCards[i][0] == action || OpponentCards[i][1] == action)
                    {
                        removeIndexes.Add(i);
                    }
                }
                for (int i = removeIndexes.Count - 1; i >= 0; i--)
                {
                    int removeIndex = removeIndexes[i];
                    OpponentCards.RemoveAt(removeIndex);
                    CounterFactualProbabilities.RemoveAt(removeIndex);
                }
            }

            //Set Action Options
            if (this.ActivePlayer == Player.Player1 || this.ActivePlayer == Player.Player2)
            {
                this.ActionOptions = PokerRules.AvailablePlayerActions(History);
            }
            else if (this.ActivePlayer == Player.ChancePublic)
            {
                this.ActionOptions = PokerRules.AvailableChanceActions(History, BRPlayerCards.ToList());
            }
        }
    }
}
