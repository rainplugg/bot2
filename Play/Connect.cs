using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace Play
{
   public class Connect
   {
      public static SQLiteDataReader Query(string str)
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|play.db");
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection);
         try {
            SQLiteConnection.Open();
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
            return reader;
         } catch { return null; }
      }

      public static void LoadUser(List<User> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `User`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new User(
                     query.GetValue(0).ToString(),
                     query.GetValue(1).ToString(),
                     Convert.ToInt32(query.GetValue(2)),
                     query.GetValue(3).ToString()
                  ));
               }
            }
         } catch { }
      }

      public static void LoadPlay(List<Play> data)
      {
         try {
            data.Clear();
            SQLiteDataReader query = Query("select * from `Play`;");
            if (query != null) {
               while (query.Read()) {
                  data.Add(new Play(
                     Convert.ToInt32(query.GetValue(0)),
                     query.GetValue(1).ToString(),
                     query.GetValue(2).ToString(),
                     query.GetValue(3).ToString(),
                     query.GetValue(4).ToString(),
                     Convert.ToInt32(query.GetValue(5)),
                     query.GetValue(6).ToString(),
                     query.GetValue(7).ToString()
                  ));
               }
            }
         } catch { }
      }
   }
}
