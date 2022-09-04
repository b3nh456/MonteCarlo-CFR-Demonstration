using System;
using System.Collections.Generic;

namespace MyPokerSolver
{
    internal class Program
    {
        static void Main(string[] args)
        {

            //Game Inputs
            //TO DO: Add Command line inputs?
            int numIterations = 1000000;
            List<string> board = new List<string>() { "2d", "2s", "2c", "2h", "3s"};
            List<string> player1Range = new List<string>() { "AA", "KK", "QQ"};
            List<string> player2Range = new List<string>() { "AA", "KK", "QQ"};
            int startPotSize = 50;
            int effectiveStackSize = 100;
            List<float> availableBetSizes = new List<float>() { (float)0.5};

            //set pot size, effective stacks and player actions
            PokerRules.SetStart(startPotSize, effectiveStackSize);
            PlayerAction.SetPlayerActions(availableBetSizes);


            //arrange board and hand combos
            List<string> boardArranged = Card.ArrangeCards(board);
            List<string[]> P1HandCombos = new List<string[]>();
            List<string[]> P2HandCombos = new List<string[]>();
            foreach (string hand in player1Range)
            {
                P1HandCombos.AddRange(Card.GetArrangedHandCombos(hand));
            }
            foreach (string hand in player2Range)
            {
                P2HandCombos.AddRange(Card.GetArrangedHandCombos(hand));
            }

            //create CFRTrainer object
            VanillaCFRTrainer trainer = new VanillaCFRTrainer();

            //Train Trainer
            float P1Util = trainer.Train(numIterations, boardArranged, P1HandCombos, P2HandCombos);

            Console.WriteLine($"Player 1 Utility: {P1Util} Player 2 Utility: {startPotSize-P1Util}");

            InfoSetUI.View(trainer, board.Count);

        }
    }
}
