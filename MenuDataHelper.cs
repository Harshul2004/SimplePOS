using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SimplePOS
{
 public class MenuItemModel
 {
 public string Name { get; set; }
 public double Price { get; set; }
 }

 public static class MenuDataHelper
 {
 public static List<MenuItemModel> LoadMenuItems()
 {
 var items = new List<MenuItemModel>();
 using var conn = new SQLiteConnection($"Data Source={AppDomain.CurrentDomain.BaseDirectory}menu.db;Version=3;");
 conn.Open();
 using var cmd = new SQLiteCommand("SELECT Name, Price FROM MenuItems ORDER BY Name", conn);
 using var reader = cmd.ExecuteReader();
 while (reader.Read())
 {
 items.Add(new MenuItemModel
 {
 Name = reader.GetString(0),
 Price = reader.GetDouble(1)
 });
 }
 return items;
 }
 }
}
