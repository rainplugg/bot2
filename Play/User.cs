namespace Play
{
   public class User
   {
      public string id { get; set; }
      public string nickname { get; set; }
      public int balance { get; set; }
      public string message { get; set; }
      public User(string id, string nickname, int balance, string message)
      {
         this.id = id;
         this.nickname = nickname;
         this.balance = balance;
         this.message = message;
      }
   }
}
