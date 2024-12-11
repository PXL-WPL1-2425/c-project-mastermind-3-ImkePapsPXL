using Microsoft.VisualBasic;
using System;
using System.Diagnostics.Eventing.Reader;
using System.IO.Packaging;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
using WpfLabel = System.Windows.Controls.Label;



namespace WpfApp
{
    public partial class MainWindow : Window
    {
        private WpfLabel[] labels;
        private Brush[] targetColors = new Brush[4];
        private Brush[] predefinedColors = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Blue, Brushes.Yellow, Brushes.Orange, Brushes.White };
        private Dictionary<Brush, string> colorName = new Dictionary<Brush, string>
    {
        { Brushes.Red, "Red" },
        { Brushes.Green, "Green" },
        { Brushes.Blue, "Blue" },
        { Brushes.Yellow, "Yellow" },
        { Brushes.Orange, "Orange" },
        { Brushes.White, "White" }
    };

        private int score = 100;
        private int attempts = 0;
        private int remainingAttempts = 10;
        private List<(string playerName, int attempts, int score)> highScores = new List<(string playerName, int attempts, int score)>();
        private string playerName;
        private int currentPlayerIndex = 0;
        private List<string> playerNames = new List<string>();
        bool match = true;
        private string currentPlayer = string.Empty; 
        private string nextPlayer = string.Empty;

        DispatcherTimer timer = new DispatcherTimer();
        TimeSpan elapsedTime;
        DateTime startTime;

        public MainWindow()
        {
            InitializeComponent();
            labels = new WpfLabel[] { label1, label2, label3, label4 };
            GenerateTargetColors();
            StartGame();
            StartCountDown();
            this.Title = $"{currentPlayer}";
        }
        private void GenerateTargetColors()
        {
            Random rand = new Random();
            for (int i = 0; i < targetColors.Length; i++)
            {
                targetColors[i] = predefinedColors[rand.Next(predefinedColors.Length)];
            }
        }
        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WpfLabel clickedLabel = (WpfLabel)sender;
            int currentIndex = Array.IndexOf(predefinedColors, clickedLabel.Background);
            int nextIndex = (currentIndex + 1) % predefinedColors.Length;
            clickedLabel.Background = predefinedColors[nextIndex];
        }
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            CheckCode();
            StartCountDown();
            attempts++;
            this.Title = $"{playerName}";
        }
        private void NextPlayer()
        {
            // Controleer of de lijst leeg is
            if (playerNames == null || playerNames.Count == 0)
            {
                MessageBox.Show("Geen spelers meer in de lijst. Het spel wordt afgesloten.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            currentPlayer = playerNames[0];

            if (playerNames.Count > 1)
            {
                playerNames.RemoveAt(0); 
                playerNames.Add(currentPlayer); 
                nextPlayer = playerNames[0]; 
            }
            else
            {
                nextPlayer = "Geen volgende speler";
            }
            //Debugging();
            this.Title = $"Speler: {currentPlayer}'s beurt";
            ResetGame();
        }
        private void Debugging()
        {
            string debugInfo = $"Huidige spelerslijst: {string.Join(", ", playerNames)}";
            MessageBox.Show(debugInfo, "Debug Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private bool gameOver = false;
        private void CheckCode()
        {
            if (gameOver) return; 

            int correctColors = 0;
            int correctPositions = 0;
            int totalPenalty = 0;
            match = true; 
            string colorCode = string.Join(", ", targetColors.Select(color => colorName[color]));

            for (int i = 0; i < labels.Length; i++)
            {
                if (labels[i].Background == targetColors[i])
                {
                    correctPositions++;
                    totalPenalty += 0;
                }
                else if (targetColors.Contains(labels[i].Background))
                {
                    correctColors++;
                    totalPenalty += 1;
                    match = false;
                }
                else
                {
                    totalPenalty += 2;
                    match = false;
                }
            }

            score -= totalPenalty;
            if (score < 0) score = 0;

            AddAttemptToList(correctPositions, correctColors);

            if (match)
            {
                gameOver = true; 
                MessageBox.Show($"Code is gekraakt in {attempts} pogingen! Nu is het {nextPlayer} aan de beurt.",
                                "Gefeliciteerd!", MessageBoxButton.OK, MessageBoxImage.Information);
                NextPlayer();
                timer.Stop();
                UpdateHighScores(score);
            }
            else if (remainingAttempts <= 0 || score <= 0)
            {
                gameOver = true; 
                MessageBox.Show($"De code was {colorCode}. Nu is het {nextPlayer} aan de beurt.",
                                "Helaas!", MessageBoxButton.OK, MessageBoxImage.Information);
                timer.Stop();
                UpdateHighScores(score);
                NextPlayer();
            }
            else
            {
                remainingAttempts--;
                UpdateHighScores(score);
            }

            scoreLabel.Content = $"Score: {score}";
        }

        private void UpdateHighScores(int newScore)
        {
            highScores.Add((playerName, attempts, score));
            highScores = highScores.OrderByDescending(x => x.score).Take(15).ToList();
            if (highScores.Count > 15)
            {
                highScores.RemoveAt(0);
            }
        }
        private void AddAttemptToList(int correctPositions, int correctColors)
        {
            StackPanel attemptPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5)
            };

            foreach (var label in labels)
            {
                WpfLabel colorLabel = new WpfLabel
                {
                    Width = 30,
                    Height = 30,
                    Background = label.Background,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Black,
                    Margin = new Thickness(2)
                };
                attemptPanel.Children.Add(colorLabel);
            }
            for (int i = 0; i < correctPositions; i++)
            {
                WpfLabel feedbackLabel = new WpfLabel
                {
                    Width = 20,
                    Height = 20,
                    Background = Brushes.Red,
                    Margin = new Thickness(2),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2)
                };
                attemptPanel.Children.Add(feedbackLabel);
            }
            for (int i = 0; i < correctColors; i++)
            {
                WpfLabel feedbackLabel = new WpfLabel
                {
                    Width = 20,
                    Height = 20,
                    Background = Brushes.White,
                    Margin = new Thickness(2),
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(2)
                };
                attemptPanel.Children.Add(feedbackLabel);
            }
            attemptsPanel.Children.Add(attemptPanel);
        }
        private void StartCountDown()
        {
            startTime = DateTime.Now;
            timer.Tick += Timer_Tick;
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }
        private void StopCountDown()
        {
            timer.Stop();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            elapsedTime = DateTime.Now - startTime;
            timerLabel.Content = elapsedTime.ToString("ss");

            if (elapsedTime.TotalSeconds > 10)
            {
                StopCountDown();
                attempts++;
                this.Title = $"{playerName}";
                StartCountDown();
                if (attempts >= remainingAttempts)
                {
                    timer.Stop();
                    string colorCode = string.Join(", ", targetColors.Select(color => colorName[color]));
                    MessageBox.Show($"Geen pogingen meer. De code was {colorCode}.", "FAILED", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        private void ToggleDebug()
        {
#if DEBUG
            debugLabel.Visibility = debugLabel.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;

            debugLabel.Content = $"Doelkleurencode: {string.Join(", ", targetColors.Select(color => colorName[color]))}";
#endif
        }
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F12 && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                ToggleDebug();
            }
        }
        public void ShowGeneratedCode()
        {
#if DEBUG
            debugLabel.Content = $" {string.Join(", ", targetColors.Select(color => colorName[color]))}";
#endif
        }
        public void StartGame()
        {
            playerNames = new List<string>();

            while (true)
            {
                string name = Interaction.InputBox("Wat is uw naam?", "Welkom", "").Trim();

                while (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("De naam mag niet leeg zijn. Probeer het opnieuw.", "Foutieve invoer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    name = Interaction.InputBox("Wat is uw naam?", "Welkom", "").Trim();
                }
                playerNames.Add(name);
               
                MessageBoxResult answer = MessageBox.Show("Speelt er nog een speler mee?", "Speler toevoegen", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (answer == MessageBoxResult.No)
                {
                    break;
                }
                currentPlayer = playerNames[0];
                nextPlayer = playerNames.Count > 1 ? playerNames[1] : "Geen volgende speler";
                StartCountDown();
            }
        }
        private void ResetGame()
        {
            score = 100;
            attempts = 0;
            remainingAttempts = 10;

            attemptsPanel.Children.Clear();
            scoreLabel.Content = 100;

            GenerateTargetColors();
            
            this.Title = $"Speler: {currentPlayer}'s beurt";       
                
            label1.Background = Brushes.White;
            label2.Background = Brushes.White;
            label3.Background = Brushes.White;
            label4.Background = Brushes.White;
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show($"Wilt u het spel vroegtijdig beëindigen?", $"Poging {attempts}/10", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (answer == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
        private void newGameMenu_Click(object sender, RoutedEventArgs e)
        {
            ResetGame();
            StartGame();
        }
        private void highScoreMenu_Click(object sender, RoutedEventArgs e)
        {
            string highscoreEntry = string.Join("\n", highScores.Select((entry, index) => $"{entry.playerName} - {entry.attempts} pogingen - {entry.score}/100"));

            MessageBox.Show(string.IsNullOrEmpty(highscoreEntry) ? "Nog geen highscores!" : highscoreEntry,
            "Mastermind highscores", MessageBoxButton.OK, MessageBoxImage.Information);
            NextPlayer();
        }
        private void closeGameMenu_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void amountOfAttemptsMenu_Click(object sender, RoutedEventArgs e)
        {
            string answer = Interaction.InputBox("Hoeveel pogingen wilt u?", "Pogingen kiezen", " ", 20);
            bool result = Int32.TryParse(answer, out remainingAttempts);

            if (!result || remainingAttempts < 3 || remainingAttempts > 20)
            {
                MessageBox.Show("Voer een geldig aantal pogingen in tussen 3 en 20.", "Foutieve invoer", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            }
        }
    }


}
