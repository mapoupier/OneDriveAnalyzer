using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

class Program
{
    static void Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : @"C:\Users\mapoupier\OneDrive" ;
        string databasePath = "fileIndex.db";

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            connection.Open();
            CreateDatabaseSchema(connection);

            var files = EnumerateFiles(path);
            var fileCategories = CategorizeFiles(files);

            InsertFilesIntoDatabase(connection, fileCategories);

            Console.WriteLine("File indexing complete.");
        }
    }

    static void CreateDatabaseSchema(IDbConnection connection)
    {
        string createTableSql = @"
            CREATE TABLE Files (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                Size INTEGER NOT NULL,
                Extension TEXT NOT NULL,
                Category TEXT NOT NULL
            );
        ";

        connection.Execute(createTableSql);
    }

    static List<FileInfo> EnumerateFiles(string path)
    {
        var files = new List<FileInfo>();
        var directories = new Stack<string>(new[] { path });

        while (directories.Count > 0)
        {
            var currentDirectory = directories.Pop();
            try
            {
                files.AddRange(Directory.EnumerateFiles(currentDirectory).Select(f => new FileInfo(f)));

                foreach (var subDirectory in Directory.EnumerateDirectories(currentDirectory))
                {
                    directories.Push(subDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing {currentDirectory}: {ex.Message}");
            }
        }

        return files;
    }

    static List<(FileInfo File, string Category)> CategorizeFiles(List<FileInfo> files)
    {
        var categories = new List<(FileInfo, string)>();

        foreach (var file in files)
        {
            string category = file.Extension.ToLower() switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "Picture",
                ".doc" or ".docx" => "Word Document",
                ".xls" or ".xlsx" => "Excel Document",
                ".pdf" => "PDF Document",
                _ => "Other"
            };

            categories.Add((file, category));
        }

        return categories;
    }

    static void InsertFilesIntoDatabase(IDbConnection connection, List<(FileInfo File, string Category)> files)
    {
        string insertSql = @"
            INSERT INTO Files (FileName, FilePath, Size, Extension, Category)
            VALUES (@FileName, @FilePath, @Size, @Extension, @Category);
        ";

        foreach (var (file, category) in files)
        {
            connection.Execute(insertSql, new
            {
                FileName = file.Name,
                FilePath = file.FullName,
                Size = file.Length,
                Extension = file.Extension,
                Category = category
            });
        }
    }
}
