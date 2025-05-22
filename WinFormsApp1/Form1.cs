using Timer = System.Windows.Forms.Timer;
using NAudio.Wave;

namespace VirtualPiano
{
    public partial class PianoForm : Form
    {
        private readonly string projectDirectory;
        private readonly string logFilePath;
        private readonly string soundsPath;
        private readonly Color[] rainbowColors = new Color[]
        {
            Color.Red, Color.Orange, Color.Yellow, Color.Green,
            Color.Blue, Color.Indigo, Color.Violet
        };
        private DateTime lastLogUpdate = DateTime.Now;
        private System.Timers.Timer logTimer;

        public PianoForm()
        {
            InitializeComponent();

            projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            logFilePath = Path.Combine(projectDirectory, "log.txt");
            soundsPath = Path.Combine(projectDirectory, "sounds");

            InitializePianoUI();
            InitializeLogger();
            LogEvent("Program started.");
        }

       private void InitializePianoUI()
{
    this.Text = "Virtual Piano";
    this.Width = 1300;
    this.Height = 900;
    this.KeyPreview = true;
    this.BackColor = Color.Black;
    this.StartPosition = FormStartPosition.CenterScreen;

    Panel pianoPanel = new Panel
    {
        Location = new Point(100, 50),
        Size = new Size(7 * 120, 400),
        BackColor = Color.Black
    };
    this.Controls.Add(pianoPanel);

    // Додаємо білі клавіші
    for (int i = 0; i < 7; i++)
    {
        Button keyButton = new Button
        {
            Name = $"key{i + 1}",
            Width = 100,
            Height = 350,
            Location = new Point(i * 120, 20),
            BackColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Consolas", 14F, FontStyle.Bold),
            Tag = i + 1,
            Text = ""
        };
        keyButton.FlatAppearance.BorderColor = Color.Black;
        keyButton.FlatAppearance.BorderSize = 3;
        keyButton.Click += KeyButton_Click;
        pianoPanel.Controls.Add(keyButton);
    }


    int[] blackKeyOffsets = { 1, 2, 4, 5, 6 }; // між якими білими стоять чорні
    foreach (int i in blackKeyOffsets)
    {
        Button blackKey = new Button
        {
            Width = 70,
            Height = 220,
            Location = new Point((i * 120) - 45, 0), // -45 центрує між білими, 0 — вище
            BackColor = Color.Black,
            FlatStyle = FlatStyle.Flat,
            Tag = $"b{i}",
            Text = "",
            Parent = pianoPanel
        };
        blackKey.FlatAppearance.BorderColor = Color.Black;
        blackKey.FlatAppearance.BorderSize = 1;

        blackKey.Click += (s, e) =>
        {
            MessageBox.Show($"Black key {blackKey.Tag} pressed (sound not assigned).");
        };

        pianoPanel.Controls.Add(blackKey);
        blackKey.BringToFront(); // щоб вони були поверх білих
    }


    // ASCII-style заголовок "PIANO"
    Label asciiLabel = new Label
    {
        Font = new Font("Consolas", 14F, FontStyle.Bold),
        ForeColor = Color.White,
        BackColor = Color.Black,
        Location = new Point(100, 500),
        AutoSize = true,
        Text = @"
 ____ ___     _    _   _  _ __
 |  _ \_ _|  / \  | \ | |  _  |
 | |_) | |  / _ \ |  \| | | | |
 |  __/| | / ___ \| |\  | |_| |
 |_|  |___|_/   \_\_| \_| ___ |"
    };
    this.Controls.Add(asciiLabel);

    // Goodbye
    Label goodbyeLabel = new Label
    {
        Text = "Goodbye !",
        Font = new Font("Consolas", 24F, FontStyle.Regular),
        ForeColor = Color.Magenta,
        BackColor = Color.Black,
        Location = new Point(120, 580),
        AutoSize = true
    };
    this.Controls.Add(goodbyeLabel);

    // Інструкції
    Label instructionsLabel = new Label
    {
        Text = "Notes are played by pressing keys:\n1 , 2 , 3 , 4 , 5 , 6 , 7",
        Font = new Font("Consolas", 20F),
        ForeColor = Color.White,
        BackColor = Color.Black,
        Location = new Point(800, 500),
        AutoSize = true
    };
    this.Controls.Add(instructionsLabel);

    // Вихід
    Label exitLabel = new Label
    {
        Text = "To exit, press \"b\"",
        Font = new Font("Consolas", 20F),
        ForeColor = Color.Yellow,
        BackColor = Color.Black,
        Location = new Point(800, 600),
        AutoSize = true
    };
    this.Controls.Add(exitLabel);

    this.KeyDown += PianoForm_KeyDown;
}



        private void InitializeLogger()
        {
            if (!File.Exists(logFilePath))
            {
                File.Create(logFilePath).Close();
            }

            logTimer = new System.Timers.Timer(15000);
            logTimer.Elapsed += (s, e) => { };
            logTimer.Start();
        }

        private void KeyButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is int noteNumber)
            {
                PlayNote(noteNumber);
                HighlightKey(btn, noteNumber);
                LogEvent($"Note {noteNumber} played.");
            }
        }

        private void PianoForm_KeyDown(object sender, KeyEventArgs e)
        {
             if (e.Control && e.KeyCode == Keys.F)
            {
                LogEvent("Program exited by user.");
                Application.Exit();
            }
            else
            {
                int noteNumber = e.KeyValue - (int)'0';
                if (noteNumber >= 1 && noteNumber <= 7)
                {
                    PlayNote(noteNumber);
                    var btn = FindButtonByTag(noteNumber);
                    if (btn != null)
                        HighlightKey(btn, noteNumber);

                    LogEvent($"Note {noteNumber} played by keyboard.");
                }
                else
                {
                    MessageBox.Show("Invalid input! Please press a number between 1 and 7.", "Input Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogEvent($"Invalid input: {e.KeyCode}");
                }
            }
        }

        private Button FindButtonByTag(int noteNumber)
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is Panel panel)
                {
                    foreach (Control btn in panel.Controls)
                    {
                        if (btn is Button b && (int)b.Tag == noteNumber)
                            return b;
                    }
                }
            }
            return null;
        }

        private void PlayNote(int noteNumber)
        {
            string filePath = Path.Combine(soundsPath, $"note{noteNumber}.wav");
            if (File.Exists(filePath))
            {
                try
                {
                    var audioFile = new AudioFileReader(filePath);
                    var outputDevice = new WaveOutEvent();
                    outputDevice.Init(audioFile);
                    outputDevice.Play();

                    outputDevice.PlaybackStopped += (s, e) =>
                    {
                        outputDevice.Dispose();
                        audioFile.Dispose();
                    };
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing sound: {ex.Message}", "Sound Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogEvent($"Sound error: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show($"Sound file not found: {filePath}", "File Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogEvent($"Sound file missing: {filePath}");
            }
        }

        private void HighlightKey(Button btn, int noteNumber)
        {
            btn.BackColor = rainbowColors[(noteNumber - 1) % rainbowColors.Length];
            Timer t = new Timer();
            t.Interval = 500;
            t.Tick += (s, e) =>
            {
                btn.BackColor = Color.White;
                (s as Timer)?.Stop();
                (s as Timer)?.Dispose();
            };
            t.Start();
        }

        private void LogEvent(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string logMessage = $"{timestamp} - Event - {message}";
            try
            {
                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing to log file: {ex.Message}", "Log Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            lastLogUpdate = DateTime.Now;
        }
    }
}
