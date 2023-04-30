using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.ReplyMarkups;

namespace Play
{
   internal class Program
   {
      readonly static InlineKeyboardMarkup cancelKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "Otmena") } });
      readonly static InlineKeyboardMarkup menuKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🎲 Играть", "GoPlay") }, new[] { InlineKeyboardButton.WithCallbackData("📱 Личный кабинет", "UserArea") } });
      readonly static InlineKeyboardMarkup trueBalancePlayKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("👨🏼‍💼 Пригласить друга", "InviteFriend") }, new[] { InlineKeyboardButton.WithCallbackData("〽️ Поиск противника", "FindRival") } });
      readonly static InlineKeyboardMarkup addBalanceKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("💴 Пополнить баланс", "AddBalance") }, new[] { InlineKeyboardButton.WithCallbackData("🔚 В меню", "BackToMenu") } });
      readonly static InlineKeyboardMarkup addBalanceMenuKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("💴 Пополнить", "PayBalance") }, new[] { InlineKeyboardButton.WithCallbackData("🔚 В меню", "BackToMenu") } });
      readonly static InlineKeyboardMarkup goToMenuKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔚 В меню", "BackToMenu") } });
      readonly static InlineKeyboardMarkup getNumberKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🎲 Кинуть кости", "GetNumber") } });
      readonly static InlineKeyboardMarkup personalKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("💴 Пополнить баланс", "AddBalance") }, new[] { InlineKeyboardButton.WithCallbackData("🧾 История игр", "HistoryPlay") }, new[] { InlineKeyboardButton.WithCallbackData("🔚 В меню", "BackToMenu") } });


      private static string token { get; set; } = "6123367281:AAFrPTXtsRggDpUdP4j-7rzR40YL0FfUGEU";
      private static TelegramBotClient client;
      static void Main()
      {
         client = new TelegramBotClient(token);
         client.StartReceiving();
         client.OnMessage += ClientMessage;
         client.OnCallbackQuery += (object sc, CallbackQueryEventArgs ev) => {
            InlineButtonOperation(sc, ev);
         };
         Console.ReadLine();
      }

      static List<User> users = new List<User>();
      static List<Play> plays = new List<Play>();
      static List<Find> finds = new List<Find>();

      private static async void ClientMessage(object sender, MessageEventArgs e)
      {
         try {
            var message = e.Message;
            Connect.LoadPlay(plays);
            var user = GetUser(message.Chat.Id);
            if (user != null && user.message == "block") {
               await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
               return;
            }
            var play = plays.Find(x => x.player_1 == message.Chat.Id.ToString() && x.status != "end" || x.player_2 == message.Chat.Id.ToString() && x.status != "end");
            if (play == null) {
               var find = finds.Find(x => x.id == message.Chat.Id || x.rival == message.Chat.Id);
               if (find == null) {
                  if (message.Text == "/start") {
                     if (user == null) {
                        Connect.Query("insert into `User` (id, balance, message) values ('" + message.Chat.Id + "', 0, 'waitnickname');");
                        await client.SendTextMessageAsync(message.Chat.Id, "*Добро пожаловать*\n\nВведите своё игровое имя", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                     }
                     return;
                  }
                  else if (message.Text == "/menu" && user.message == "none") {
                     await client.SendTextMessageAsync(message.Chat.Id, "*Игровое меню*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: menuKey);
                     return;
                  }
                  else {
                     if (user != null) {
                        if (user.message == "waitnickname") { // ожидание никнейма от пользователя
                           Connect.LoadUser(users);
                           var nick = users.Find(x => x.nickname == message.Text);
                           if (nick == null && message.Text[0] != '/') {
                              Connect.Query("update `User` set nickname = '" + message.Text + "', message = 'none' where id = '" + message.Chat.Id + "';");
                              await client.SendTextMessageAsync(message.Chat.Id, "*Здравствуйте " + message.Text + "*\n\nДля открытия меню выберите соответствующий пункт в \"Меню\" или пропишите команду /menu", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           }
                           else
                              await client.SendTextMessageAsync(message.Chat.Id, "*Регистрация*\n\nДанное имя уже занято, введите другое", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           return;
                        }
                        else if (user.message == "waitinviteid") { // ожидание id друга для приглашения в игру
                           try {
                              try {
                                 await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId - 1);
                              } catch { }
                              Connect.LoadUser(users);
                              var friend = users.Find(x => x.nickname == message.Text);
                              if (friend != null) {
                                 if (friend.balance >= 10) {
                                    Connect.Query("update `User` set message = 'block' where id = '" + message.Chat.Id + "' or id = '" + friend.id + "';");
                                    var to = await client.SendTextMessageAsync(friend.id, "*Приглашение в игру*\n\nС вами хочет сыграть " + user.nickname, Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                    var from = await client.SendTextMessageAsync(message.Chat.Id, "*Пригласить друга*\n\nПриглашение в игру отправлено, ожидайте ответа", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                    InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CIV|" + from.MessageId + "|" + from.Chat.Id + "|" + to.MessageId + "|" + to.Chat.Id) } });
                                    InlineKeyboardMarkup inviteKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Принять", "AcceptInvite|" + from.MessageId + "|" + from.Chat.Id + "|" + to.MessageId + "|" + to.Chat.Id) }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отколнить", "CancelInvite|" + from.MessageId + "|" + from.Chat.Id + "|" + to.MessageId + "|" + to.Chat.Id) } });
                                    try {
                                       await client.EditMessageReplyMarkupAsync(message.Chat.Id, from.MessageId, replyMarkup: keyboard);
                                    } catch { }
                                    try {
                                       await client.EditMessageReplyMarkupAsync(to.Chat.Id, to.MessageId, replyMarkup: inviteKey);
                                    } catch { }
                                    int timer = 0;
                                    while (true) {
                                       if (timer == 100000) {
                                          Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "' or id = '" + friend.id + "';");
                                          await client.SendTextMessageAsync(message.Chat.Id, "*Приглашение в игру*\n\nИгрок " + friend.id + " не ответил на ваше предложение", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                          return;
                                       }
                                       await Task.Delay(300);
                                       timer += 300;
                                    }
                                 }
                                 else {
                                    await client.SendTextMessageAsync(message.Chat.Id, "*Пригасить друга*\n\nУ данного игрока недостаточно средств", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                    Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "';");
                                 }
                              }
                              else {
                                 await client.SendTextMessageAsync(message.Chat.Id, "*Пригасить друга*\n\nДанного игрока не существует", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                 Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "';");
                              }
                           } catch {
                              await client.SendTextMessageAsync(message.Chat.Id, "*Пригасить друга*\n\nДанного игрока не существует", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "';");
                           }
                           return;
                        }
                     }
                  }
               }
               else
                  await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
            }
            else
               await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
         } catch { }
      }

      private static async void InlineButtonOperation(object sc, CallbackQueryEventArgs ev)
      {
         try {
            var message = ev.CallbackQuery.Message;
            var data = ev.CallbackQuery.Data;
            Connect.LoadPlay(plays);
            if (plays.Find(x => x.player_1 == message.Chat.Id.ToString() && x.status != "end" || x.player_2 == message.Chat.Id.ToString() && x.status != "end") == null && finds.Find(x => x.id == message.Chat.Id || x.rival == message.Chat.Id) == null) {
               if (data == "GoPlay") {
                  var user = GetUser(message.Chat.Id);
                  if (user != null) {
                     if (user.balance >= 10) {
                        await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Начало игры*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: trueBalancePlayKey);
                     }
                     else
                        await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Недостаточно средств*\n\nСтоимость игры в кости: 10 фишек\nВаш баланс: " + user.balance + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: addBalanceKey);
                  }
                  return;
               }
               else if (data == "InviteFriend") {
                  Connect.Query("update `User` set message = 'waitinviteid' where id = '" + message.Chat.Id + "';");
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Пригласить друга*\n\nВведите имя друга для приглашения в игру", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelKey);
                  return;
               }
               else if (data == "FindRival") {
                  Connect.LoadPlay(plays);
                  var play = plays.Find(x => x.player_1 == message.Chat.Id.ToString() && x.status != "end" || x.player_2 == message.Chat.Id.ToString() && x.status != "end");
                  if (play == null) {
                     await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*〽️ Поиск противника*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: cancelKey);
                     var user = GetUser(message.Chat.Id);
                     if (user != null) {
                        if (user.message == "none") {
                           if (finds.Count > 0) {
                              for (int i = 0; i < finds.Count; i++) {
                                 if (finds[i].rival == -1) {
                                    finds[i].rival = message.Chat.Id;
                                    await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                                    return;
                                 }
                              }
                           }
                           else {
                              finds.Add(new Find(message.Chat.Id, -1));
                              int timer = 120;
                              Find search = null;
                              while (true) {
                                 try {
                                    search = finds.Find(x => x.id == message.Chat.Id && x.rival != -1);
                                    if (search != null) {
                                       if (search.rival != -1) {
                                          await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                                          Play(search, user);
                                          return;
                                       }
                                       else
                                          search = null;
                                    }
                                    if (timer >= 100000) {
                                       finds.Remove(search);
                                       await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*⛔️ Противник не найден*", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                       return;
                                    }
                                    timer += 300;
                                    await Task.Delay(300);
                                 } catch { }
                              }
                           }
                        }
                     }
                  }
                  else
                     await client.DeleteMessageAsync(message.Chat.Id, message.MessageId);
                  return;
               }
               else if (data == "AddBalance") {
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Пополнение баланса*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: addBalanceMenuKey);
                  return;
               }
               else if (data == "BackToMenu") {
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Игровое меню*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: menuKey);
                  return;
               }
               else if (data == "Otmena") {
                  var search = finds.Find(x => x.id == message.Chat.Id);
                  if (search != null)
                     finds.Remove(search);
                  Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "';");
                  await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "⛔️ Отменено");
                  return;
               }
               else if (data == "PayBalance") {
                  var user = GetUser(message.Chat.Id);
                  if (user != null) {
                     Connect.Query("update `User` set balance = " + Convert.ToInt32(user.balance + 10) + " where id = '" + message.Chat.Id + "';");
                     await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Успешно*\n\nНа ваш баланс зачислено 10 фишек\nТекущий баланс: " + Convert.ToInt32(user.balance + 10) + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: goToMenuKey);
                  }
                  return;
               }
               else if (data == "UserArea") {
                  var user = GetUser(message.Chat.Id);
                  if (user != null) {
                     await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*Личный кабинет*\n\nИгровой ник: " + user.nickname + "\nБаланс: " + user.balance + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: personalKey);
                  }
                  return;
               }
               else if (data == "HistoryPlay") {
                  string response = "*История последних игр*\n\n";
                  Connect.LoadPlay(plays);
                  var play = plays.FindAll(x => x.player_1 == message.Chat.Id.ToString() && x.status == "end" || x.player_2 == message.Chat.Id.ToString() && x.status == "end");
                  if (play.Count > 0) {
                     play.Reverse();
                     List<Play> lastPlay = new List<Play>();
                     for (int i = 0; i < 5; i++)
                        lastPlay.Add(new Play(play[i].id, play[i].player_1, play[i].player_2, play[i].score_1, play[i].score_2, play[i].bet, play[i].date, play[i].status));
                     User user = null;
                     for (int i = 0; i < lastPlay.Count; i++) {
                        if (lastPlay[i].player_1 != message.Chat.Id.ToString()) {
                           user = GetUser(Convert.ToInt64(lastPlay[i].player_1));
                           string victory = string.Empty;
                           if ((Convert.ToInt32(lastPlay[i].score_2.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_2.Split('|')[1])) > (Convert.ToInt32(lastPlay[i].score_1.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_1.Split('|')[1])))
                              victory = "✅ Победа";
                           else if ((Convert.ToInt32(lastPlay[i].score_2.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_2.Split('|')[1])) < (Convert.ToInt32(lastPlay[i].score_1.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_1.Split('|')[1])))
                              victory = "⛔️ Поражение";
                           else
                              victory = "⚠️ Ничья";
                           response += victory + "\nПротивник: " + user.nickname + "\nСтавка: " + lastPlay[i].bet + " фишек\nСчёт: " + GetScore(Convert.ToInt32(lastPlay[i].score_2.Split('|')[0]), Convert.ToInt32(lastPlay[i].score_2.Split('|')[1])) + "\nСчёт противника: " + GetScore(Convert.ToInt32(lastPlay[i].score_1.Split('|')[0]), Convert.ToInt32(lastPlay[i].score_1.Split('|')[1])) + "\nДата: " + lastPlay[i].date + "\n\n";
                        }
                        else {
                           user = GetUser(Convert.ToInt64(lastPlay[i].player_2));
                           string victory = string.Empty;
                           if ((Convert.ToInt32(lastPlay[i].score_1.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_1.Split('|')[1])) > (Convert.ToInt32(lastPlay[i].score_2.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_2.Split('|')[1])))
                              victory = "✅ Победа";
                           else if ((Convert.ToInt32(lastPlay[i].score_1.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_1.Split('|')[1])) < (Convert.ToInt32(lastPlay[i].score_2.Split('|')[0]) + Convert.ToInt32(lastPlay[i].score_2.Split('|')[1])))
                              victory = "⛔️ Поражение";
                           else
                              victory = "⚠️ Ничья";
                           response += victory + "\nПротивник: " + user.nickname + "\nСтавка: " + lastPlay[i].bet + " фишек\nСчёт: " + GetScore(Convert.ToInt32(lastPlay[i].score_1.Split('|')[0]), Convert.ToInt32(lastPlay[i].score_1.Split('|')[1])) + "\nСчёт противника: " + GetScore(Convert.ToInt32(lastPlay[i].score_2.Split('|')[0]), Convert.ToInt32(lastPlay[i].score_2.Split('|')[1])) + "\nДата: " + lastPlay[i].date + "\n\n";
                        }
                     }
                     await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, response, Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: goToMenuKey);
                  }
                  else
                     await client.EditMessageTextAsync(message.Chat.Id, message.MessageId, "*История игр*\n\nНе обнаружено недавних игр", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: goToMenuKey);
                  return;
               }
               else if (data.Contains("CIV|")) {
                  Connect.Query("update `User` set message = 'none' where id = '" + data.Split('|')[2] + "' or id = '" + data.Split('|')[4] + "';");
                  await client.EditMessageTextAsync(data.Split('|')[2], Convert.ToInt32(data.Split('|')[1]), "⛔️ Отменено", replyMarkup: null);
                  await client.DeleteMessageAsync(data.Split('|')[4], Convert.ToInt32(data.Split('|')[3]));
               }
               else if (data.Contains("AcceptInvite|")) {
                  Connect.Query("update `User` set message = 'none' where id = '" + data.Split('|')[2] + "' or id = '" + data.Split('|')[4] + "';");
                  await client.EditMessageReplyMarkupAsync(data.Split('|')[2], Convert.ToInt32(data.Split('|')[1]), replyMarkup: null);
                  await client.EditMessageReplyMarkupAsync(data.Split('|')[4], Convert.ToInt32(data.Split('|')[3]), replyMarkup: null);
                  var user = GetUser(Convert.ToInt64(data.Split('|')[2]));
                  Find search = new Find(Convert.ToInt64(data.Split('|')[2]), Convert.ToInt64(data.Split('|')[4]));
                  Play(search, user);
               }
               else if (data.Contains("CancelInvite|")) {
                  Connect.Query("update `User` set message = 'none' where id = '" + data.Split('|')[2] + "' or id = '" + data.Split('|')[4] + "';");
                  await client.EditMessageTextAsync(data.Split('|')[2], Convert.ToInt32(data.Split('|')[1]), "⛔️ Приглашение отклонено", replyMarkup: null);
                  await client.EditMessageTextAsync(data.Split('|')[4], Convert.ToInt32(data.Split('|')[3]), "⛔️ Отменено", replyMarkup: null);
               }
               else if (data.Contains("OneMoreTime|")) {
                  try {
                     await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
                  } catch { }
                  try {
                     await client.EditMessageReplyMarkupAsync(data.Split('|')[1], Convert.ToInt32(data.Split('|')[2]), replyMarkup: null);
                  } catch { }
                  var user = GetUser(message.Chat.Id);
                  if (user != null) {
                     var friend = GetUser(Convert.ToInt64(data.Split('|')[1]));
                     if (friend != null) {
                        var find = finds.Find(x => x.id.ToString() == data.Split('|')[1] || x.rival.ToString() == data.Split('|')[1]);
                        Connect.LoadPlay(plays);
                        if (find == null && plays.Find(x => x.player_1 == data.Split('|')[1] && x.status != "end" || x.player_2 == data.Split('|')[1] && x.status != "end") == null) {
                           if (friend.balance >= 10) {
                              Connect.Query("update `User` set message = 'block' where id = '" + message.Chat.Id + "' or id = '" + friend.id + "';");
                              var to = await client.SendTextMessageAsync(friend.id, "*Приглашение в игру*\n\nИгрок " + user.nickname + " требует реванш", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              var from = await client.SendTextMessageAsync(message.Chat.Id, "*Пригласить друга*\n\nЗапрос на реванш отправлен", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отменить", "CIV|" + from.MessageId + "|" + from.Chat.Id + "|" + to.MessageId + "|" + to.Chat.Id) } });
                              InlineKeyboardMarkup inviteKey = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("✅ Принять", "AcceptInvite|" + from.MessageId + "|" + from.Chat.Id + "|" + to.MessageId + "|" + to.Chat.Id) }, new[] { InlineKeyboardButton.WithCallbackData("⛔️ Отколнить", "CancelInvite|" + from.MessageId + "|" + from.Chat.Id + "|" + to.MessageId + "|" + to.Chat.Id) } });
                              try {
                                 await client.EditMessageReplyMarkupAsync(message.Chat.Id, from.MessageId, replyMarkup: keyboard);
                              } catch { }
                              try {
                                 await client.EditMessageReplyMarkupAsync(to.Chat.Id, to.MessageId, replyMarkup: inviteKey);
                              } catch { }
                              int timer = 0;
                              while (true) {
                                 if (timer == 100000) {
                                    Connect.Query("update `User` set message = 'none' where id = '" + message.Chat.Id + "' or id = '" + friend.id + "';");
                                    await client.SendTextMessageAsync(message.Chat.Id, "*Приглашение в игру*\n\nИгрок " + friend.id + " не ответил на ваше предложение", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                    return;
                                 }
                                 await Task.Delay(300);
                                 timer += 300;
                              }
                           }
                           else
                              await client.SendTextMessageAsync(message.Chat.Id, "*Пригасить друга*\n\nУ данного пользователя недостаточно баланса", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                        }
                        else
                           await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
                     }
                  }
               }
            }
            else {
               if (data == "GetNumber") {
                  try {
                     await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
                  } catch { }
                  var play = plays.Find(x => x.player_1 == message.Chat.Id.ToString() && x.status == "progress" || x.player_2 == message.Chat.Id.ToString() && x.status == "progress");
                  if (play != null) {
                     Random rnd = new Random();
                     int score_1 = rnd.Next(1, 6);
                     int score_2 = rnd.Next(1, 6);
                     Connect.LoadUser(users);
                     if (play.player_1 == message.Chat.Id.ToString()) {
                        Connect.Query("update `Play` set score_1 = '" + score_1 + "|" + score_2 + "' where id = " + play.id + ";");
                        if (play.score_1 == "") {
                           string req = GetScore(score_1, score_2);
                           var user = users.Find(x => x.id == message.Chat.Id.ToString());
                           if (user != null) {
                              await client.SendTextMessageAsync(message.Chat.Id, req + "\nИгроку " + user.nickname + " выпало число *" + Convert.ToInt32(score_1 + score_2) + "*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: null);
                              await client.SendTextMessageAsync(play.player_2, req + "\nИгроку " + user.nickname + " выпало число *" + Convert.ToInt32(score_1 + score_2) + "*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: null);
                           }
                        }
                     }
                     else if (play.player_2 == message.Chat.Id.ToString()) {
                        Connect.Query("update `Play` set score_2 = '" + score_1 + "|" + score_2 + "' where id = " + play.id + ";");
                        if (play.score_2 == "") {
                           string req = GetScore(score_1, score_2);
                           var user = users.Find(x => x.id == message.Chat.Id.ToString());
                           if (user != null) {
                              await client.SendTextMessageAsync(message.Chat.Id, req + "\nИгроку " + user.nickname + " выпало число *" + Convert.ToInt32(score_1 + score_2) + "*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: null);
                              await client.SendTextMessageAsync(play.player_1, req + "\nИгроку " + user.nickname + " выпало число *" + Convert.ToInt32(score_1 + score_2) + "*", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: null);
                           }
                        }
                     }
                  }
                  return;
               }
               else
                  await client.EditMessageReplyMarkupAsync(message.Chat.Id, message.MessageId, replyMarkup: null);
            }
         } catch { }
      }

      private static async void Play(Find search, User user)
      {
         try {
            Connect.Query("insert into `Play` (player_1, player_2, bet, status) values ('" + search.id + "', '" + search.rival + "', 10, 'progress');");
            Connect.LoadPlay(plays);
            var play = plays.Find(x => x.player_1 == search.id.ToString() && x.status == "progress" && x.player_2 == search.rival.ToString());
            if (play != null) {
               finds.Remove(search);
               int balance_1 = GetUser(Convert.ToInt64(play.player_1)).balance - 10;
               int balance_2 = GetUser(Convert.ToInt64(play.player_2)).balance - 10;
               var msgId_1 = await client.SendTextMessageAsync(search.id, "*Противник найден*\n\nЙо-хо ну что, поехали сыграем, кому то сегодня фартанет одному из вас так точно", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: getNumberKey);
               await Task.Delay(300);
               var msgId_2 = await client.SendTextMessageAsync(search.rival, "*Противник найден*\n\nЙо-хо ну что, поехали сыграем, кому то сегодня фартанет одному из вас так точно", Telegram.Bot.Types.Enums.ParseMode.Markdown, replyMarkup: getNumberKey);
               try {
                  await client.DeleteMessageAsync(search.rival, msgId_2.MessageId - 1);
               } catch { }
               int timer = 0;
               while (true) {
                  try {
                     Connect.LoadPlay(plays);
                     play = plays.Find(x => x.id == play.id);
                     if (play.score_1 != "" && play.score_2 != "") { // если оба игрока получили число
                        if ((Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) > (Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1]))) {
                           user = GetUser(Convert.ToInt64(play.player_1));
                           string winner = user.nickname;
                           user = GetUser(Convert.ToInt64(play.player_2));
                           if (user != null) {
                              balance_1 += 20;
                              Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + play.player_1 + "'; update `User` set balance = " + balance_2 + " where id = '" + play.player_2 + "'; update `Play` set date = '" + DateTime.Now + "', status = 'end' where id = " + play.id + ";");
                              await Task.Delay(300);
                              var msg1 = await client.SendTextMessageAsync(play.player_1, "*✅ Победа*\n\nВы выиграли игрока " + user.nickname + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) + ":" + Convert.ToInt32(Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1])) + "\nТекущий баланс: " + balance_1 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              await Task.Delay(300);
                              var msg2 = await client.SendTextMessageAsync(play.player_2, "*⛔️ Поражение*\n\nВы проиграли игроку " + winner + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) + ":" + Convert.ToInt32(Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1])) + "\nТекущий баланс: " + balance_2 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              InlineKeyboardMarkup omtKey1 = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔱 Реванш", "OneMoreTime|" + search.rival + "|" + msg2.MessageId) } });
                              InlineKeyboardMarkup omtKey2 = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔱 Реванш", "OneMoreTime|" + search.id + "|" + msg1.MessageId) } });
                              try { await client.EditMessageReplyMarkupAsync(play.player_1, msg1.MessageId, replyMarkup: omtKey1); await Task.Delay(10000); await client.EditMessageReplyMarkupAsync(play.player_1, msg1.MessageId, replyMarkup: null); } catch { }
                              try { await client.EditMessageReplyMarkupAsync(play.player_2, msg2.MessageId, replyMarkup: omtKey2); await Task.Delay(10000); await client.EditMessageReplyMarkupAsync(play.player_2, msg2.MessageId, replyMarkup: null); } catch { }
                              return;
                           }
                        }
                        else if ((Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) < (Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1]))) {
                           user = GetUser(Convert.ToInt64(play.player_2));
                           string winner = user.nickname;
                           user = GetUser(Convert.ToInt64(play.player_1));
                           if (user != null) {
                              balance_2 += 20;
                              Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + play.player_1 + "'; update `User` set balance = " + balance_2 + " where id = '" + play.player_2 + "'; update `Play` set date = '" + DateTime.Now + "', status = 'end' where id = " + play.id + ";");
                              await Task.Delay(300);
                              var msg2 = await client.SendTextMessageAsync(play.player_2, "*✅ Победа*\n\nВы выиграли игрока " + user.nickname + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) + ":" + Convert.ToInt32(Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1])) + "\nТекущий баланс: " + balance_2 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              await Task.Delay(300);
                              var msg1 = await client.SendTextMessageAsync(play.player_1, "*⛔️ Поражение*\n\nВы проиграли игроку " + winner + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) + ":" + Convert.ToInt32(Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1])) + "\nТекущий баланс: " + balance_1 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              InlineKeyboardMarkup omtKey1 = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔱 Реванш", "OneMoreTime|" + search.rival + "|" + msg2.MessageId) } });
                              InlineKeyboardMarkup omtKey2 = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔱 Реванш", "OneMoreTime|" + search.id + "|" + msg1.MessageId) } });
                              try { await client.EditMessageReplyMarkupAsync(play.player_1, msg1.MessageId, replyMarkup: omtKey1); } catch { }
                              try { await client.EditMessageReplyMarkupAsync(play.player_2, msg2.MessageId, replyMarkup: omtKey2); } catch { }
                              return;
                           }
                        }
                        else {
                           balance_1 += 10;
                           balance_2 += 10;
                           Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + play.player_1 + "'; update `User` set balance = " + balance_2 + " where id = '" + play.player_2 + "'; update `Play` set date = '" + DateTime.Now + "', status = 'end' where id = " + play.id + ";");
                           await Task.Delay(300);
                           var msg1 = await client.SendTextMessageAsync(play.player_1, "*⚠️ Ничья*\n\nИгра закончилась с равным счётом\nТекущий баланс: " + balance_1 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           await Task.Delay(300);
                           var msg2 = await client.SendTextMessageAsync(play.player_2, "*⚠️ Ничья*\n\nИгра закончилась с равным счётом\nТекущий баланс: " + balance_2 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           InlineKeyboardMarkup omtKey1 = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔱 Реванш", "OneMoreTime|" + search.rival + "|" + msg2.MessageId) } });
                           InlineKeyboardMarkup omtKey2 = new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("🔱 Реванш", "OneMoreTime|" + search.id + "|" + msg1.MessageId) } });
                           try { await client.EditMessageReplyMarkupAsync(play.player_1, msg1.MessageId, replyMarkup: omtKey1); } catch { }
                           try { await client.EditMessageReplyMarkupAsync(play.player_2, msg2.MessageId, replyMarkup: omtKey2); } catch { }
                           return;
                        }
                     }
                     if (timer >= 100000) {
                        if (play.score_1 != "" && play.score_2 == "") { // если получил число только первый игрок
                           balance_1 += 20;
                           Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + play.player_1 + "'; update `User` set balance = " + balance_2 + " where id = '" + play.player_2 + "'; update `Play` set date = '" + DateTime.Now + "', status = 'end', score_2 = '0|0' where id = " + play.id + ";");
                           user = null;
                           user = GetUser(Convert.ToInt64(play.player_1));
                           string winner = user.nickname;
                           if (user != null) {
                              Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + user.id + "';");
                              user = GetUser(Convert.ToInt64(play.player_2));
                              if (user != null) {
                                 await Task.Delay(300);
                                 await client.SendTextMessageAsync(play.player_1, "*✅ Победа*\n\nВы выиграли игрока " + user.nickname + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) + ":0\nТекущий баланс: " + balance_1 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                 await Task.Delay(300);
                                 await client.SendTextMessageAsync(play.player_2, "*⛔️ Поражение*\n\nВы проиграли игроку " + winner + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_1.Split('|')[0]) + Convert.ToInt32(play.score_1.Split('|')[1])) + ":0\nТекущий баланс: " + balance_2 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              }
                              return;
                           }
                        }
                        else if (play.score_2 != "" && play.score_1 == "") { // если получил число только второй игрок
                           balance_2 += 20;
                           Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + play.player_1 + "'; update `User` set balance = " + balance_2 + " where id = '" + play.player_2 + "'; update `Play` set date = '" + DateTime.Now + "', status = 'end', score_1 = '0|0' where id = " + play.id + ";");
                           user = null;
                           user = GetUser(Convert.ToInt64(play.player_2));
                           string winner = user.nickname;
                           if (user != null) {
                              Connect.Query("update `User` set balance = " + balance_2 + " where id = '" + user.id + "';");
                              user = GetUser(Convert.ToInt64(play.player_1));
                              if (user != null) {
                                 await Task.Delay(300);
                                 await client.SendTextMessageAsync(play.player_2, "*✅ Победа*\n\nВы выиграли игрока " + user.nickname + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1])) + ":0\nТекущий баланс: " + balance_2 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                 await Task.Delay(300);
                                 await client.SendTextMessageAsync(play.player_1, "*⛔️ Поражение*\n\nВы проиграли игроку " + winner + " со счетом " + Convert.ToInt32(Convert.ToInt32(play.score_2.Split('|')[0]) + Convert.ToInt32(play.score_2.Split('|')[1])) + ":0\nТекущий баланс: " + balance_1 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                              }
                              return;
                           }
                        }
                        else if (play.score_1 == "" && play.score_2 == "") { // если никто не получил число
                           balance_1 += 10;
                           balance_2 += 10;
                           Connect.Query("update `User` set balance = " + balance_1 + " where id = '" + play.player_1 + "'; update `User` set balance = " + balance_2 + " where id = '" + play.player_2 + "'; update `Play` set date = '" + DateTime.Now + "', status = 'end', score_1 = '0|0', score_2 = '0|0' where id = " + play.id + ";");
                           await Task.Delay(300);
                           await client.SendTextMessageAsync(play.player_1, "*⚠️ Ничья*\n\nНикто из игроков не бросил кости, сумма ставки возвращена на баланс\nТекущий баланс: " + balance_1 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           await Task.Delay(300);
                           await client.SendTextMessageAsync(play.player_2, "*⚠️ Ничья*\n\nНикто из игроков не бросил кости, сумма ставки возвращена на баланс\nТекущий баланс: " + balance_2 + " фишек", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                           return;
                        }
                     }
                     await Task.Delay(1000);
                     timer += 950;
                  } catch { }
               }
            }
         } catch { }
         return;
      }

      private static User GetUser(long chatId)
      {
         try {
            Connect.LoadUser(users);
            return users.Find(x => x.id == chatId.ToString());
         } catch { return null; }
      }

      private static string GetScore(int score_1, int score_2)
      {
         try {
            string request = string.Empty;
            if (score_1 == 1) request += "1️⃣";
            else if (score_1 == 2) request += "2️⃣";
            else if (score_1 == 3) request += "3️⃣";
            else if (score_1 == 4) request += "4️⃣";
            else if (score_1 == 5) request += "5️⃣";
            else if (score_1 == 6) request += "6️⃣";
            else if (score_1 == 0) request += " 0️⃣";

            if (score_2 == 1) request += " 1️⃣";
            else if (score_2 == 2) request += " 2️⃣";
            else if (score_2 == 3) request += " 3️⃣";
            else if (score_2 == 4) request += " 4️⃣";
            else if (score_2 == 5) request += " 5️⃣";
            else if (score_2 == 6) request += " 6️⃣";
            else if (score_2 == 0) request += " 0️⃣";
            return request;
         } catch { return null; }
      }
   }
}
