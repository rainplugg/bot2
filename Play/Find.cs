namespace Play
{
   public class Find
   {
      public long id { get; set; }
      public long rival { get; set; }
      public Find(long id, long rival)
      {
         this.id = id;
         this.rival = rival;
      }
   }
}
