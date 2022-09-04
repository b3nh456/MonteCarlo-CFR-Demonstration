using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyPokerSolver
{
    public static class HandRanker
    {
        /// <summary>
        /// 
        /// Hand Rankings
        /// 9 - Straight Flush
        /// 8 - Four of a Kind
        /// 7 - Full House
        /// 6 - Flush
        /// 5 - Straight
        /// 4 - Trips
        /// 3 - 2 Pair
        /// 2 - 1 Pair
        /// 1 - High Card
        /// 
        /// </summary>
        
        public enum HandRankName
        {
            Error,
            HighCard,
            Onepair,
            TwoPair,
            Trips,
            Straight,
            Flush,
            FullHouse,
            Quads,
            StraightFlush
        }

        public static int DetermineWinner(List<Card> board, List<Card> player1WholeCards, List<Card> player2WholeCards)
        {
            if (board.Count != 5)
            {
                throw new Exception("Board does not contain 5 cards");
            }
            if (player1WholeCards.Count != 2 )
            {
                throw new Exception("Player 1 does not have 2 cards");
            }
            if (player2WholeCards.Count != 2)
            {
                throw new Exception("Player 2 does not have 2 cards");
            }

            return DetermineWinner(board.Concat(player1WholeCards).ToList(), board.Concat(player2WholeCards).ToList());
        }

        //  Returns: 0 - Split Pot,  1 - Player 1 wins,  2 - Player 2 wins
        private static int DetermineWinner(List<Card> Player1Cards, List<Card> Player2Cards)
        {
            var HandRankP1 = GetHandRank(Player1Cards);
            var HandRankP2 = GetHandRank(Player2Cards);

            for (int i = 0; i < 6; i++)
            {
                if (HandRankP1[i] > HandRankP2[i])
                {
                    return 1;
                }
                if (HandRankP1[i] < HandRankP2[i])
                {
                    return 2;
                }
            }

            return 0;
        }

        public static int DetermineWinner(int[] HandRankP1, int[] HandRankP2)
        {
            for (int i = 0; i < 6; i++)
            {
                if (HandRankP1[i] > HandRankP2[i])
                {
                    return 1;
                }
                if (HandRankP1[i] < HandRankP2[i])
                {
                    return 2;
                }
            }

            return 0;
        }


        // Returns a list of ints where the lower indexes ints are of higher importance, and a higher int indicates a higher hand rank
        // first int is hand rank, next 5 ints are highest cards of hand in order from highest to lowest
        public static int[] GetHandRank(List<Card> cards)
        {
            if (cards.Count != 7)
            {
                throw new Exception("Hand must contain seven cards");
            }

            //Ordering makes it easier to work with
            //Pairs and trips will be added in rank order, meaning know that matchedIndexes["Pairs"][0] will rank higher than [1]
            cards = cards.OrderByDescending(c => c.Rank).ToList();

            int handRank;
            List<int>? cardIndexesUsed = new List<int>();

            //Calculate what is withint cards
            List<int>? flushIndexes = FindFlush(cards);
            List<int>? straightIndexes = FindStraight(cards);
            Dictionary<string, List<List<int>>> matchedIndexes = FindMatchedRanks(cards);

            ///// STRAIGHT FLUSH /////
            //// TO DO : THIS WILL NOT WORK
            //      -CANT COMPARE LISTS
            //      -FLUSH AND STRAIGHT INDEXES DONT NECESARILY LINE UP IN STRAIGHT FLUSH
            if (flushIndexes != null && flushIndexes == straightIndexes)
            {
                handRank = 9;
                cardIndexesUsed = flushIndexes.OrderByDescending(c => cards[c].Rank).ToList();
            }
            /////   QUADS   /////
            else if (matchedIndexes["Quads"].Count == 1)
            {
                handRank = 8;
                cardIndexesUsed = matchedIndexes["Quads"][0];
            }
            /////   FULL HOUSE   /////
            else if (matchedIndexes["Trips"].Count >= 1 && matchedIndexes["Pairs"].Count >= 1)
            {
                handRank = 7;
                cardIndexesUsed = matchedIndexes["Trips"][0];
                cardIndexesUsed.AddRange(matchedIndexes["Pairs"][0]);

            }
            /////   FLUSH   /////
            else if (flushIndexes != null)
            {
                handRank = 6;
                cardIndexesUsed = flushIndexes;
            }
            /////   STRAIGHT   /////
            else if (straightIndexes != null)
            {
                handRank = 5;
                cardIndexesUsed = straightIndexes;
            }
            /////   TRIPS   /////
            else if (matchedIndexes["Trips"].Count >= 1)
            {
                handRank = 4;
                cardIndexesUsed = matchedIndexes["Trips"][0];
            }
            /////  TWO PAIR  /////
            else if (matchedIndexes["Pairs"].Count >= 2)
            {
                handRank = 3;
                cardIndexesUsed = matchedIndexes["Pairs"][0];
                cardIndexesUsed.AddRange(matchedIndexes["Pairs"][1]);
            }
            /////  ONE PAIR  /////
            else if (matchedIndexes["Pairs"].Count == 1)
            {
                handRank = 2;
                cardIndexesUsed = matchedIndexes["Pairs"][0];
            }
            /////  HIGH CARD  /////
            else { handRank = 1; }


            //Iterates from highest rank due to already being ordered
            for (int i = 0; i < cards.Count; i++)
            {
                //Break when got the 5 cards weve used
                if (cardIndexesUsed.Count == 5) { break; }
                //Dont add cards already used
                if (cardIndexesUsed.Contains(i)) { continue; }

                cardIndexesUsed.Add(i);
            }

            int[] cardsRank = new int[6];
            cardsRank[0] = handRank;
            for (int i = 0; i < 5; i++)
            {
                cardsRank[i+1] = (int)cards[cardIndexesUsed[i]].Rank;
            }

            return cardsRank;

        }

        private static List<int> FindFlush(List<Card> cards)
        {
            List<int> flushCards = new List<int>();
            int totalCount = 0;

            for (int suit = 0; suit < 4; suit++)
            {
                if (totalCount > 2)
                {
                    break;
                }

                flushCards.Clear();

                for (int i = 0; i < cards.Count; i++)
                {
                    if ((Suit)suit == cards[i].Suit)
                    {
                        totalCount++;
                        flushCards.Add(i);
                        if (flushCards.Count == 5)
                        {
                            return flushCards;
                        }
                    }
                }
            }
            return null;
        }

        private static List<int> FindStraight(List<Card> cards)
        {
            List<int> straightCards = new List<int>();


            for (int i = 0; i < cards.Count; i++)
            {
                //if no cards in straight cards list or if matches previous card
                if (straightCards.Count == 0 || cards[i].Rank == cards[i - 1].Rank - 1)
                {
                    straightCards.Add(i);
                }
                //if cards are same still possibility of straight so just continue without adding
                else if (cards[i].Rank == cards[i - 1].Rank)
                {

                }
                else
                {
                    if (i > 3)
                    {
                        break;
                    }
                    straightCards.Clear();
                    straightCards.Add(i);
                }
            }

            if (straightCards.Count < 4)
            {
                return null;
            }
            if (straightCards.Count > 4)
            {
                return straightCards.GetRange(0, 5);
            }
            else//4 straight cards - checks edge case with straight going 5 4 3 2 A
            {
                if (straightCards[0] > 3 || cards[straightCards[0]].Rank != Rank.Five || cards[0].Rank != Rank.Ace)
                {
                    return null;
                }
                straightCards.Add(0);
                return straightCards.GetRange(0, 5);
            }

        }

        //Returnings a dictionary with keys "Pairs" "Trips" and "Quads", each dictionary value is a list whos elements is a list of the indexes of the occuring matched cards
        private static Dictionary<string, List<List<int>>> FindMatchedRanks(List<Card> cards)
        {
            Dictionary<string, List<List<int>>> matchingCards = new Dictionary<string, List<List<int>>>()
            {
                { "Pairs", new List<List<int>>() },
                { "Trips", new List<List<int>>() },
                { "Quads", new List<List<int>>() }

            };

            //skip indexes weve already added

            int[] skipIndexes = new int[cards.Count];


            for (int i = 0; i < cards.Count; i++)
            {
                //skip indexes weve already added
                //if (skipIndexes.Contains(i)) { continue; }
                if (skipIndexes[i] == -1) { continue; }


                List<int> matchIndexes = new List<int>();
                matchIndexes.Add(i);
                //Look forward through rest of cards in list
                for (int j = i + 1; j < cards.Count; j++)
                {
                    if (cards[j].Rank == cards[i].Rank)
                    {
                        matchIndexes.Add(j);
                        skipIndexes[j] = -1;
                    }
                }
                if (matchIndexes.Count == 2)
                {
                    matchingCards["Pairs"].Add(matchIndexes);
                }
                else if (matchIndexes.Count == 3)
                {
                    matchingCards["Trips"].Add(matchIndexes);
                }
                else if (matchIndexes.Count == 4)
                {
                    matchingCards["Quads"].Add(matchIndexes);
                }

            }
            return matchingCards;
        }

    }
}
