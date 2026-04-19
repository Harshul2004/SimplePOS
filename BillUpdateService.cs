using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.Json;

namespace SimplePOS
{
 public static class BillUpdateService
 {
 public static void UpdateBillItemsAndTotal(string billId, List<BillItem> items, double newTotal)
 {
 if (string.IsNullOrWhiteSpace(billId)) throw new ArgumentException("BillId required");
 string itemsJson = JsonSerializer.Serialize(items);
 using var conn = new SQLiteConnection($"Data Source={AppDomain.CurrentDomain.BaseDirectory}menu.db;Version=3;");
 conn.Open();
 using var tx = conn.BeginTransaction();
 try
 {
 using var cmd = new SQLiteCommand("UPDATE Bills SET Items = @items, TotalAmount = @total WHERE BillId = @billId", conn, tx);
 cmd.Parameters.AddWithValue("@items", itemsJson);
 cmd.Parameters.AddWithValue("@total", newTotal);
 cmd.Parameters.AddWithValue("@billId", billId);
 int rows = cmd.ExecuteNonQuery();
 if (rows !=1)
 throw new Exception("Bill not found or update failed.");
 tx.Commit();
 }
 catch
 {
 tx.Rollback();
 throw;
 }
 }
 }
}
