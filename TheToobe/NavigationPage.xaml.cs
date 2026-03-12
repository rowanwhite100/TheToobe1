namespace TheToobe;

using Microsoft.Data.Sqlite;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SQLite;
using System;
using System.Diagnostics;
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
    private async void OnInitiateClicked(object sender, EventArgs args) //asyncronous method
    {
        string origin = Regex.Replace(Station1_input.Text.Trim().ToLower(), @"\b([a-z])", m => m.Value.ToUpper());
        string dstination = Regex.Replace(Station2_input.Text.Trim().ToLower(), @"\b([a-z])", m => m.Value.ToUpper()); //regex conversion to how the database is formatted

        int Station1ID = CheckStation(origin);
        int Station2ID = CheckStation(dstination);
        
        if (Station1ID == -1 && Station2ID == -1)
        {
            await DisplayAlert("Error", "Neither station was found. Please check your spelling and try again.", "OK");
        }
        else if (Station1ID == -1)
        {
            await DisplayAlert("Error", "Station 1 was not found. Please check your spelling and try again.", "OK");
        }
        else if (Station2ID == -1)
        {
            OutputBox.Text = (origin + " " + dstination);
            await DisplayAlert("Error", "Station 2 was not found. Please check your spelling and try again.", "OK");  
        }
        else
        {

            OutputBox.Text = AstarTraversal(Station1ID, Station2ID);
            //navigate to the next page and pass the station
        }
    }
    private async void OneTextChanged(object sender, TextChangedEventArgs e)
    {
        string oldText = e.OldTextValue;
        string newText = e.NewTextValue;
        var standardRegexCharacters = new Regex("^[a-zA-Z0-9.' ]*$");
        try
        {
            if (!standardRegexCharacters.IsMatch(newText))
            {
                try
                {
                    Station1_input.Text = oldText;

                }
                catch
                {
                    Station1_input.Text = "";
                }
            }
        }
        catch
        {
            await DisplayAlert("error", "invalid text entered", "ok");
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
    private async void TwoTextChanged(object sender, TextChangedEventArgs e)
    {
        string oldText = e.OldTextValue;
        string newText = e.NewTextValue;
        var standardRegexCharacters = new Regex("^[a-zA-Z0-9.' ]*$");
        try
        {
            if (!standardRegexCharacters.IsMatch(newText))
            {
                try
                {
                    Station2_input.Text = oldText;

                }
                catch
                {
                    Station2_input.Text = "";
                }

                
            }
        }
        catch
        {
            await DisplayAlert("error", "invalid text entered", "ok");
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
    public static Dictionary<int, (double Latitude, double Longitude)> StationCoords = new Dictionary<int, (double Latitude, double Longitude)>();

    public Dictionary<int, List<(int, double)>> graph = BuildAdjacencyList();//initalises the graph as public
    public int CheckStation(string Station)
    {
        try //exeption handling
        {
            using var connection = new SqliteConnection("Data Source=\"C:\\Users\\rowan\\source\\repos\\NEA\\ToobeDataBase.db\""); //connecting to the database
            connection.Open();

            var sql = @"
                SELECT Name, Station_ID
                FROM Stations
                WHERE Name LIKE @StationInput
                ";
            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@StationInput", Station + "%");

            using var reader = command.ExecuteReader();
            
            if (reader.Read())
            {
                string name = reader.GetString(0);
                int id = reader.GetInt32(1);

                if (name == Station)
                {
                    return id;//returns stattion ID on found
                } 
            }
            else
            {
                DisplayAlert("error", $"'{Station}' not found in db", "OK");
                return -1;
            }
        
        }
        catch (Exception e)
        {
            // Display the exception
            DisplayAlert("sql", $"sql executed for '{e.Message}'", "OK");
        }

        DisplayAlert("error", $"something bad went worng for {Station}", "OK");
        return -1; //station not found
    }
    public static Dictionary<int, List<(int neighbour, double distance)>> BuildAdjacencyList()
    {
        var adjacencyList = new Dictionary<int, List<(int, double)>>();
        
        using var connection = new SqliteConnection("Data Source=\"C:\\Users\\rowan\\source\\repos\\NEA\\ToobeDataBase.db\""); //connecting to the database
        try
        {
            connection.Open();

            var ConnectionCommand = connection.CreateCommand();
            ConnectionCommand.CommandText = @"
            SELECT Station1, Station2
            FROM Connections
            ";//returns station connections IDs

            var PositionCommand = connection.CreateCommand();
            PositionCommand.CommandText = @"
            SELECT Station_ID,  Latitude, Longitude
            FROM Stations
            ";

           
            using var posReader = PositionCommand.ExecuteReader();
            while (posReader.Read())
            {
                int id = posReader.GetInt32(0);
                double lat = posReader.GetDouble(1);
                double lon = posReader.GetDouble(2);

                StationCoords[id] = (lat, lon);
            }
          

            using var reader = ConnectionCommand.ExecuteReader();
            while (reader.Read())
            {
                int startStation = reader.GetInt32(0);
                int endStation = reader.GetInt32(1);

                var (startLat, startLong) = StationCoords[startStation];
                var (endLat, endLong) = StationCoords[endStation];

                double distance = DistanceConverter(startLat, startLong, endLat, endLong);

                AddConnection(adjacencyList, startStation, endStation, distance);
                AddConnection(adjacencyList, endStation, startStation, distance);
                //stores the neigbours coords (at 4,5) THEN the current (at 6,7)

                //undirected grahp hence bothways
            }

            connection.Close();
        }
        catch (Exception e)
        {
            // Display the exception
            Console.Write($"Error {e.Message}");
        }

        return adjacencyList;
    }
    private static void AddConnection(Dictionary<int, List<(int station, double distance)>> graph, int startStat, int endStat, double distance) 
    { 
        if (!graph.TryGetValue(startStat, out var neighbors))
        {
            neighbors = new List<(int, double)>();
            graph[startStat] = neighbors;//new list at correct key
        }
        
        if (!neighbors.Any(x => x.station == endStat))//checks each station value x in niebours 
        {
            neighbors.Add((endStat,distance));
        }//this avoids duplicate entries
    }
    private string AstarTraversal(int origin, int destination)
    {
        double[] bestDistance = new double[310]; // array of best distances to each node from the source
        PriorityQueue<int, double> nodesToVisit = new PriorityQueue<int, double>(); //priority queue of nodes to visit organised by ID, heuristic + distance
        var nodePredeccesors = new Dictionary<int, List<int>>(); //array of predecessors for each node  

        for (int i = 0; i < bestDistance.Length; i++) //initialising data structures
        {
            bestDistance[i] = double.MaxValue; //set all values to max to represent infinity as graph is unvisited
            nodePredeccesors[i] = new List<int>(); //initialise the list of predecessors for each node
        }
        bestDistance[Convert.ToInt32(origin)] = 0; //distance to source is 0
        nodesToVisit.Enqueue(origin, 0); //enqueue origin with 0 distance

        //Dictionary<int, List<(int station, double distance)>> graph, int startStat, int endStat, double distance)
        //format of the adjacency list

        (double originLat, double originLong) = StationCoords[origin];
        (double DestinationLat, double DestinationLong) = StationCoords[destination];

        while (nodesToVisit.Count > 0)//A* traversal loop
        {
            nodesToVisit.TryDequeue(out int current, out double priority);//deques the next node and its priority

            if (bestDistance[current] >= priority)//handles duplicates by skipping them if they have a higher priority than the best distance (initally set to max)
            {
                foreach (var neighbour in graph[current])//explores all neighbours
                {
                    int neigbourStation = neighbour.Item1;
                    double nextDistance = neighbour.Item2;

                    (double neighbourLat, double neighbourLong) = StationCoords[neighbour.Item1];

                    double currentDistance = bestDistance[current];

                    double heuristic = DistanceConverter(DestinationLat,DestinationLong,neighbourLat,neighbourLong);

                    double neighbourPriority = Math.Round(currentDistance + nextDistance + heuristic, 0);

                    nodesToVisit.Enqueue(neigbourStation, neighbourPriority);

                    if (bestDistance[neigbourStation] > neighbourPriority)
                    {
                        bestDistance[neigbourStation] = neighbourPriority;

                        int[] Predecessors = nodePredeccesors[current].ToArray();
                        Predecessors = Predecessors.Append(current).ToArray(); //adds the current node to the list of predecessors for the neighbour

                        nodePredeccesors[neigbourStation] = Predecessors.ToList(); //updates the list of predecessors for the neighbour
                    }
                }
            }
        }

        int[] shortestPath = nodePredeccesors[destination].ToArray();
        shortestPath = shortestPath.Append(destination).ToArray();   

        return getOutput(shortestPath);
    }
    private static double DistanceConverter(double startLat, double startLong, double endLat, double endLong)
    {
        //this mwthod does distances, and can be used also ofor heuristic calculations
        double radius = 6378.137; //radius of the earth in km
        double pi = 3.14159;

        double dLat = (endLat * pi) / 180 - (startLat * pi) / 180; //latitude distance in rad
        double dLong = (endLong * pi) / 180 - (startLong * pi) / 180; //longitude distance (converts to radians)

        double Haversine = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(startLat * pi / 180) * Math.Cos(endLat * pi / 180) * Math.Sin(dLong / 2) * Math.Sin(dLong / 2); //the Haversine formula (square of half the length of a chord)
        double angle = 2 * Math.Atan2(Math.Sqrt(Haversine), Math.Sqrt(1 - Haversine)); // finds the central angle in the earth

        double d = angle * radius; //in km
        d = d * 1000; //convert to meters
        return Math.Round(d,3);
    }
    private string getOutput(int[] stationID)
    {//takes array of in IDs adn converts to path

        var stationNames = new Dictionary<int, string>();

        try
        {
            using var SQLconnection = new SqliteConnection("Data Source=\"C:\\Users\\rowan\\source\\repos\\NEA\\ToobeDataBase.db\""); //connecting to the database
            var getNameCommand = SQLconnection.CreateCommand();

            SQLconnection.Open();

            string[] path = stationID.Select((ID, index) => $"@{index}").ToArray();//gets id inputs and converts index for sql format

            getNameCommand.CommandText = $@"
            SELECT Station_ID, Name
            FROM Stations
            WHERE Station_ID IN ({string.Join(",", path)})
            ";//sql query   

            
            var lines = new List<int>();

            for (int i = 0; i < stationID.Length; i++)
            {
                getNameCommand.Parameters.AddWithValue(path[i], stationID[i]);
            }

            using var reader2 = getNameCommand.ExecuteReader();

            while (reader2.Read())
            {
                int id = reader2.GetInt32(0);
                string name = reader2.GetString(1);

                stationNames[id] = name;
            }
            SQLconnection.Close();


            SQLconnection.Open();

            var lineNames = new Dictionary<int, string>
            {
                {1, "Bakerloo Line"},
                {2, "Central Line" },
                {3, "Circle Line" },
                {4, "District Line" },
                {5, "East London Line" },
                {6, "Hammersmith & City Line" },
                {7, "Jubilee Line" },
                {8, "Metropolitan Line" },
                {9, "Northern Line" },
                {10, "Piccadilly Line" },
                {11, "Victoria Line" },
                {12, "Waterloo & City Line" },
                {13, "DLR" }
            };

            string output = "";

            int lastLine = -1;

            for (int i = 0; i < stationID.Length - 1; i++)
            {
                int station1 = stationID[i];
                int station2 = stationID[i + 1];

                var lineCommand = SQLconnection.CreateCommand();

                lineCommand.CommandText = @"
                SELECT Line
                FROM Connections
                WHERE 
                (Station1 = @station1 AND Station2 = @station2) OR (Station1 = @station2 AND Station2 = @station1)    
                ";

                lineCommand.Parameters.AddWithValue("@station1", station1);
                lineCommand.Parameters.AddWithValue("@station2", station2);

                int lineID = Convert.ToInt32(lineCommand.ExecuteScalar());

                if (lastLine == -1)
                {
                    output += $"FROM {stationNames[station1]} USING {lineNames[lineID]} TO ";
                }
                else if (lastLine != lineID)
                {
                    output += $"{stationNames[station1]} \nFROM {stationNames[station1]} USING {lineNames[lineID]} TO ";
                }
                
                if (i == stationID.Length - 2)
                {
                    output += $"{stationNames[station1]}";
                }

                lastLine = lineID;
            }
            
            return output;
            //return string.Join(" TO ", stationID.Select(id => stationNames[id]));
        }
        catch(Exception e)
        {
            DisplayAlert("big error", $"someone did a doodoo - \n{e.Message}", "sure thing buddy");
        }

        return "error no path returned";
    }
}