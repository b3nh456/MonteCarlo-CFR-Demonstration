using System;
using System.Collections.Generic;
using System.Linq;

namespace MyPokerSolver
{
    public enum Rank
    {
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King,
        Ace,
    }
    public enum Suit
    {
        Heart,
        Club,
        Diamond,
        Spade,
    }

    public class Card
    {

        public Rank Rank { get; set; }
        public Suit Suit { get; set; }

        //Constructor creates card from rank and suit
        public Card(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }
        //constructor creates a random card
        public Card()
        {
            var rnd = new Random();
            Rank = (Rank)rnd.Next(Enum.GetValues(typeof(Rank)).Length);
            Suit = (Suit)rnd.Next(Enum.GetValues(typeof(Suit)).Length);
        }
        //Is card higher rank than a comparison card
        private bool IsHigherThan(Card comparisonCard)
        {
            if (Rank > comparisonCard.Rank)
            {
                return true;
            }
            if (Rank < comparisonCard.Rank)
            {
                return false;
            }
            if (Suit > comparisonCard.Suit)
            {
                return true;
            }
            return false;
        }



        //Dictionaries used for converting between Characters to Rank or Suit enum
        readonly static Dictionary<char, Rank> CharToRank = new Dictionary<char, Rank>()
        {
            { '2', Rank.Two },
            { '3', Rank.Three },
            { '4', Rank.Four },
            { '5', Rank.Five },
            { '6', Rank.Six },
            { '7', Rank.Seven },
            { '8', Rank.Eight },
            { '9', Rank.Nine },
            { 'T', Rank.Ten },
            { 'J', Rank.Jack },
            { 'Q', Rank.Queen },
            { 'K', Rank.King },
            { 'A', Rank.Ace }
        };
        readonly static Dictionary<Rank, char> RankToChar = new Dictionary<Rank, char>()
        {
            {Rank.Two,'2' },
            {Rank.Three,'3' },
            {Rank.Four ,'4'},
            {Rank.Five ,'5'},
            {Rank.Six ,'6'},
            {Rank.Seven ,'7'},
            {Rank.Eight ,'8'},
            {Rank.Nine ,'9'},
            {Rank.Ten ,'T'},
            {Rank.Jack ,'J'},
            {Rank.Queen ,'Q'},
            {Rank.King ,'K'},
            {Rank.Ace ,'A'}
        };
        readonly static Dictionary<char, Suit> CharToSuit = new Dictionary<char, Suit>()
        {
            { 's', Suit.Spade },
            { 'd', Suit.Diamond },
            { 'c', Suit.Club },
            { 'h', Suit.Heart }
        };
        readonly static Dictionary<Suit, char> SuitToChar = new Dictionary<Suit, char>()
        {
            {Suit.Spade,'s' },
            {Suit.Diamond,'d' },
            {Suit.Club ,'c'},
            {Suit.Heart ,'h'}
        };


        public override string ToString()
        {
            return $"{Card.RankToChar[this.Rank]}{Card.SuitToChar[this.Suit]}";
        }

        public static readonly string[] AllCardsArray = new string[] { "As", "Ad", "Ac", "Ah",  "Ks", "Kd", "Kc", "Kh",  "Qs", "Qd", "Qc", "Qh",
                                                                           "Js", "Jd", "Jc", "Jh",  "Ts" ,"Td" ,"Tc" ,"Th" ,  "9s", "9d", "9c", "9h",
                                                                           "8s", "8d", "8c", "8h",  "7s" ,"7d" ,"7c" ,"7h" ,  "6s", "6d", "6c", "6h",
                                                                           "5s", "5d", "5c", "5h",  "4s" ,"4d" ,"4c" ,"4h" ,  "3s", "3d", "3c", "3h",
                                                                           "2s", "2d", "2c", "2h",};



        public static bool IsStringACard(string historyElement)
        {


            if (historyElement.Length == 2)
            {
                if (Card.RankToChar.Values.Contains(historyElement[0]) && Card.SuitToChar.Values.Contains(historyElement[1]))
                {
                    return true;
                }
            }
            return false;

        }

        //Card from string
        public static Card StringToCard(string historyElement)
        {
            try
            {
                Card card = new Card(Card.CharToRank[historyElement[0]], Card.CharToSuit[historyElement[1]]);
                return card;
            }
            catch
            {
                throw new Exception("Invalid History Element String");
            }

        }

        //Arrange a list of cards using insert sort - small lists so insert sort will be efficient
        public static void ArrangeCards(List<Card> cards)
        {
            //INSERT SORT
            for (int i = 1; i < cards.Count; i++)
            {
                //cards to left of i are sorted so just swaps with each card to its left untill in correct place
                for (int j = i - 1; j >= 0; j--)
                {
                    if (cards[j + 1].IsHigherThan(cards[j]))
                    {
                        Card c = cards[j + 1];
                        cards[j + 1] = cards[j];
                        cards[j] = c;
                    }
                }
            }

        }

        public static List<string> ArrangeCards(List<string> cards)
        {
            List<Card> cardsC = cards.Select(c => Card.StringToCard(c)).ToList();
            Card.ArrangeCards(cardsC);
            cards = cardsC.Select(c => c.ToString()).ToList();
            return cards;
        }


        //returns the list of hand combos (arranged) from a string input code representing a group of cards 
        //   EG AKo is all hand combinations that make Ace King offsuit
        //      AKs is all hand combinations that make Ace King suited
        //      AA is all hand combinations that make pocket Aces
        public static List<string[]> GetArrangedHandCombos(string cardsAbstracted)
        {
            if (Card.RankToChar.Values.Contains(cardsAbstracted[0]) == false || Card.RankToChar.Values.Contains(cardsAbstracted[1]) == false)
            {
                throw new Exception("Inavlid Card Ranking");
            }


            ///// PAIRED CARDS /////
            if (cardsAbstracted.Length == 2 && cardsAbstracted[0] == cardsAbstracted[1])
            {
                List<string[]> cardPairList = new List<string[]>();
                Rank rank = CharToRank[cardsAbstracted[0]];

                for (int i = 3; i >= 1; i--)
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        string[] addCards = new string[2] { new Card(rank, (Suit)i).ToString(), new Card(rank, (Suit)j).ToString() };
                        cardPairList.Add(addCards);
                    }
                }
                return cardPairList;
            }

            ///// SUITED CARDS /////
            if (cardsAbstracted.Length == 3 && cardsAbstracted[2] == 's')
            {
                List<string[]> cardPairList = new List<string[]>();
                Rank rank1 = CharToRank[cardsAbstracted[0]];
                Rank rank2 = CharToRank[cardsAbstracted[1]];

                //Arrange so in correct order
                if (rank2 > rank1)
                {
                    rank1 = CharToRank[cardsAbstracted[1]];
                    rank2 = CharToRank[cardsAbstracted[0]];
                }


                for (int i = 3; i >= 0; i--)
                {
                    string[] addCards = new string[2] { new Card(rank1, (Suit)i).ToString(), new Card(rank2, (Suit)i).ToString() };
                    cardPairList.Add(addCards);
                }
                return cardPairList;
            }

            ///// OFFSUIT CARDS /////
            if (cardsAbstracted.Length == 3 && cardsAbstracted[2] == 'o')
            {
                List<string[]> cardPairList = new List<string[]>();
                Rank rank1 = CharToRank[cardsAbstracted[0]];
                Rank rank2 = CharToRank[cardsAbstracted[1]];

                //Arrange so in correct order
                if (rank2 > rank1)
                {
                    rank1 = CharToRank[cardsAbstracted[1]];
                    rank2 = CharToRank[cardsAbstracted[0]];
                }

                for (int i = 3; i >= 0; i--)
                {
                    for (int j = 3; j >= 0; j--)
                    {
                        if (j == i) { continue; }//skip if suited

                        string[] addCards = new string[2] { new Card(rank1, (Suit)i).ToString(), new Card(rank2, (Suit)j).ToString() };
                        cardPairList.Add(addCards);
                    }
                }
                return cardPairList;
            }


            throw new Exception("Incorrect hand format");
        }

    }
}
