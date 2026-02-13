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

public partial class NavigationPage : ContentPage //inheritance
{
    public Dictionary<string, List<string>> list = BuildAdjacencyList();
    public NavigationPage()
    {
        InitializeComponent();
    }
    private void OnInitiateClicked(object sender, EventArgs args)
    {

        string station1 = Regex.Replace(Station1_input.Text.ToLower(), @"\b([a-z])", m => m.Value.ToUpper());
        string station2 = Regex.Replace(Station2_input.Text.ToLower(), @"\b([a-z])", m => m.Value.ToUpper()); //regex conversion to how the database is formatted


        //try //exeption handling
        //{
        //    // SELECT “StartStation”, “EndStation” FROM “StationConnections” WHERE “StartStation” = x OR “EndStation” = x
        //    var sql = @"
        //        SELECT Station1, Station2
        //        FROM Connections
        //        WHERE Station1 = @StationInput OR Station2 = @StationInput
        //        ";
        //    using var connection = new SqliteConnection("Data Source=\"C:\\Users\\rowan\\source\\repos\\NEA\\ToobeDataBase.db\""); //connecting to the database
        //    connection.Open();
        //    using var command = new SqliteCommand(sql, connection);
        //    command.Parameters.AddWithValue("@StationInput", station1);

        //    using var reader = command.ExecuteReader();

        //    var outputLines = new System.Text.StringBuilder();
        //    while (reader.Read())
        //    {
        //        var left = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
        //        var right = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
        //        outputLines.AppendLine($"{left} - {right}");
        //    }

        //    OutputBox.Text = outputLines.ToString().TrimEnd();



        //    connection.Close();

        //}
        //catch (SqliteException e)
        //{
        //    // Display the exception
        //    Console.WriteLine(e.Message);
        //}

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
    public static Dictionary<string, List<string>> BuildAdjacencyList()
    {
        var adjacencyList = new Dictionary<string, List<string>>();
        using var connection = new SqliteConnection("Data Source=\"C:\\Users\\rowan\\source\\repos\\NEA\\ToobeDataBase.db\""); //connecting to the database
        try
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
            SELECT Station1, Station2
            FROM Connections;
        ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string start = reader.GetString(0);
                string end = reader.GetString(1);

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
    private static void AddConnection(Dictionary<string, List<string>> graph, string start, string end)
    {
        if (!graph.TryGetValue(start, out var neighbors))
        {
            neighbors = new List<string>();
            graph[start] = neighbors;
        }

        if (!neighbors.Contains(end))
        {
            neighbors.Add(end);
        }//this avoids duplicate entries
    }
    private void AstarTraversal(string Station1, string Station2)
    {
        int[] bestDistance = new int[303]; // array of best distances to each node from the source
        PriorityQueue<string, int> nodesToVisit = new PriorityQueue<string, int>(); //priority queue of nodes to visit organised by heuristic + distance
        int[] predecessors = new int[303]; //array of predecessors for each node

        for (int i = 0; i < bestDistance.Length; i++) //initialising best distances to infinity
        {
            bestDistance[i] = -1; //set all values to -1 to represent infinity as graph is unvisited
            bestDistance[Convert.ToInt32(Station1)] = 0; //distance to source is 0
        }

        try //exeption handling
        {
            using var connection = new SqliteConnection("Data Source=\"C:\\Users\\rowan\\source\\repos\\NEA\\ToobeDataBase.db\""); //connecting to the database
            connection.Open();

            connection.Close();
        }
        catch (SqliteException e)
        {
            // Display the exception
            Console.WriteLine(e.Message);
        }

        //take out all the connections and order by smallest staion number fisrst the remove any duplicaates
        //this also allows for a true adgecency list to be created for easyer querying later
    }



}


class Station
{ 
    private string Station_Name { get; set; }
    private string Id { get; set; }
    private float Latitude { get; set; }
    private float Longitude { get; set; }
    private float zone { get; set; }

    public Station(string StationInput)
    {

        

    }

}


