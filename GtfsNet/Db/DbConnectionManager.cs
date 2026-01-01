using System.Collections.Generic;
using System.IO;
using GtfsNet.Structs;
using SQLite;

namespace GtfsNet.Db;

public class DbConnectionManager
{
    private Dictionary<string, SQLiteConnection> _connections = new();
    private string _basicUserame;
    private DirectoryInfo _baseDirectory;

    public DbConnectionManager(DirectoryInfo baseDirectory)
    {
        _baseDirectory = baseDirectory;
        string dbPath = Path.Combine(_baseDirectory.FullName, "GtfsNet.db");
        bool isNew = !File.Exists(dbPath);
        if (isNew)
        {   
            CreateSchema( dbPath);
        }
        using var conn = new SQLiteConnection(dbPath);
        conn.Close();
        
    }

    /// <summary>
    /// Creates all the necessary db schemes 
    /// </summary>
    /// <param name="conn"></param>
    private void CreateSchema(string dbPath)
    {
        using var db = new SQLiteConnection(dbPath);
        db.CreateTable<Stop>();
        db.Close();
    }
}