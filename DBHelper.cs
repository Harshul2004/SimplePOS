using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace SimplePOS
{
    internal static class DBHelper
    {
        private static string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "menu.db");

        public static void EnsureDatabase()
        {
            if (!File.Exists(dbPath))
                SQLiteConnection.CreateFile(dbPath);

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // ---- Create Categories table ----
                string createCategoriesTable = @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL
                    );";
                new SQLiteCommand(createCategoriesTable, conn).ExecuteNonQuery();

                // ---- Create SubCategories table ----
                string createSubCategoriesTable = @"
                    CREATE TABLE IF NOT EXISTS SubCategories (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        CategoryID INTEGER,
                        FOREIGN KEY(CategoryID) REFERENCES Categories(ID)
                    );";
                new SQLiteCommand(createSubCategoriesTable, conn).ExecuteNonQuery();

                // ---- Create MenuItems table ----
                string createMenuItemsTable = @"
                    CREATE TABLE IF NOT EXISTS MenuItems (
                        ID INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Price REAL NOT NULL,
                        SubCategoryID INTEGER,
                        FOREIGN KEY(SubCategoryID) REFERENCES SubCategories(ID)
                    );";
                new SQLiteCommand(createMenuItemsTable, conn).ExecuteNonQuery();

                // ---- Create AdminCredentials table ----
                string createAdminTable = @"
                CREATE TABLE IF NOT EXISTS AdminCredentials (
                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                    PIN TEXT NOT NULL
                );";
                new SQLiteCommand(createAdminTable, conn).ExecuteNonQuery();

                // ---- Create Bills table ----
                string createBillsTable = @"
                    CREATE TABLE IF NOT EXISTS Bills (
                        BillId TEXT PRIMARY KEY,
                        Date TEXT NOT NULL,
                        Time TEXT NOT NULL,
                        Items TEXT NOT NULL,
                        TotalAmount REAL NOT NULL
                    );";
                new SQLiteCommand(createBillsTable, conn).ExecuteNonQuery();



                // ---- Seed default PIN if table empty ----
                if ((long)new SQLiteCommand("SELECT COUNT(*) FROM AdminCredentials;", conn).ExecuteScalar() == 0)
                {
                    string insertDefaultPin = "INSERT INTO AdminCredentials (PIN) VALUES ('1234');"; // default, can change later from GUI
                    new SQLiteCommand(insertDefaultPin, conn).ExecuteNonQuery();
                }

                // ---- Seed Categories ----
                if ((long)new SQLiteCommand("SELECT COUNT(*) FROM Categories;", conn).ExecuteScalar() == 0)
                {
                    string insertCategories = @"
                        INSERT INTO Categories (Name) VALUES
                        ('FOOD'),
                        ('BEVERAGES'),
                        ('DESSERT');";
                    new SQLiteCommand(insertCategories, conn).ExecuteNonQuery();
                }

                // ---- Seed SubCategories ----
                if ((long)new SQLiteCommand("SELECT COUNT(*) FROM SubCategories;", conn).ExecuteScalar() == 0)
                {
                    string insertSubCategories = @"
                        -- FOOD
                        INSERT INTO SubCategories (Name, CategoryID) VALUES 
                        ('Dosa', 1),
                        ('Pizza', 1),
                        ('Indian Dishes', 1),
                        ('South Indian', 1),
                        -- BEVERAGES
                        ('Coffee', 2),
                        ('Tea', 2),
                        ('Cold Drinks', 2),
                        ('Water', 2),
                        -- DESSERT
                        ('Ice Creams', 3),
                        ('Sundaes', 3);";
                    new SQLiteCommand(insertSubCategories, conn).ExecuteNonQuery();
                }

                // ---- Seed MenuItems ----
                if ((long)new SQLiteCommand("SELECT COUNT(*) FROM MenuItems;", conn).ExecuteScalar() == 0)
                {
                    string insertMenuItems = @"
                        INSERT INTO MenuItems (Name, Price, SubCategoryID) VALUES
                        ('Masala Dosa', 50, 1),
                        ('Idli', 30, 4),
                        ('Samosa', 20, 3),
                        ('Tea', 10, 6),
                        ('Coffee', 15, 5),
                        ('Coke', 25, 7),
                        ('Vanilla Ice Cream', 60, 9);";
                    new SQLiteCommand(insertMenuItems, conn).ExecuteNonQuery();
                }

                conn.Close();
            }
        }

        // ---- Get all categories ----
        public static List<(int ID, string Name)> GetCategories()
        {
            var list = new List<(int, string)>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT ID, Name FROM Categories", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add((reader.GetInt32(0), reader.GetString(1)));
                conn.Close();
            }
            return list;
        }

        // ---- Get subcategories by category ----
        public static List<(int ID, string Name)> GetSubCategories(int categoryId)
        {
            var list = new List<(int, string)>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT ID, Name FROM SubCategories WHERE CategoryID=@catId", conn);
                cmd.Parameters.AddWithValue("@catId", categoryId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add((reader.GetInt32(0), reader.GetString(1)));
                conn.Close();
            }
            return list;
        }

        // ---- Get menu items by subcategory ----
        public static List<(int ID, string Name, double Price)> GetMenuBySubCategory(int subCategoryId)
        {
            var list = new List<(int, string, double)>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT ID, Name, Price FROM MenuItems WHERE SubCategoryID=@subCatId", conn);
                cmd.Parameters.AddWithValue("@subCatId", subCategoryId);
                using var reader = cmd.ExecuteReader();
                while (reader.Read()) list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetDouble(2)));
                conn.Close();
            }
            return list;
        }

        // ---- Add menu item ----
        public static void AddMenuItem(string name, double price, int subCategoryId)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO MenuItems (Name, Price, SubCategoryID) VALUES (@name, @price, @subCatId)", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@subCatId", subCategoryId);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static void DeleteMenuItem(int id)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM MenuItems WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static void AddCategory(string name)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO Categories (Name) VALUES (@name)", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static void DeleteCategory(int id)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM Categories WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static void AddSubCategory(string name, int categoryId)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO SubCategories (Name, CategoryID) VALUES (@name, @catId)", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@catId", categoryId);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static void DeleteSubCategory(int id)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM SubCategories WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static string GetAdminPIN()
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT PIN FROM AdminCredentials LIMIT 1;", conn);
            var pin = cmd.ExecuteScalar()?.ToString();
            conn.Close();
            return pin ?? "1234"; // fallback just in case
        }

        public static void SetAdminPIN(string newPin)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE AdminCredentials SET PIN=@pin WHERE ID=1;", conn);
            cmd.Parameters.AddWithValue("@pin", newPin);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        // ---- Update Category ----
        public static void UpdateCategory(int id, string newName)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE Categories SET Name=@name WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@name", newName);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        // ---- Update SubCategory ----
        public static void UpdateSubCategory(int id, string newName, int newCategoryId)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE SubCategories SET Name=@name, CategoryID=@catId WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@name", newName);
            cmd.Parameters.AddWithValue("@catId", newCategoryId);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        // ---- Update MenuItem ----
        public static void UpdateMenuItem(int id, string newName, double newPrice, int newSubCategoryId)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("UPDATE MenuItems SET Name=@name, Price=@price, SubCategoryID=@subCatId WHERE ID=@id", conn);
            cmd.Parameters.AddWithValue("@name", newName);
            cmd.Parameters.AddWithValue("@price", newPrice);
            cmd.Parameters.AddWithValue("@subCatId", newSubCategoryId);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static void AddBill(string billId, DateTime dateTime, string itemsJson, double total)
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("INSERT INTO Bills (BillId, Date, Time, Items, TotalAmount) VALUES (@billId, @date, @time, @items, @total)", conn);
            cmd.Parameters.AddWithValue("@billId", billId);
            cmd.Parameters.AddWithValue("@date", dateTime.ToString("dd/MM/yyyy"));
            cmd.Parameters.AddWithValue("@time", dateTime.ToString("HH:mm"));
            cmd.Parameters.AddWithValue("@items", itemsJson);
            cmd.Parameters.AddWithValue("@total", total);
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static string GetLastBillId()
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT BillId FROM Bills ORDER BY ROWID DESC LIMIT 1", conn);
            var result = cmd.ExecuteScalar();
            conn.Close();
            return result?.ToString() ?? "BJFF-AA000000";
        }

        public static string GetNextBillId()
        {
            string lastBillId = GetLastBillId(); // e.g., "BJFF-AA000001"
            string prefix = "BJFF-";
            string alpha = lastBillId.Substring(5, 2);
            int number = int.Parse(lastBillId.Substring(7, 6));

            // Increment number, and if it overflows, increment alpha
            number++;
            if (number > 999999)
            {
                number = 1;
                alpha = IncrementAlpha(alpha);
            }
            return $"{prefix}{alpha}{number:D6}";
        }

        // Helper to increment two-letter alpha code
        private static string IncrementAlpha(string alpha)
        {
            char first = alpha[0];
            char second = alpha[1];
            if (second < 'Z')
                second++;
            else
            {
                second = 'A';
                if (first < 'Z')
                    first++;
                else
                    first = 'A'; // wrap around after ZZ
            }
            return $"{first}{second}";
        }

        public static List<Bill> GetBillsByDate(DateTime date)
        {
            var bills = new List<Bill>();
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            string dateStr = date.ToString("dd/MM/yyyy");
            using var cmd = new SQLiteCommand("SELECT * FROM Bills WHERE Date = @date", conn);
            cmd.Parameters.AddWithValue("@date", dateStr);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                bills.Add(new Bill
                {
                    BillId = reader.GetString(reader.GetOrdinal("BillId")),
                    Date = reader.GetString(reader.GetOrdinal("Date")),
                    Time = reader.GetString(reader.GetOrdinal("Time")),
                    Items = reader.GetString(reader.GetOrdinal("Items")),
                    TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount"))
                });
            }
            conn.Close();
            return bills;
        }

        public static Bill GetLastBill()
        {
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT * FROM Bills ORDER BY ROWID DESC LIMIT 1", conn);
            using var reader = cmd.ExecuteReader();
            Bill bill = null;
            if (reader.Read())
            {
                bill = new Bill
                {
                    BillId = reader.GetString(reader.GetOrdinal("BillId")),
                    Date = reader.GetString(reader.GetOrdinal("Date")),
                    Time = reader.GetString(reader.GetOrdinal("Time")),
                    Items = reader.GetString(reader.GetOrdinal("Items")),
                    TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount"))
                };
            }
            conn.Close();
            return bill;
        }

        // Get a bill by its BillId. Returns null if not found.
        public static Bill GetBillById(string billId)
        {
            if (string.IsNullOrEmpty(billId)) return null;
            using var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;");
            conn.Open();
            using var cmd = new SQLiteCommand("SELECT * FROM Bills WHERE BillId = @billId LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@billId", billId);
            using var reader = cmd.ExecuteReader();
            Bill bill = null;
            if (reader.Read())
            {
                bill = new Bill
                {
                    BillId = reader.GetString(reader.GetOrdinal("BillId")),
                    Date = reader.GetString(reader.GetOrdinal("Date")),
                    Time = reader.GetString(reader.GetOrdinal("Time")),
                    Items = reader.GetString(reader.GetOrdinal("Items")),
                    TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount"))
                };
            }
            conn.Close();
            return bill;
        }

        // ---- Get all subcategories ----
        public static List<(int ID, string Name, int CategoryID)> GetAllSubCategories()
        {
            var list = new List<(int, string, int)>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT ID, Name, CategoryID FROM SubCategories", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2)));
                conn.Close();
            }
            return list;
        }

        // ---- Get all menu items ----
        public static List<(int ID, string Name, double Price, int SubCategoryID)> GetAllMenuItems()
        {
            var list = new List<(int, string, double, int)>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT ID, Name, Price, SubCategoryID FROM MenuItems", conn);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                    list.Add((reader.GetInt32(0), reader.GetString(1), reader.GetDouble(2), reader.GetInt32(3)));
                conn.Close();
            }
            return list;
        }

        // Get latest N bills (for cache)
        public static List<Bill> GetLatestBills(int count)
        {
            var bills = new List<Bill>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT * FROM Bills ORDER BY ROWID DESC LIMIT @limit", conn);
                cmd.Parameters.AddWithValue("@limit", count);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bills.Add(new Bill
                    {
                        BillId = reader.GetString(reader.GetOrdinal("BillId")),
                        Date = reader.GetString(reader.GetOrdinal("Date")),
                        Time = reader.GetString(reader.GetOrdinal("Time")),
                        Items = reader.GetString(reader.GetOrdinal("Items")),
                        TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount"))
                    });
                }
                conn.Close();
            }
            return bills;
        }

        // Get bills by page (for paging)
        public static List<Bill> GetBillsPage(int pageIndex, int pageSize)
        {
            var bills = new List<Bill>();
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using var cmd = new SQLiteCommand("SELECT * FROM Bills ORDER BY ROWID DESC LIMIT @limit OFFSET @offset", conn);
                cmd.Parameters.AddWithValue("@limit", pageSize);
                cmd.Parameters.AddWithValue("@offset", pageIndex * pageSize);
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    bills.Add(new Bill
                    {
                        BillId = reader.GetString(reader.GetOrdinal("BillId")),
                        Date = reader.GetString(reader.GetOrdinal("Date")),
                        Time = reader.GetString(reader.GetOrdinal("Time")),
                        Items = reader.GetString(reader.GetOrdinal("Items")),
                        TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount"))
                    });
                }
                conn.Close();
            }
            return bills;
        }

        public static void UpdateBill(string billId, DateTime dateTime, string itemsJson, double total)
        {
            using var conn = new System.Data.SQLite.SQLiteConnection($"Data Source={AppDomain.CurrentDomain.BaseDirectory}menu.db;Version=3;");
            conn.Open();
            using var cmd = new System.Data.SQLite.SQLiteCommand(
                "UPDATE Bills SET Date = @date, Time = @time, Items = @items, TotalAmount = @total WHERE BillId = @billId", conn);
            // Always use dd/MM/yyyy and HH:mm for date/time
            cmd.Parameters.AddWithValue("@date", dateTime.ToString("dd/MM/yyyy"));
            cmd.Parameters.AddWithValue("@time", dateTime.ToString("HH:mm"));
            cmd.Parameters.AddWithValue("@items", itemsJson);
            cmd.Parameters.AddWithValue("@total", total);
            cmd.Parameters.AddWithValue("@billId", billId);
            cmd.ExecuteNonQuery();
            conn.Close();
        }
    }
}
