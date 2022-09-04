<p1>
<h1>Vanilla CFR Demonstration</h1>

This repository is for a simple demonstration of vanilla counterfactual regret minimisation algorithm.
This can be used to find Nash Equilibrium strategies in **post flop** for No Limit Texas Holdem

<h3>Inputs </h3>
Within Program Main you can manually change the following inputs:<br>
-Number of iterations<br>
-Starting Board<br>
-Player 1 Range<br>
-Player 2 Range<br>
-Start pot size<br>
-Effective stack size<br>
-Available bet sizes (as a percentage of the pot)<br><br>
Note: Due to this being designed as a demonstration the code and algorithm are very slow, and will take a long time to run through any sizable game tree.<br>

<h3> CLI </h3>
After running all the iterations a primitive CLI will be available to view the strategies from the algorithm

Action Key:<br>
C0 = Check or Call dependant on context<br>
F0 = Fold<br>
R0 = Raise by amount of bet size 1<br>
R1 = Raise by amount of bet size 2<br>
R2 = etc.<br>
S0 = All in<br>

Card Key:<br>
Card Rank: 2-9,T,J,Q,K,A<br>
Card Suit: s=spades, d=diamonds, h=hearts, c=clubs<br>

Output will be of the form:<br>
[history] [Stategy]<br>

EXAMPLE: A history of "Ad_As_2d_3s_5c_R0_R0"<br>
Will output the strategy of player 1 who has a hand of Ace diamonds, Ace spades after a 2d,3s,5c flop and after raising and then being re-raised<br>
The strategy is [0.8,0.2] for the options C0, R0. This means the strategy is check 80% and raise 20%
</p1>

