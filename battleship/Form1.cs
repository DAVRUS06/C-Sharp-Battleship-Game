using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Media;
using System.IO;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace battleship
{
    public partial class Form1 : Form
    {
        // Arrays to hold the buttons of each board
        Button[,] EnemyArray = new Button[10, 10];
        Button[,] PlayerArray = new Button[10, 10];

        Fleet playerFleet = new Fleet();
        Fleet opponentFleet = new Fleet();

        // Arrays to hold the status of a zone
        bool[,] PlayerZones = new bool[10, 10];
        bool[,] EnemyZones = new bool[10, 10];

        // bools for ship placement
        bool PlacingShips = true;
        bool PlacingFirstClick = true;
        bool CarrierPlaced = false;
        bool BattleshipPlaced = false;
        bool CruiserPlaced = false;
        bool SubmarinePlaced = false;
        bool DestroyerPlaced = false;
        Coord head = new Coord(-1, -1);
        Coord tail = new Coord(-1, -1);
        bool up = false;
        bool down = false;
        bool left = false;
        bool right = false;
        Coord lastShot = new Coord(0,0);

        Coord[] Carrier = new Coord[5];
        Coord[] Battleship = new Coord[4];
        Coord[] Cruiser = new Coord[3];
        Coord[] Submarine = new Coord[3];
        Coord[] Destroyer = new Coord[2];

        string ipAddress = "";
        int port = 12345;
        bool server = false;

        IPAddress localAddress;
        TcpListener listener;
        TcpClient client;
        NetworkStream stream;
        StreamWriter writer;
        StreamReader reader;
        StreamReader readData;
        StreamWriter writeData;
        bool listeningForClients = true;
        bool gameover = false;


        public Form1()
        {
            BuildPlayerBoard();
            BuildEnemyBoard();
            FillArrays();
            DisableMyBoard();
            InitializeComponent();
        }

        // Game start
        private void StartGame()
        {
            // Need to set to true 
            PlacingShips = true;
            EnableMyBoard();
            labelPlayerCarrierStatus.Text = "Placing...";
            labelEnemyCarrierStatus.Text = "In Play";
            labelEnemyBattleshipStatus.Text = "In Play";
            labelEnemyCruiserStatus.Text = "In Play";
            labelEnemySubmarineStatus.Text = "In Play";
            labelEnemyDestroyerStatus.Text = "In Play";
        }

        // Handles the placing of the ships
        private void PlaceShips(Coord c)
        {
            if(!CarrierPlaced)
            {

                if(PlacingFirstClick)
                {
                    // Set the head equal to the coord
                    PlacingFirstClick = false;
                    head = c;
                    DisableMyBoard();
                    PathEnable("Carrier", head);
                }
                else
                {
                    tail = c;
                    ShipPOS(5, "Carrier");
                    DrawShip(4);
                    PlacingFirstClick = true;
                    CarrierPlaced = true;
                    labelPlayerCarrierStatus.Text = "In Play";
                    EnableMyBoard();
                    labelPlayerBattleshipStatus.Text = "Placing...";
                }
            }
            else if(!BattleshipPlaced)
            {
                if (PlacingFirstClick)
                {
                    // Set the head equal to the coord
                    PlacingFirstClick = false;
                    head = c;
                    DisableMyBoard();
                    PathEnable("Battleship", head);
                }
                else
                {
                    tail = c;
                    ShipPOS(4, "Battleship");
                    DrawShip(3);
                    PlacingFirstClick = true;
                    BattleshipPlaced = true;
                    labelPlayerBattleshipStatus.Text = "In Play";
                    EnableMyBoard();
                    labelPlayerCruiserStatus.Text = "Placing...";
                }
            }
            else if(!CruiserPlaced)
            {
                if (PlacingFirstClick)
                {
                    // Set the head equal to the coord
                    PlacingFirstClick = false;
                    head = c;
                    DisableMyBoard();
                    PathEnable("Cruiser", head);
                }
                else
                {
                    tail = c;
                    ShipPOS(3, "Cruiser");
                    DrawShip(2);
                    PlacingFirstClick = true;
                    CruiserPlaced = true;
                    labelPlayerCruiserStatus.Text = "In Play";
                    EnableMyBoard();
                    labelPlayerSubmarineStatus.Text = "Placing...";
                }
            }
            else if(!SubmarinePlaced)
            {
                if (PlacingFirstClick)
                {
                    // Set the head equal to the coord
                    PlacingFirstClick = false;
                    head = c;
                    DisableMyBoard();
                    PathEnable("Submarine", head);
                }
                else
                {
                    tail = c;
                    ShipPOS(3, "Submarine");
                    DrawShip(2);
                    PlacingFirstClick = true;
                    SubmarinePlaced = true;
                    labelPlayerSubmarineStatus.Text = "In Play";
                    EnableMyBoard();
                    labelPlayerDestroyerStatus.Text = "Placing...";
                }
            }
            else if(!DestroyerPlaced)
            {
                if (PlacingFirstClick)
                {
                    // Set the head equal to the coord
                    PlacingFirstClick = false;
                    head = c;
                    DisableMyBoard();
                    PathEnable("Destroyer", head);
                }
                else
                {
                    tail = c;
                    ShipPOS(2, "Destroyer");
                    DrawShip(1);
                    PlacingFirstClick = true;
                    DestroyerPlaced = true;
                    labelPlayerDestroyerStatus.Text = "In Play";
                    EnableMyBoard();
                }
            }

            if (CarrierPlaced && BattleshipPlaced && CruiserPlaced && SubmarinePlaced && DestroyerPlaced)
            {
                DisableMyBoard();
                playerFleet.Carrier = Carrier;
                playerFleet.Battleship = Battleship;
                playerFleet.Cruiser = Cruiser;
                playerFleet.Submarine = Submarine;
                playerFleet.Destroyer = Destroyer;
                if (server == false)
                    signalReady();
            }


        }

        // get ship positions
        private void ShipPOS(int n, string name)
        {
            Coord[] temp;
            if (n == 5)
                temp = new Coord[5];
            else if (n == 4)
                temp = new Coord[4];
            else if (n == 3)
                temp = new Coord[3];
            else if (n == 2)
                temp = new Coord[2];
            else
            {
                temp = new Coord[1];
            }


            int k = 0;
            if (tail.X < head.X)
            {
                // Went left
                for (int j = head.X; j >= tail.X; j--)
                {
                    PlayerZones[j, head.Y] = true;
                    PlayerArray[j, head.Y].Enabled = false;
                    PlayerArray[j, head.Y].BackColor = System.Drawing.Color.Green;
                    PlayerArray[j, head.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    Coord t = new Coord(j, head.Y);
                    temp[k] = t;
                    k++;
                }
            }
            else if (tail.X > head.X)
            {
                // Went Right
                for (int j = head.X; j <= tail.X; j++)
                {
                    PlayerZones[j, head.Y] = true;
                    PlayerArray[j, head.Y].Enabled = false;
                    PlayerArray[j, head.Y].BackColor = System.Drawing.Color.Green;
                    PlayerArray[j, head.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    Coord t = new Coord(j, head.Y);
                    temp[k] = t;
                    k++;
                }
            }
            else if (tail.Y < head.Y)
            {
                // Went up
                for (int i = head.Y; i >= tail.Y; i--)
                {
                    PlayerZones[head.X, i] = true;
                    PlayerArray[head.X, i].Enabled = false;
                    PlayerArray[head.X, i].BackColor = System.Drawing.Color.Green;
                    PlayerArray[head.X, i].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    Coord t = new Coord(head.X, i);
                    temp[k] = t;
                    k++;
                }
            }
            else if (tail.Y > head.Y)
            {
                // Went Down
                for (int i = head.Y; i <= tail.Y; i++)
                {
                    PlayerZones[head.X, i] = true;
                    PlayerArray[head.X, i].Enabled = false;
                    PlayerArray[head.X, i].BackColor = System.Drawing.Color.Green;
                    PlayerArray[head.X, i].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    Coord t = new Coord(head.X, i);
                    temp[k] = t;
                    k++;
                }

            }

            if (name.Equals("Carrier"))
                Carrier = temp;
            else if (name.Equals("Battleship"))
                Battleship = temp;
            else if (name.Equals("Cruiser"))
                Cruiser = temp;
            else if (name.Equals("Submarine"))
                Submarine = temp;
            else if (name.Equals("Destroyer"))
                Destroyer = temp;

            else
            {
                temp = new Coord[1];
            }
        }

        // private void draw ship
        private void DrawShip(int size)
        {
            if (up)
            {
                PlayerArray[head.X, head.Y - size].FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                up = false;
            }
            if(down)
            {
                PlayerArray[head.X, head.Y + size].FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                down = false;
            }
            if (left)
            {
                PlayerArray[head.X - size, head.Y].FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                left = false;
            }
            if (right)
            {
                PlayerArray[head.X + size, head.Y].FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                right = false;
            }



            if (tail.X < head.X)
            {
                // Went left
                for(int j = head.X; j >= tail.X; j--)
                {
                    PlayerZones[j, head.Y] = true;
                    PlayerArray[j, head.Y].Enabled = false;
                    PlayerArray[j, head.Y].BackColor = System.Drawing.Color.Green;
                    PlayerArray[j, head.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                }
            }
            else if(tail.X > head.X)
            {
                // Went Right
                for(int j = head.X; j <= tail.X; j++)
                {
                    PlayerZones[j, head.Y] = true;
                    PlayerArray[j, head.Y].Enabled = false;
                    PlayerArray[j, head.Y].BackColor = System.Drawing.Color.Green;
                    PlayerArray[j, head.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                }
            }
            else if(tail.Y < head.Y)
            {
                // Went up
                for(int i = head.Y; i >= tail.Y; i--)
                {
                    PlayerZones[head.X, i] = true;
                    PlayerArray[head.X, i].Enabled = false;
                    PlayerArray[head.X, i].BackColor = System.Drawing.Color.Green;
                    PlayerArray[head.X, i].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                }
            }
            else if(tail.Y > head.Y)
            {
                // Went Down
                for(int i = head.Y; i <= tail.Y; i++)
                {
                    PlayerZones[head.X, i] = true;
                    PlayerArray[head.X, i].Enabled = false;
                    PlayerArray[head.X, i].BackColor = System.Drawing.Color.Green;
                    PlayerArray[head.X, i].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                }

            }
        }

        // Enable paths for ships
        private void PathEnable(string ship, Coord c)
        {
            if(ship.Equals("Carrier"))
            {
                // Size 5
                //Check left
                if (c.X - 4 >= 0)
                {
                    // Check left
                    if (PlayerZones[c.X - 1, c.Y] == false && PlayerZones[c.X - 2, c.Y] == false && PlayerZones[c.X - 3, c.Y] == false && PlayerZones[c.X - 4, c.Y] == false)
                    {
                        left = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X - 4, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X - 4, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Right
                if (c.X + 4 <= 9)
                {
                    // Check left
                    if (PlayerZones[c.X + 1, c.Y] == false && PlayerZones[c.X + 2, c.Y] == false && PlayerZones[c.X + 3, c.Y] == false && PlayerZones[c.X + 4, c.Y] == false)
                    {
                        right = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X + 4, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X + 4, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Up
                if(c.Y - 4 >= 0)
                {
                    if(PlayerZones[c.X, c.Y - 1] == false && PlayerZones[c.X, c.Y - 2] == false && PlayerZones[c.X, c.Y - 3] == false && PlayerZones[c.X, c.Y - 4] == false)
                    {
                        up = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y - 4].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y - 4].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Down
                if (c.Y + 4 <= 9)
                {
                    if (PlayerZones[c.X, c.Y + 1] == false && PlayerZones[c.X, c.Y + 2] == false && PlayerZones[c.X, c.Y + 3] == false && PlayerZones[c.X, c.Y + 4] == false)
                    {
                        down = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y + 4].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y + 4].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }
            }
            else if (ship.Equals("Battleship"))
            {
                // Size 4
                //Check left
                if (c.X - 3 >= 0)
                {
                    // Check left
                    if (PlayerZones[c.X - 1, c.Y] == false && PlayerZones[c.X - 2, c.Y] == false && PlayerZones[c.X - 3, c.Y] == false)
                    {
                        left = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X - 3, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X - 3, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Right
                if (c.X + 3 <= 9)
                {
                    // Check left
                    if (PlayerZones[c.X + 1, c.Y] == false && PlayerZones[c.X + 2, c.Y] == false && PlayerZones[c.X + 3, c.Y] == false)
                    {
                        right = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X + 3, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X + 3, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Up
                if (c.Y - 3 >= 0)
                {
                    if (PlayerZones[c.X, c.Y - 1] == false && PlayerZones[c.X, c.Y - 2] == false && PlayerZones[c.X, c.Y - 3] == false)
                    {
                        up = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y - 3].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y - 3].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Down
                if (c.Y + 3 <= 9)
                {
                    if (PlayerZones[c.X, c.Y + 1] == false && PlayerZones[c.X, c.Y + 2] == false && PlayerZones[c.X, c.Y + 3] == false)
                    {
                        down = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y + 3].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y + 3].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }
            }
            else if (ship.Equals("Cruiser"))
            {
                // Size 3
                //Check left
                if (c.X - 2 >= 0)
                {
                    // Check left
                    if (PlayerZones[c.X - 1, c.Y] == false && PlayerZones[c.X - 2, c.Y] == false)
                    {
                        left = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X - 2, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X - 2, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Right
                if (c.X + 2 <= 9)
                {
                    // Check left
                    if (PlayerZones[c.X + 1, c.Y] == false && PlayerZones[c.X + 2, c.Y] == false)
                    {
                        right = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X + 2, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X + 2, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Up
                if (c.Y - 2 >= 0)
                {
                    if (PlayerZones[c.X, c.Y - 1] == false && PlayerZones[c.X, c.Y - 2] == false)
                    {
                        up = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y - 2].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y - 2].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Down
                if (c.Y + 2 <= 9)
                {
                    if (PlayerZones[c.X, c.Y + 1] == false && PlayerZones[c.X, c.Y + 2] == false)
                    {
                        down = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y + 2].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y + 2].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }
            }
            else if (ship.Equals("Submarine"))
            {
                // Size 3
                //Check left
                if (c.X - 2 >= 0)
                {
                    // Check left
                    if (PlayerZones[c.X - 1, c.Y] == false && PlayerZones[c.X - 2, c.Y] == false)
                    {
                        left = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X - 2, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X - 2, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Right
                if (c.X + 2 <= 9)
                {
                    // Check left
                    if (PlayerZones[c.X + 1, c.Y] == false && PlayerZones[c.X + 2, c.Y] == false)
                    {
                        right = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X + 2, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X + 2, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Up
                if (c.Y - 2 >= 0)
                {
                    if (PlayerZones[c.X, c.Y - 1] == false && PlayerZones[c.X, c.Y - 2] == false)
                    {
                        up = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y - 2].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y - 2].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Down
                if (c.Y + 2 <= 9)
                {
                    if (PlayerZones[c.X, c.Y + 1] == false && PlayerZones[c.X, c.Y + 2] == false)
                    {
                        down = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y + 2].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y + 2].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }
            }
            else if (ship.Equals("Destroyer"))
            {
                // Size 2
                //Check left
                if (c.X - 1 >= 0)
                {
                    // Check left
                    if (PlayerZones[c.X - 1, c.Y] == false)
                    {
                        left = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X - 1, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X - 1, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Right
                if (c.X + 1 <= 9)
                {
                    // Check left
                    if (PlayerZones[c.X + 1, c.Y] == false)
                    {
                        right = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X + 1, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X + 1, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Up
                if (c.Y - 1 >= 0)
                {
                    if (PlayerZones[c.X, c.Y - 1] == false)
                    {
                        up = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y - 1].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y - 1].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }

                // Check Down
                if (c.Y + 1 <= 9)
                {
                    if (PlayerZones[c.X, c.Y + 1] == false)
                    {
                        down = true;
                        PlayerArray[c.X, c.Y].Enabled = true;
                        PlayerArray[c.X, c.Y + 1].Enabled = true;
                        PlayerArray[c.X, c.Y].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                        PlayerArray[c.X, c.Y + 1].FlatAppearance.BorderColor = System.Drawing.Color.Green;
                    }
                }
            }

        }

        // My board disable
        private void DisableMyBoard()
        {
            for (int i = 0; i < 10; i++)
            {
                //columns
                for (int j = 0; j < 10; j++)
                {
                    PlayerArray[i, j].Enabled = false;
                    //PlayerArray[i, j].FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                }
            }
        }

        // My board enable
        private void EnableMyBoard()
        {
            for (int i = 0; i < 10; i++)
            {
                //columns
                for (int j = 0; j < 10; j++)
                {
                    if(!PlayerZones[i,j])
                    {
                        PlayerArray[i, j].Enabled = true;
                    }
                }

            }
        }

        // Handles the Play button
        private void Play(object sender, EventArgs e)
        {
            ipAddress = textBoxIP.Text;
            port = Int32.Parse(textBoxPort.Text);
            labelGameStatus.Text = "Playing";
            labelGameStatus.ForeColor = System.Drawing.Color.Green;
            button1.Enabled = false;
            buttonHost.Enabled = false;
            server = false;

            Thread t = new Thread(RunClient);
            t.Start();
            StartGame();
        }

        // Handles the button presses on the enemy's board
        private void EnemyZoneClick(object sender, EventArgs e)
        {
            // All button names for the enemy are "buttonX(letter)(number)" format, extract the letter and number from the name
            Button b = (Button)sender;
            string buttonName = b.Name;


            int x = (int)Char.GetNumericValue(buttonName[7]);
            int y = (int)Char.GetNumericValue(buttonName[8]);

            EnemyZones[x, y] = true;
            EnemyArray[x, y].Enabled = false;
            EDEnemyButtons(false);
            labelTurn.Text = "Their Turn";
            Coord newcoord = new Coord(x, y);
            Shoot(newcoord);
        }

        // shoot
        private void Shoot(Coord c)
        {
            msg m = new msg();
            m.type = "move";
            m.body = "move";
            m.name = textBoxUsername.Text;
            m.move = c;
            lastShot = c;
            
            string output = JsonConvert.SerializeObject(m);
            writer.WriteLine(output);
            writer.Flush();
        }

        // Handles the button presses on the player's board
        private void MyZoneClick(object sender, EventArgs e)
        {
            // All button names for the player are "button(letter)(number)" format, extract the letter and number from the name
            Button b = (Button)sender;
            string buttonName = b.Name;
            

            int x = (int)Char.GetNumericValue(buttonName[6]);
            int y = (int)Char.GetNumericValue(buttonName[7]);

            if (PlacingShips)
            {
                Coord temp = new Coord(x, y);
                PlaceShips(temp);
            }
            
        }

        // Handle the Chat send
        private void ChatSend(object sender, EventArgs e)
        {
            msg t = new msg();
            t.name = textBoxUsername.Text;
            t.body = textBoxChat.Text;
            t.type = "chat";
            t.move = new Coord(0,0);
            textBoxChat.Text = "";
            string output = JsonConvert.SerializeObject(t);
            writer.WriteLine(output);
            writer.Flush();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;
        }

        // Fills the arrays as empty
        private void FillArrays()
        {
            for(int i = 0; i < 10; i++)
            {
                for(int j = 0; j < 10; j++)
                {
                    PlayerZones[j, i] = false;
                    EnemyZones[j, i] = false;
                }
            }
        }

        // Builds the players board
        private void BuildPlayerBoard()
        {
            int width = 40;
            int height = 40;
            int x = 74;
            int y = 744;
            for(int i = 0; i < 10; i ++)
            {
                for(int j = 0; j < 10; j++)
                {
                    if (j != 0)
                        x += 60;
                    Button b = new Button();
                    b.Size = new Size(height, width);
                    b.Location = new Point(x, y);
                    b.Text = "";
                    b.BackColor = System.Drawing.Color.FromArgb(255, 30, 30, 30);
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                    b.FlatAppearance.BorderSize = 2;
                    b.UseVisualStyleBackColor = true;
                    b.TabStop = false;
                    b.Name = "button" + j.ToString() + i.ToString();
                    b.Click += MyZoneClick;
                    PlayerArray[j, i] = b;
                    this.Controls.Add(PlayerArray[j, i]);
                }
                y += 60;
                x = 74;
            }
        }

        // Builds the enemy board
        private void BuildEnemyBoard()
        {
            int width = 40;
            int height = 40;
            int x = 694;
            int y = 744;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (j != 0)
                        x += 60;
                    Button b = new Button();
                    b.Size = new Size(height, width);
                    b.Location = new Point(x, y);
                    b.Text = "";
                    b.BackColor = System.Drawing.Color.FromArgb(255, 30, 30, 30);
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(255, 200, 200, 200);
                    b.FlatAppearance.BorderSize = 2;
                    b.UseVisualStyleBackColor = true;
                    b.TabStop = false;
                    b.Name = "buttonX" + j.ToString() + i.ToString();
                    b.Click += EnemyZoneClick;
                    EnemyArray[j, i] = b;
                    this.Controls.Add(EnemyArray[j, i]);
                }
                y += 60;
                x = 694;
            }
        }

        // Say ready
        private void signalReady()
        {
            EDEnemyButtons(false);
            labelTurn.Text = "Their Turn";
            msg m = new msg();
            m.type = "ready";
            m.body = "ready";
            m.name = textBoxUsername.Text;
            m.move = new Coord(0, 0);
            string output = JsonConvert.SerializeObject(m);
            writer.WriteLine(output);
            writer.Flush();
        }

        private void RunClient()
        {
            ipAddress = textBoxIP.Text;
            client = new TcpClient(ipAddress, port);
            stream = client.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);

            string clientInput;
            try
            {
                while(!string.IsNullOrEmpty(clientInput = reader.ReadLine()))
                {
                    string output;
                    msg message = JsonConvert.DeserializeObject<msg>(clientInput);
                    msg reply = new msg();
                    reply.name = textBoxUsername.Text;
                    reply.move = new Coord(0, 0);

                    if (message.type.Equals("chat"))
                    {
                        string text = message.name + ": " + message.body + Environment.NewLine;
                        textBoxChatLog.Text += text;

                    }
                    else if (message.type.Equals("move"))
                    {
                        string text = message.name + " shot at X = " + message.move.X + " Y = " + message.move.Y + Environment.NewLine;
                        textBoxChatLog.Text += text;
                        
                        bool check = checkHit(message.move);
                        if (check)
                        {
                            reply.type = "hit";
                            reply.body = "hit";
                            PlayerArray[message.move.X, message.move.Y].BackColor = System.Drawing.Color.Red;
                        }
                        else
                        {
                            reply.type = "miss";
                            reply.body = "miss";
                            PlayerArray[message.move.X, message.move.Y].BackColor = System.Drawing.Color.White;
                        }
                        output = JsonConvert.SerializeObject(reply);
                        writer.WriteLine(output);
                        writer.Flush();
                        if (!gameover)
                        {
                            labelTurn.Text = "Your Turn";
                            // enable th enemy grid again.
                            EDEnemyButtons(true);
                        }
                    }
                    else if (message.type.Equals("quit"))
                    {
                        string text = message.name + " quits... " + Environment.NewLine;
                        ServerDisco();
                        break;
                    }
                    else if (message.type.Equals("sunk"))
                    {
                        if (message.body.Equals("carrier"))
                        {
                            labelEnemyCarrierStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy carrier sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("battleship"))
                        {
                            labelEnemyBattleshipStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy battleship sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("cruiser"))
                        {
                            labelEnemyCruiserStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy cruiser sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("submarine"))
                        {
                            labelEnemySubmarineStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy submarine sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("destroyer"))
                        {
                            labelEnemyDestroyerStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy destroyer sunk!" + Environment.NewLine;
                        }

                    }
                    else if (message.type.Equals("miss"))
                    {
                        EnemyArray[lastShot.X, lastShot.Y].BackColor = System.Drawing.Color.White;
                        textBoxChatLog.Text += "You missed!" + Environment.NewLine;
                    }
                    else if (message.type.Equals("hit"))
                    {
                        EnemyArray[lastShot.X, lastShot.Y].BackColor = System.Drawing.Color.Red;
                        textBoxChatLog.Text += "You hit!" + Environment.NewLine;
                    }
                    else if (message.type.Equals("won"))
                    {
                        labelTurn.Text = "YOU WON!!!";
                        //EDEnemyButtons(false);
                        MasterLock();
                    }
                    else if (message.type.Equals("ready"))
                    {
                        EDEnemyButtons(true);
                    }
                    if (writer == null)
                        break;
                }
            }
            catch(Exception e)
            {
                textBoxChatLog.Text += e.ToString();
            }


        }

        // server
        private void Server()
        {
            server = true;
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            listener.BeginAcceptTcpClient(handleClient, listener);
        }

        private void handleClient(IAsyncResult result)
        {
            if(listeningForClients)
            {
                client = listener.EndAcceptTcpClient(result);
                listeningForClients = false;
                ThreadPool.QueueUserWorkItem(runServer, client);
            }
        }

        private void runServer(object obj)
        {
            stream = client.GetStream();
            writer = new StreamWriter(stream);
            reader = new StreamReader(stream);
            string clientInput;

            try
            {
                while (!string.IsNullOrEmpty(clientInput = reader.ReadLine()))
                {
                    string output;
                    msg message = JsonConvert.DeserializeObject<msg>(clientInput);
                    msg reply = new msg();
                    reply.name = textBoxUsername.Text;
                    reply.move = new Coord(0, 0);

                    if (message.type.Equals("chat"))
                    {
                        string text = message.name + ": " + message.body + Environment.NewLine;
                        textBoxChatLog.Text += text;

                    }
                    else if (message.type.Equals("move"))
                    {
                        string text = message.name + " shot at X = " + message.move.X + " Y = " + message.move.Y + Environment.NewLine;
                        textBoxChatLog.Text += text;
                        bool check = checkHit(message.move);
                       
                        if (check)
                        {
                            reply.type = "hit";
                            reply.body = "hit";
                            PlayerArray[message.move.X, message.move.Y].BackColor = System.Drawing.Color.Red;
                        }
                        else
                        {
                            reply.type = "miss";
                            reply.body = "miss";
                            PlayerArray[message.move.X, message.move.Y].BackColor = System.Drawing.Color.White;
                        }
                        output = JsonConvert.SerializeObject(reply);
                        writer.WriteLine(output);
                        writer.Flush();
                        if(!gameover)
                        {
                            labelTurn.Text = "Your Turn";
                            // enable th enemy grid again.
                            EDEnemyButtons(true);
                        }
                        

                    }
                    else if (message.type.Equals("quit"))
                    {
                        string text = message.name + " quits... " + Environment.NewLine;
                        ServerDisco();
                        break;
                    }
                    else if (message.type.Equals("sunk"))
                    {
                        if (message.body.Equals("carrier"))
                        {
                            labelEnemyCarrierStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy carrier sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("battleship"))
                        {
                            labelEnemyBattleshipStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy battleship sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("cruiser"))
                        {
                            labelEnemyCruiserStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy cruiser sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("submarine"))
                        {
                            labelEnemySubmarineStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy submarine sunk!" + Environment.NewLine;
                        }
                        else if (message.body.Equals("destroyer"))
                        {
                            labelEnemyDestroyerStatus.Text = "Sunk";
                            textBoxChatLog.Text += "Enemy destroyer sunk!" + Environment.NewLine;
                        }
                            
                    }
                    else if (message.type.Equals("miss"))
                    {
                        EnemyArray[lastShot.X, lastShot.Y].BackColor = System.Drawing.Color.White;
                        textBoxChatLog.Text += "You missed!" + Environment.NewLine;
                    }
                    else if (message.type.Equals("hit"))
                    {
                        EnemyArray[lastShot.X, lastShot.Y].BackColor = System.Drawing.Color.Red;
                        textBoxChatLog.Text += "You hit!" + Environment.NewLine;
                    }
                    else if (message.type.Equals("won"))
                    {
                        labelTurn.Text = "YOU WON!!!";
                        //EDEnemyButtons(false);
                        MasterLock();
                    }
                    else if (message.type.Equals("ready"))
                    {
                        EDEnemyButtons(true);
                        labelTurn.Text = "Your Turn";
                        textBoxChatLog.Text += message.name + " is ready to play." + Environment.NewLine;
                    }
                        if (writer == null)
                        break;
                }

            }
            catch (Exception e)
            {
                textBoxChatLog.Text += e.ToString();
            }
        }

        private void ServerDisco()
        {
            try
            {
                if (writer != null)
                    writer = null;
                if (stream != null)
                    stream = null;
                if (reader != null)
                    reader = null;
                if (client != null)
                    client.Close();

            }
            catch
            {

            }
        }

        private void Sunk(string name)
        {
            msg n = new msg();
            n.type = "sunk";
            n.name = textBoxUsername.Text;
            n.move = new Coord();
            n.body = name;
            string output = JsonConvert.SerializeObject(n);
            writer.WriteLine(output);
            writer.Flush();

            if(playerFleet.CarrierSunk == true && playerFleet.BattleshipSunk == true 
                && playerFleet.CruiserSunk == true && playerFleet.SubmarineSunk == true 
                && playerFleet.DestroyerSunk == true)
            {
                msg w = new msg();
                w.type = "won";
                w.name = textBoxUsername.Text;
                w.move = new Coord();
                n.body = "Winner Winner!!";
                string outputWin = JsonConvert.SerializeObject(w);
                writer.WriteLine(outputWin);
                writer.Flush();
                labelTurn.Text = "You Lost.";
                gameover = true;
                MasterLock();
            }
        }

        private void MasterLock()
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    EnemyArray[j, i].Enabled = false;
                }
            }
        }

        private bool checkHit(Coord c)
        {
            // check carrier
            for(int i = 0; i < 5; i++)
            {
                if (Carrier[i].X == c.X && Carrier[i].Y == c.Y)
                {
                    Carrier[i].Hit = true;

                    bool sunk = true;
                    for (int j = 0; j < 5; j++)
                    {
                        if (Carrier[j].Hit == false)
                            sunk = false;
                    }
                    if (sunk)
                    {
                        labelPlayerCarrierStatus.Text = "Sunk";
                        playerFleet.CarrierSunk = true;
                        Sunk("carrier");
                        
                    }
                    return true;
                }
                    
            }
            // check battleship
            for (int i = 0; i < 4; i++)
            {
                if (Battleship[i].X == c.X && Battleship[i].Y == c.Y)
                {
                    Battleship[i].Hit = true;
                    bool sunk = true;
                    for (int j = 0; j < 4; j++)
                    {
                        if (Battleship[j].Hit == false)
                            sunk = false;
                    }
                    if (sunk)
                    {
                        labelPlayerBattleshipStatus.Text = "Sunk";
                        playerFleet.BattleshipSunk = true;
                        Sunk("battleship");
                        
                    }
                    return true;
                }
            }
            // check cruiser
            for (int i = 0; i < 3; i++)
            {
                if (Cruiser[i].X == c.X && Cruiser[i].Y == c.Y)
                {
                    Cruiser[i].Hit = true;
                    bool sunk = true;
                    for (int j = 0; j < 3; j++)
                    {
                        if (Cruiser[j].Hit == false)
                            sunk = false;
                    }
                    if (sunk)
                    {
                        labelPlayerCruiserStatus.Text = "Sunk";
                        playerFleet.CruiserSunk = true;
                        Sunk("cruiser");
                        
                    }
                    return true;
                }
            }
            // Check submarine
            for (int i = 0; i < 3; i++)
            {
                if (Submarine[i].X == c.X && Submarine[i].Y == c.Y)
                {
                    Submarine[i].Hit = true;
                    bool sunk = true;
                    for (int j = 0; j < 3; j++)
                    {
                        if (Submarine[j].Hit == false)
                            sunk = false;
                    }
                    if (sunk)
                    {
                        labelPlayerSubmarineStatus.Text = "Sunk";
                        playerFleet.SubmarineSunk = true;
                        Sunk("submarine");
                        
                    }
                    return true;
                }
            }
            // Check destroyer
            for (int i = 0; i < 2; i++)
            {
                if (Destroyer[i].X == c.X && Destroyer[i].Y == c.Y)
                {
                    Destroyer[i].Hit = true;
                    bool sunk = true;
                    for (int j = 0; j < 2; j++)
                    {
                        if (Destroyer[j].Hit == false)
                            sunk = false;
                    }
                    if (sunk)
                    {
                        labelPlayerDestroyerStatus.Text = "Sunk";
                        playerFleet.DestroyerSunk = true;
                        Sunk("destroyer");
                        
                    }
                    return true;
                }
            }

            return false;
        }

        private void buttonHost_Click(object sender, EventArgs e)
        {
            Server();
            buttonHost.Enabled = false;
            button1.Enabled = false;
            StartGame();
            labelGameStatus.Text = "Playing";
            labelGameStatus.ForeColor = System.Drawing.Color.Green;
            EDEnemyButtons(false);
        }

        private void EDEnemyButtons(bool b)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    if(EnemyZones[j, i] == false)
                        EnemyArray[j, i].Enabled = b;
                }
            }
        }

        private void textBoxChatLog_TextChanged(object sender, EventArgs e)
        {
            textBoxChatLog.SelectionStart = textBoxChatLog.Text.Length;
            textBoxChatLog.ScrollToCaret();
        }
    }
}
