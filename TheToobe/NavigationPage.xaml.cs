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
            OutputBox.Text =  AstarTraversal(Station1ID, Station2ID);
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
    public Dictionary<int, List<int>> graph = BuildAdjacencyList();//initalises the graph as public


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
    private string AstarTraversal(int origin, int destination)
    {
        int[] bestDistance = new int[310]; // array of best distances to each node from the source
        PriorityQueue<int, int> nodesToVisit = new PriorityQueue<int, int>(); //priority queue of nodes to visit organised by ID, heuristic + distance
        var nodePredeccesors = new Dictionary<int, List<int>>(); //array of predecessors for each node  
        
        for (int i = 0; i < bestDistance.Length; i++) //initialising data structures
        {
            bestDistance[i] = int.MaxValue; //set all values to max to represent infinity as graph is unvisited
            nodePredeccesors[i] = new List<int>(); //initialise the list of predecessors for each node
        }
        bestDistance[Convert.ToInt32(origin)] = 0; //distance to source is 0
        nodesToVisit.Enqueue(origin, 0); //enqueue origin with 0 distance

        double[] destinationLatLong = getLatLong(destination);
        double destinationLat = destinationLatLong[0];
        double destinationLong = destinationLatLong[1];

        double[] originLatLong = getLatLong(origin);
        double originLat = originLatLong[0];
        double originLong = originLatLong[1];

        while (nodesToVisit.Count > 0)//A* traversal loop
        {
            nodesToVisit.TryDequeue(out int current, out int priority);//deques the next node and its priority

            if (bestDistance[current] >= priority)//handles duplicates by skipping them if they have a higher priority than the best distance (initally set to max)
            {
                double[] currentLatLong = getLatLong(current);
                double currentLat = currentLatLong[0];
                double currentLong = currentLatLong[1];

                double currentDistance = DistanceConverter(originLat, originLong, currentLat, currentLong);

                int[] neighbours = graph[current].ToArray(); //gets the neighbours of the current node

                foreach (int i in neighbours)//explores all neighbours
                {
                    double[] neighbourLatLong = getLatLong(i);
                    double neighbourLat = neighbourLatLong[0];
                    double neighbourLong = neighbourLatLong[1];

                    double heuristic = DistanceConverter(neighbourLat, neighbourLong, destinationLat, destinationLong);
                    double nextDistance = DistanceConverter(currentLat, currentLong, neighbourLat, neighbourLong);

                    double neighbourPriority = Math.Round(currentDistance + nextDistance + heuristic, 0);

                    nodesToVisit.Enqueue(i, Convert.ToInt16(neighbourPriority));

                    if (bestDistance[i] > neighbourPriority)
                    {
                        bestDistance[i] = Convert.ToInt16(neighbourPriority);

                        int[] Predecessors = nodePredeccesors[current].ToArray();
                        Predecessors = Predecessors.Append(current).ToArray(); //adds the current node to the list of predecessors for the neighbour

                        nodePredeccesors[i] = Predecessors.ToList(); //updates the list of predecessors for the neighbour
                    }
                }

            }
          
        }

        int[] shortestPath = nodePredeccesors[destination].ToArray(); 
        shortestPath = shortestPath.Append(destination).ToArray();
        string pathString = string.Join(" -> ", shortestPath);

        return pathString;
    }

    private double[] getLatLong(int ID)
    {
        try
        {
            var sql = @"
                        SELECT Latitude, Longitude
                        FROM Stations
                        WHERE Name = @StationInput
                        ";
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "ToobeDataBase.db");
            using var connection = new SqliteConnection(dbPath);
            connection.Open();

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@StationInput", ID);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                double Lat = reader.GetDouble(0);
                double Long = reader.GetDouble(1);
                connection.Close();
                return new double[] { Lat, Long };
            }
            
            connection.Close();
         
        }
        catch (SqliteException e)
        {
            // Display the exception
            Console.WriteLine(e.Message);
        }
        return new double[] {}; //error case
    }
    private double DistanceConverter(double startLat, double startLong, double endLat, double endLong)
    {
        //this mwthod does distances, and can be used also ofor heuristic calculations
        double radius = 6378.137; //radius of the earth in km
        double pi = 3.14159;

        double dLat = (endLat * pi) / 180 - (startLat * pi) / 180; //latitude distance (converts to radians)
        double dLong = (endLong * pi) / 180 - (startLong * pi) / 180; //longitude distance (converts to radians)

        double Haversine = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(startLat * pi / 180) * Math.Cos(endLat * pi / 180) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2); //the Haversine formula (square of half the length of a chord)
        double angle = 2 * Math.Atan2(Math.Sqrt(Haversine), Math.Sqrt(1 - Haversine)); // finds the central angle in the earth

        double d = angle * radius; //in km
        d = d * 1000; //convert to meters
        return Math.Round(d,3);
    }

}