namespace Play
{
   public class Play
   {
      public int id { get; set; }
      public string player_1 { get; set; }
      public string player_2 { get; set; }
      public string score_1 { get; set; }
      public string score_2 { get; set; }
      public int bet { get; set; }
      public string date { get; set; }
      public string status { get; set; }
      public Play(int id, string player_1, string player_2, string score_1, string score_2, int bet, string date, string status)
      {
         this.id = id;
         this.player_1 = player_1;
         this.player_2 = player_2;
         this.score_1 = score_1;
         this.score_2 = score_2;
         this.bet = bet;
         this.date = date;
         this.status = status;
      }
   }
}
