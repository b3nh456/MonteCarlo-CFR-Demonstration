using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPokerSolver
{
    public static class Helpers
    {
        public static List<T[]> DeepCopyListofArray<T>(List<T[]> ListList)
        {
            List<T[]> ListList2 = new List<T[]>();

            for (int i = 0; i < ListList.Count; i++)
            {
                ListList2.Add((T[])ListList[i].Clone());
            }

            return ListList2;
        }

        public static List<float> Normalise(List<float> list)
        {
            float sum = list.Sum();
            if (sum > 0)
            {
                return list.Select(e => e / sum).ToList();
            }
            else
            {
                return list.Select(e=> (float)1/list.Count).ToList();
            }

        }
        public static float[] Normalise(float[] array)
        {
            float sum = array.Sum();
            if (sum > 0)
            {
                return array.Select(e => e / sum).ToArray();
            }
            else
            {
                return array.Select(e => (float)1 / array.Count()).ToArray();
            }

        }

    }

    public enum Player
    {
        Player1,
        Player2,
        ChanceForP1,
        ChanceForP2,
        ChancePublic,
        GameEnd
    }

    public static class PlayerAction
    {
        public static List<string> Options { get; set; } = new List<string>();
        public static Dictionary<string, float> RaiseSizes { get; set; } = new Dictionary<string, float>();

        //MUST BE INITIATED FIRST
        public static void SetPlayerActions(List<float> raiseSizes)
        {
            if (raiseSizes.Count > 10)
            {
                throw new ArgumentException("Too many raise sizes");
            }

            Options.Clear();
            RaiseSizes.Clear();

            Options.Add("F0");
            Options.Add("C0");

            for(int i = 0; i < raiseSizes.Count; i++)
            {
                string key = 'R' + $"{i}";
                Options.Add(key);
                RaiseSizes[key] = raiseSizes[i];
            }

        }

        public static bool IsStringAPlayerAction(string historyElement)
        {
            return PlayerAction.Options.Contains(historyElement);
            
        }
    }

    public static class PokerRules
    {
        public static float StartingPot { get; set; } = new float();
        public static float EffectiveStacks { get; set; } = new float();

        public static void SetStart(float startPot, float startStacks)
        {
            StartingPot = startPot;
            EffectiveStacks = startStacks;
        }

        public static Player GetActivePlayer(List<string> history)
        {
            //List<string> totalHistory = StartingCards.Concat(history.Where(h => Card.IsStringACard(h) == true)).ToList();
            bool roundResolved = false;

            //IF FOLD -> game ended
            if (history.Last() == "F0")
            {
                return Player.GameEnd;
            }
            //IF ALL IN AND CALL -> round is resolved
            int shoveIndex = history.IndexOf("S0");
            if (shoveIndex != -1 && shoveIndex < history.Count - 1 && history[shoveIndex + 1] == "C0")
            {
                roundResolved = true;
            }
            //IF PLAYER HAS CALLED A PLAYER ACTION -> round is resolved
            else if (history.Last() == "C0" && PlayerAction.IsStringAPlayerAction(history[history.Count - 2]) == true)
            {
                roundResolved=true;
            }

            if (roundResolved)
            {
                int cardCount = history.Where(h => Card.IsStringACard(h) == true).Count();

                if (cardCount == 5)
                {
                    return Player.GameEnd;
                }
                else if (cardCount < 5)
                {
                    return Player.ChancePublic;
                }
                else
                {
                    throw new Exception("SHould never be here");
                }
            }
            //If game has not ended and round is not resolved, player actions determined from actions since last card
            else
            {
                int actionsSinceLastCard = history.Count - 1 - history.LastIndexOf(history.Last(h => Card.IsStringACard(h) == true));
                if (actionsSinceLastCard % 2 == 0)
                {
                    return Player.Player1;
                }
                else
                {
                    return Player.Player2;
                }
            }

        }

        public static List<string> AvailableChanceActions(List<string> history, List<string> p1Cards = null, List<string> p2Cards = null)
        {
            List<string> usedCards = history.Where(h => Card.IsStringACard(h) == true).ToList();
            if (p1Cards != null)
            {
                usedCards.AddRange(p1Cards);
            }
            if (p2Cards != null)
            {
                usedCards.AddRange(p2Cards);
            }
            usedCards = Card.ArrangeCards(usedCards);

            List<string> availableCards = Card.AllCardsArray.ToList();

            //HOW IT WORKS:
            //available cards are in order, used cards are in order
            //so goes through available cards backwards and removed each used card in order (this means only need one loop through available cards)
            int usedCardIndex = usedCards.Count - 1;
            int cardIndex = availableCards.Count - 1;
            while (usedCardIndex >= 0)
            {
                if (availableCards[cardIndex] == usedCards[usedCardIndex])
                {
                    availableCards.RemoveAt(cardIndex);
                    usedCardIndex--;
                }

                cardIndex--;
            }

            return availableCards;
        }

        public static List<string> AvailablePlayerActions(List<string> history)
        {
            //This will create a copy of the list so its not by reference
            //This only creates a shallow copy, but this is fine because the list elements are value type
            List<string> availableActions = new List<string>(PlayerAction.Options);

            if (history.Last() == "C0" || Card.IsStringACard(history.Last()) == true || history.Count == 0)
            {
                availableActions.Remove("F0");
            }

            //Get pot size as if we were to call, because thats what raise sizes are based on
            List<string> historyAndCall = new List<string>(history);
            historyAndCall.Add("C0");
            float ifCallpotSize = GetPotSize(historyAndCall);
            float remainingStack = EffectiveStacks - (ifCallpotSize - StartingPot) / 2 ;
            bool allInOption = false;
            int removeRaisesIndex = 0;
            if (remainingStack < 0)
            {
                throw new Exception("This should never be triggered cause should cause a terminal node situation");
            }

            //If last action is a shove, only optinos are call or fold
            if (history.Last() == "S0")
            {
                return availableActions.GetRange(0, 2);
            }

            for(int i = 0; i < availableActions.Count; i++)
            {

                if (availableActions[i][0] == 'R' && remainingStack <= ifCallpotSize * PlayerAction.RaiseSizes[availableActions[i]])
                {
                    allInOption = true;
                    break;
                }

                removeRaisesIndex++;
            }
            if (allInOption)
            {
                //Remove raise options that are larger than the remaining stack
                availableActions.RemoveRange(removeRaisesIndex, availableActions.Count-removeRaisesIndex);

                //Create "Shove" All in Option
                PlayerAction.RaiseSizes["S0"] = remainingStack/ifCallpotSize;
                availableActions.Add("S0");
            }

            //TO DO: some way to remove raise sizes that are larger than an all in option
            return availableActions;
        }

        //Payoffs calculated from Player 1 perspective
        public static float CalculatePayoff(List<string> history, List<string> p1Cards, List<string> p2Cards)
        {
            float potSize = GetPotSize(history);
            float potChangeFromStart = potSize - StartingPot;

            List<Card> board = history.Where((h) => Card.IsStringACard(h) == true).Select(h => Card.StringToCard(h)).ToList();
            List<Card> Player1Cards = p1Cards.Select(h => Card.StringToCard(h)).ToList();
            List<Card> Player2Cards = p2Cards.Select(h => Card.StringToCard(h)).ToList();


            int winner;
            if (history.Last() == "F0")
            {
                int actionsSinceLastCard = history.Count - 1 - history.LastIndexOf(history.Last(h => Card.IsStringACard(h) == true));
                winner = actionsSinceLastCard % 2 + 1;

            }
            else
            {
                winner = HandRanker.DetermineWinner(board, Player1Cards, Player2Cards);
            }

            switch (winner)
            {
                //Split pot
                case 0:
                    return StartingPot/2;
                //Player 1 Wins
                case 1:
                    return StartingPot + potChangeFromStart/2;
                //Player 2 Wins
                case 2:
                    return -potChangeFromStart/2;
                default:
                    throw new Exception("Should not be here");
            }
        }


        //returns -1 for no fold, 1 for player 1, 2 for player 2
        public static Player WinnerByFold(List<string> history)
        {
            if (history.Last() == "F0")
            {
                int actionsSinceLastCard = history.Count;
                for (int i = history.Count - 1; i >= 0; i--)
                {
                    if (Card.IsStringACard(history[i]))
                    {
                        actionsSinceLastCard = history.Count - 1 - i;
                        break;
                    }
                }
                //int actionsSinceLastCard = history.Count - 1 - history.LastIndexOf(history.Last(h => Card.IsStringACard(h) == true));
                return (Player)(actionsSinceLastCard % 2);
            }
            return Player.GameEnd;
        }

        public static float GetPotSize(List<string> history)
        {
            float potSize = PokerRules.StartingPot;
            float unmatchedBet = 0;

            for(int i = 0; i < history.Count; i++)
            {
                string action = history[i];

                if (action[0] == 'S')
                {
                    if (unmatchedBet == 0)
                    {
                        unmatchedBet = potSize * PlayerAction.RaiseSizes[action];
                    }
                    else //unmatched bet != 0
                    {
                        potSize = potSize + unmatchedBet * 2;
                        unmatchedBet = potSize * PlayerAction.RaiseSizes[action];
                    }
                }
                if (action[0] == 'R')
                {
                    if (unmatchedBet == 0)
                    {
                        unmatchedBet = potSize * PlayerAction.RaiseSizes[action];
                    }
                    else //unmatched bet != 0
                    {
                        potSize = potSize + unmatchedBet * 2;
                        unmatchedBet = potSize * PlayerAction.RaiseSizes[action];
                    }
                }
                else if (action[0] == 'C')
                {
                    potSize = potSize + unmatchedBet * 2;
                    unmatchedBet = 0;
                }
            }

            return potSize;
        }

    }


}
