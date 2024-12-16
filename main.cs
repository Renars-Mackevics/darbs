using System;
using System.Data.SQLite;

namespace TeslaRentalPlatform
{
    class Program
    {
        static void Main(string[] args)
        {
            DB.Setup();
            DB.AddCar("M3", 20, 0.5);
            DB.AddCar("MY", 25, 0.6);

            var cid = DB.AddClient("Renars", "renars@mail.com");
            var rid = DB.StartRent(cid, 1, DateTime.Now);

            System.Threading.Thread.Sleep(2000);
            DB.EndRent(rid, DateTime.Now.AddHours(3), 150);

            Console.WriteLine(DB.GetInfo(rid));
        }
    }

    public static class DB
    {
        private const string Conn = "Data Source=tesla_rent.db;Version=3;";

        public static void Setup()
        {
            using (var c = new SQLiteConnection(Conn))
            {
                c.Open();

                var cars =
                    "CREATE TABLE IF NOT EXISTS Cars (\n" +
                    "    ID      INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                    "    Model   TEXT,\n" +
                    "    HrRate  REAL,\n" +
                    "    KmRate  REAL\n" +
                    ");";

                var clients =
                    "CREATE TABLE IF NOT EXISTS Clients (\n" +
                    "    ID      INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                    "    Name    TEXT,\n" +
                    "    Email   TEXT\n" +
                    ");";

                var rents =
                    "CREATE TABLE IF NOT EXISTS Rents (\n" +
                    "    ID      INTEGER PRIMARY KEY AUTOINCREMENT,\n" +
                    "    CID     INTEGER,\n" +
                    "    CarID   INTEGER,\n" +
                    "    Start   DATETIME,\n" +
                    "    End     DATETIME,\n" +
                    "    Kms     REAL,\n" +
                    "    Cost    REAL\n" +
                    ");";

                new SQLiteCommand(cars, c).ExecuteNonQuery();
                new SQLiteCommand(clients, c).ExecuteNonQuery();
                new SQLiteCommand(rents, c).ExecuteNonQuery();
            }
        }

        public static int AddCar(string m, double hr, double kr)
        {
            using (var c = new SQLiteConnection(Conn))
            {
                c.Open();
                var cmd = new SQLiteCommand(
                    "INSERT INTO Cars (Model, HrRate, KmRate) VALUES (@m, @hr, @kr);",
                    c
                );
                cmd.Parameters.AddWithValue("@m", m);
                cmd.Parameters.AddWithValue("@hr", hr);
                cmd.Parameters.AddWithValue("@kr", kr);
                cmd.ExecuteNonQuery();
                return (int)c.LastInsertRowId;
            }
        }

        public static int AddClient(string n, string e)
        {
            using (var c = new SQLiteConnection(Conn))
            {
                c.Open();
                var cmd = new SQLiteCommand(
                    "INSERT INTO Clients (Name, Email) VALUES (@n, @e);",
                    c
                );
                cmd.Parameters.AddWithValue("@n", n);
                cmd.Parameters.AddWithValue("@e", e);
                cmd.ExecuteNonQuery();
                return (int)c.LastInsertRowId;
            }
        }

        public static int StartRent(int cid, int carid, DateTime start)
        {
            using (var c = new SQLiteConnection(Conn))
            {
                c.Open();
                var cmd = new SQLiteCommand(
                    "INSERT INTO Rents (CID, CarID, Start) VALUES (@cid, @carid, @start);",
                    c
                );
                cmd.Parameters.AddWithValue("@cid", cid);
                cmd.Parameters.AddWithValue("@carid", carid);
                cmd.Parameters.AddWithValue("@start", start);
                cmd.ExecuteNonQuery();
                return (int)c.LastInsertRowId;
            }
        }

        public static void EndRent(int rid, DateTime end, double kms)
        {
            using (var c = new SQLiteConnection(Conn))
            {
                c.Open();
                var cmd = new SQLiteCommand(
                    "SELECT Rents.Start, Cars.HrRate, Cars.KmRate\n" +
                    "FROM Rents\n" +
                    "JOIN Cars ON Rents.CarID = Cars.ID\n" +
                    "WHERE Rents.ID = @rid;",
                    c
                );
                cmd.Parameters.AddWithValue("@rid", rid);
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        var start = r.GetDateTime(0);
                        var hr = r.GetDouble(1);
                        var kr = r.GetDouble(2);
                        var hrs = (end - start).TotalHours;
                        var cost = (hrs * hr) + (kms * kr);

                        var up = new SQLiteCommand(
                            "UPDATE Rents\n" +
                            "SET End = @end, Kms = @kms, Cost = @cost\n" +
                            "WHERE ID = @rid;",
                            c
                        );
                        up.Parameters.AddWithValue("@end", end);
                        up.Parameters.AddWithValue("@kms", kms);
                        up.Parameters.AddWithValue("@cost", cost);
                        up.Parameters.AddWithValue("@rid", rid);
                        up.ExecuteNonQuery();
                    }
                }
            }
        }

        public static string GetInfo(int rid)
        {
            using (var c = new SQLiteConnection(Conn))
            {
                c.Open();
                var cmd = new SQLiteCommand(
                    "SELECT\n" +
                    "    Rents.ID,\n" +
                    "    Clients.Name,\n" +
                    "    Cars.Model,\n" +
                    "    Rents.Start,\n" +
                    "    Rents.End,\n" +
                    "    Rents.Kms,\n" +
                    "    Rents.Cost\n" +
                    "FROM Rents\n" +
                    "JOIN Clients ON Rents.CID = Clients.ID\n" +
                    "JOIN Cars ON Rents.CarID = Cars.ID\n" +
                    "WHERE Rents.ID = @rid;",
                    c
                );
                cmd.Parameters.AddWithValue("@rid", rid);

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return $"Rent ID: {r.GetInt32(0)}, Client: {r.GetString(1)}, Car: {r.GetString(2)}, Start: {r.GetDateTime(3)}, End: {r.GetDateTime(4)}, Kms: {r.GetDouble(5)}, Cost: {r.GetDouble(6):F2}";
                    }
                }

                return "Rent not found.";
            }
        }
    }
}
