namespace TheToobe;

using Microsoft.Data.Sqlite;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SQLite;
using System;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using static SQLite.SQLite3;

public partial class NavigationPage : ContentPage //inheritance
{
    //start of UI code
    public NavigationPage()
    {
        InitializeComponent();
    }
    private void OnInitiateClicked(object sender, EventArgs args)
    {

        string origin = Regex.Replace(Station1_input.Text.ToLower(), @"\b([a-z])", m => m.Value.ToUpper());
        string dstination = Regex.Replace(Station2_input.Text.ToLower(), @"\b([a-z])", m => m.Value.ToUpper()); //regex conversion to how the database is formatted

        int Station1ID = CheckStation(origin);
        int Station2ID = CheckStation(dstination);

        if (Station1ID == -1 || Station2ID == -1)
        {
            DisplayAlert("Error", "One or both of the stations entered were not found. Please check your spelling and try again.", "OK");
        }
        else
        {
            AstarTraversal(Station1ID, Station2ID);
            //navigate to the next page and pass the station
        }
    }
    private void OneTextChanged(object sender, TextChangedEventArgs e)
    {
        string oldText = e.OldTextValue;
        string newText = e.NewTextValue;
        var standardRegexCharacters = new Regex("^[a-zA-Z0-9 ]*$");

        if (!standardRegexCharacters.IsMatch(newText))
        {
            Station1_input.Text = oldText;
        }

        if (!string.IsNullOrWhiteSpace(Station2_input.Text) && !string.IsNullOrWhiteSpace(Station1_input.Text))
        {
            Initate_button.IsEnabled = true;
            Initate_button.BackgroundColor = Color.FromArgb("#FFFFFF");//to fix
        }
        else
        {
            Initate_button.IsEnabled = false;
            Initate_button.BackgroundColor = Color.FromArgb("#838D93");
        } 
    }
    private void TwoTextChanged(object sender, TextChangedEventArgs e)
    {
        string oldText = e.OldTextValue;
        string newText = e.NewTextValue;
        var standardRegexCharacters = new Regex("^[a-zA-Z0-9 ]*$");

        if (!standardRegexCharacters.IsMatch(newText))
        {
            Station2_input.Text = oldText;
        }

        if (!string.IsNullOrWhiteSpace(Station2_input.Text) && !string.IsNullOrWhiteSpace(Station1_input.Text))
        {
            Initate_button.IsEnabled = true;
            Initate_button.BackgroundColor = Color.FromArgb("#FFFFFF");
        }
        else
        {
            Initate_button.IsEnabled = false;
            Initate_button.BackgroundColor = Color.FromArgb("#838D93");
        }
    }

    //start of data proccessing 

    public Dictionary<int, List<int>> graph = BuildAdjacencyList();


    public int CheckStation(string Station)
    {
        try //exeption handling
        {
            var sql = @"
                SELECT Name, StationID
                FROM Stations
                WHERE Name = @StationInput
                ";
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ToobeDataBase.db");
            using var connection = new SqliteConnection(dbPath); //connecting to the database
            connection.Open();
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@StationInput", Station);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                string name = reader.GetString(0);
                int id = reader.GetInt32(1);

                if (name == Station)
                {
                    return id;
                }
                    
            }

            return -1; //station not found
        }
        catch (SqliteException e)
        {
            // Display the exception
            Console.WriteLine(e.Message);
        }

        return -1; //station not found
    }
    public static Dictionary<int, List<int>> BuildAdjacencyList()
    {
        var adjacencyList = new Dictionary<int, List<int>>();
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ToobeDataBase.db");
        using var connection = new SqliteConnection(dbPath); //connecting to the database
        try
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Station1, Station2
            FROM Connections
            ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int start = reader.GetInt32(0);
                int end = reader.GetInt32(1);

                AddConnection(adjacencyList, start, end);
                AddConnection(adjacencyList, end, start);
                //undirected grahp hence bothways
            }

            connection.Close();
        }
        catch (SqliteException e)
        {
            // Display the exception
            Console.WriteLine(e.Message);
        }


        return adjacencyList;
    }
    private static void AddConnection(Dictionary<int, List<int>> graph, int start, int end)
    {
        if (!graph.TryGetValue(start, out var neighbors))
        {
            neighbors = new List<int>();
            graph[start] = neighbors;
        }

        if (!neighbors.Contains(end))
        {
            neighbors.Add(end);
        }//this avoids duplicate entries
    }
    private void AstarTraversal(int origin, int destination)
    {
        int[] bestDistance = new int[310]; // array of best distances to each node from the source
        PriorityQueue<int, int> nodesToVisit = new PriorityQueue<int, int>(); //priority queue of nodes to visit organised by ID, heuristic + distance
        int[] predecessors = new int[310]; //array of predecessors for each node

        for (int i = 0; i < bestDistance.Length; i++) //initialising best distances to infinity
        {
            bestDistance[i] = -1; //set all values to -1 to represent infinity as graph is unvisited
        }
        bestDistance[Convert.ToInt32(origin)] = 0; //distance to source is 0

        nodesToVisit.Enqueue(origin, 0); //enqueue origin with 0 distance


    }
}