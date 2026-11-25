using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;

[assembly: AssemblyTitle("Sip & Stretch")]
[assembly: AssemblyProduct("Sip & Stretch")]
[assembly: AssemblyDescription("Вода и разминка во время работы")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace SipStretch
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool first;
            using (var mutex = new Mutex(true, @"Local\SipStretch.Desktop.1", out first))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (!first)
                {
                    MessageBox.Show("Приложение уже работает. Откройте его через значок в области уведомлений.", "Sip & Stretch");
                    return;
                }
                using (var main = new Dashboard()) Application.Run(main);
            }
        }
    }

    internal static class Style
    {
        internal static readonly Color Paper = Color.FromArgb(245, 245, 236);
        internal static readonly Color Ink = Color.FromArgb(21, 53, 49);
        internal static readonly Color Muted = Color.FromArgb(74, 98, 89);
        internal static readonly Color Sea = Color.FromArgb(14, 65, 59);
        internal static readonly Color Mint = Color.FromArgb(168, 242, 198);
        internal static readonly Color Peach = Color.FromArgb(255, 190, 138);
        internal static Font Face(float size, bool bold)
        {
            return new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
        }
        internal static Label Label(Control parent, string value, int x, int y, int w, int h, float size, bool bold, Color color)
        {
            var label = new Label { Text = value, Bounds = new Rectangle(x,y,w,h), Font = Face(size,bold),
                ForeColor = color, BackColor = Color.Transparent, UseMnemonic = false, TextAlign = ContentAlignment.MiddleLeft };
            parent.Controls.Add(label);
            return label;
        }
        internal static Button Button(Control parent, string value, int x, int y, int w, int h, Color fill, EventHandler action)
        {
            var b = new Button { Text = value, Bounds = new Rectangle(x,y,w,h), FlatStyle = FlatStyle.Flat,
                BackColor = fill, ForeColor = Ink, Font = Face(9.5f,true), Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false };
            b.FlatAppearance.BorderSize = 0;
            b.Click += action;
            parent.Controls.Add(b);
            return b;
        }
        internal static void Form(Form f, string title, int w, int h)
        {
            f.AutoScaleDimensions = new SizeF(96,96);
            f.AutoScaleMode = AutoScaleMode.Dpi;
            f.Text = title;
            f.ClientSize = new Size(w,h);
            f.BackColor = Paper;
            f.FormBorderStyle = FormBorderStyle.FixedSingle;
            f.MaximizeBox = false;
            f.StartPosition = FormStartPosition.CenterScreen;
            f.Font = Face(10,false);
        }
    }

    // All moving elements share one clock. Hidden windows stop requesting frames.
    internal sealed class Scene : Control
    {
        private readonly System.Windows.Forms.Timer frames = new System.Windows.Forms.Timer();
        private readonly Stopwatch motion = Stopwatch.StartNew();
        internal bool Water = true;
        internal Scene()
        {
            DoubleBuffered = true;
            BackColor = Style.Sea;
            frames.Interval = 33;
            frames.Tick += delegate { Invalidate(); };
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (frames != null) frames.Enabled = Visible && IsHandleCreated;
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            frames.Enabled = Visible;
        }
        internal void SetActive(bool active)
        {
            frames.Enabled = active && IsHandleCreated;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Art.Draw(e.Graphics, ClientRectangle, Water, (motion.Elapsed.TotalSeconds % 3.2) / 3.2);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) frames.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class Dashboard : Form
    {
        private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"SipStretch.ini");
        private Preferences prefs;
        private readonly ReminderClock clock;
        private readonly NotifyIcon tray;
        private readonly ContextMenuStrip menu = new ContextMenuStrip();
        private readonly System.Windows.Forms.Timer tick = new System.Windows.Forms.Timer();
        private readonly Label lead, countdown, waterTime, moveTime;
        private readonly Scene scene;
        private readonly Button pause;
        private readonly ToolStripMenuItem trayPause;
        private BreakWindow reminder;
        private bool paused, exiting, settingsOpen;

        internal Dashboard()
        {
            Style.Form(this,"Sip & Stretch",564,512);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            prefs = Preferences.Load(configPath);
            clock = new ReminderClock(prefs, DateTime.UtcNow);

            Style.Label(this,"Sip & Stretch",24,10,330,52,23,true,Style.Ink);
            Style.Button(this,"Настройки",418,19,122,36,Color.White,OpenSettings);
            var hero = new Panel { Bounds = new Rectangle(24,72,516,168), BackColor = Style.Sea };
            Controls.Add(hero);
            lead = Style.Label(hero,"",20,16,281,42,13,true,Style.Mint);
            countdown = Style.Label(hero,"",17,54,285,85,37,true,Style.Paper);
            scene = new Scene { Bounds = new Rectangle(303,2,206,164) };
            hero.Controls.Add(scene);
            VisibleChanged += delegate { scene.SetActive(Visible); };

            Style.Label(this,"Напоминания",24,253,320,42,16,true,Style.Ink);
            waterTime = Row(true,302);
            moveTime = Row(false,369);
            pause = Style.Button(this,"Пауза",24,457,179,37,Color.White,TogglePause);
            Style.Button(this,"Свернуть в трей",351,457,189,37,Style.Mint,delegate { Hide(); });

            menu.Items.Add("Открыть Sip & Stretch",null,delegate { OpenDashboard(); });
            trayPause = (ToolStripMenuItem)menu.Items.Add("Пауза",null,TogglePause);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Выход",null,delegate { exiting = true; Close(); });
            tray = new NotifyIcon { Icon = Icon, Text = "Sip & Stretch", ContextMenuStrip = menu, Visible = true };
            tray.DoubleClick += delegate { OpenDashboard(); };
            Resize += delegate { if (WindowState == FormWindowState.Minimized) Hide(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) {
                if (!exiting && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true; Hide();
                }
            };
            tick.Interval = 500;
            tick.Tick += delegate { UpdateState(); };
            UpdateState();
            tick.Start();
        }

        private Label Row(bool water, int y)
        {
            var row = new Panel { Bounds = new Rectangle(24,y,516,59), BackColor = Color.White };
            Controls.Add(row);
            row.Controls.Add(new Panel { Bounds = new Rectangle(0,0,6,59), BackColor = water ? Style.Mint : Style.Peach });
            Style.Label(row,water ? "Вода" : "Разминка",16,3,225,28,12,true,Style.Ink);
            var label = Style.Label(row,"",16,29,338,23,9,false,Style.Muted);
            var b = Style.Button(row,"Начать сейчас",362,10,143,39,
                water ? Style.Mint : Style.Peach,delegate { ShowBreak(water, true); });
            b.AccessibleName = water ? "Начать перерыв на воду" : "Начать разминку";
            return label;
        }
        private string Describe(bool water)
        {
            var next = clock.Next(water);
            if (!next.HasValue) return "Выключено";
            int minutes = water ? prefs.WaterMinutes : prefs.MoveMinutes;
            return minutes + " мин   ·   " + (paused ? "на паузе" : "в " + next.Value.ToLocalTime().ToString("HH:mm"));
        }
        private void UpdateState()
        {
            var now = DateTime.UtcNow;
            var water = clock.Next(true);
            var move = clock.Next(false);
            bool firstWater = !move.HasValue || (water.HasValue && water.Value <= move.Value);
            var next = firstWater ? water : move;
            scene.Water = firstWater;
            lead.Text = paused ? "Таймеры на паузе" : !next.HasValue ? "Таймеры выключены" :
                (firstWater ? "До перерыва на воду" : "До разминки");
            long seconds = next.HasValue ? Math.Max(0L,(long)Math.Ceiling((next.Value-now).TotalSeconds)) : 0L;
            countdown.Text = paused || !next.HasValue ? "—" : string.Format("{0:00}:{1:00}",seconds/60,seconds%60);
            waterTime.Text = Describe(true);
            moveTime.Text = Describe(false);
            if (!paused && !settingsOpen && reminder == null)
            {
                bool? due = clock.Due(now);
                if (due.HasValue) ShowBreak(due.Value, false);
            }
        }
        private void OpenDashboard()
        {
            Show(); WindowState = FormWindowState.Normal; Activate();
        }
        private void TogglePause(object sender, EventArgs e)
        {
            paused = !paused;
            if (paused && reminder != null) reminder.Close();
            if (!paused) clock.Resume(DateTime.UtcNow);
            pause.Text = trayPause.Text = paused ? "Продолжить" : "Пауза";
            UpdateState();
        }
        private void OpenSettings(object sender, EventArgs e)
        {
            settingsOpen = true;
            try
            {
                using (var form = new Options(prefs,configPath))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        prefs = form.Result;
                        clock.Reset(prefs,DateTime.UtcNow);
                    }
                }
            }
            finally { settingsOpen = false; }
            UpdateState();
        }
        private void ShowBreak(bool water, bool manual)
        {
            if (reminder != null) { reminder.BringToFront(); return; }
            var window = new BreakWindow(water,manual,Icon);
            reminder = window;
            window.FormClosed += delegate {
                if (window.Done) clock.Finish(water,DateTime.UtcNow);
                else if (!manual || window.Deferred) clock.Snooze(water,DateTime.UtcNow);
                reminder = null;
            };
            window.Show();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tick.Dispose();
                if (reminder != null) reminder.Close();
                if (tray != null) { tray.Visible = false; tray.Dispose(); }
                menu.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class BreakWindow : Form
    {
        private readonly bool manual;
        internal bool Done, Deferred;
        internal BreakWindow(bool water, bool startManually, Icon icon)
        {
            manual = startManually;
            Style.Form(this,water ? "Перерыв на воду" : "Короткая разминка",370,342);
            Icon = icon;
            TopMost = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Controls.Add(new Scene { Bounds = new Rectangle(0,0,370,164), Water = water });
            Style.Label(this,water ? "Пора попить воды" : "Время размяться",18,173,338,40,19,true,Style.Ink);
            Style.Label(this,water ? "Отведите взгляд от экрана и сделайте несколько глотков воды." :
                "Встаньте и сделайте несколько комфортных приседаний или пройдитесь по комнате.",
                18,215,336,58,10,false,Style.Muted);
            var ok = Style.Button(this,"Готово",18,285,150,40,water ? Style.Mint : Style.Peach,
                delegate { Done = true; Close(); });
            Style.Button(this,"Через 5 мин",179,285,173,40,Color.White,delegate { Deferred = true; Close(); });
            AcceptButton = ok;
        }
        protected override bool ShowWithoutActivation { get { return !manual; } }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(Math.Max(area.Left,area.Right-Width-16),Math.Max(area.Top,area.Bottom-Height-16));
        }
    }

    internal sealed class Options : Form
    {
        private readonly CheckBox water, move;
        private readonly NumericUpDown waterMin, moveMin;
        private readonly string path;
        internal Preferences Result;
        internal Options(Preferences p,string config)
        {
            Style.Form(this,"Настройки · Sip & Stretch",420,308);
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            path = config;
            Style.Label(this,"Настройки",20,10,375,48,21,true,Style.Ink);
            Style.Label(this,"Интервал между напоминаниями, мин",21,66,390,30,11,false,Style.Muted);
            waterMin = Minutes(p.WaterMinutes,106);
            moveMin = Minutes(p.MoveMinutes,155);
            water = Toggle("Вода",p.WaterEnabled,106,waterMin);
            move = Toggle("Разминка",p.MoveEnabled,155,moveMin);
            Style.Label(this,"Сохранение запускает отсчёт заново.",22,203,390,28,9,false,Style.Muted);
            CancelButton = Style.Button(this,"Отмена",20,249,135,39,Color.White,delegate { Close(); });
            AcceptButton = Style.Button(this,"Сохранить",243,249,157,39,Style.Mint,SaveOptions);
        }
        private NumericUpDown Minutes(int value,int y)
        {
            var n = new NumericUpDown { Bounds = new Rectangle(288,y+4,112,32), Minimum = 1, Maximum = 480,
                Value = value, Font = Style.Face(13,false) };
            n.AccessibleName = y == 106 ? "Интервал для воды в минутах" : "Интервал для разминки в минутах";
            Controls.Add(n); return n;
        }
        private CheckBox Toggle(string label,bool enabled,int y,NumericUpDown n)
        {
            var c = new CheckBox { Bounds = new Rectangle(22,y,245,38), Text = label, Checked = enabled,
                Font = Style.Face(12,true), ForeColor = Style.Ink };
            n.Enabled = enabled;
            c.CheckedChanged += delegate { n.Enabled = c.Checked; };
            Controls.Add(c); return c;
        }
        private void SaveOptions(object sender,EventArgs e)
        {
            var p = new Preferences { WaterEnabled = water.Checked, MoveEnabled = move.Checked,
                WaterMinutes = (int)waterMin.Value, MoveMinutes = (int)moveMin.Value };
            try { p.Save(path); }
            catch (UnauthorizedAccessException) { SaveFailed(); return; }
            catch (IOException) { SaveFailed(); return; }
            Result = p; DialogResult = DialogResult.OK; Close();
        }
        private void SaveFailed()
        {
            MessageBox.Show(this,"Не удалось записать SipStretch.ini рядом с программой. Проверьте, доступна ли папка для записи.",
                Text,MessageBoxButtons.OK,MessageBoxIcon.Warning);
        }
    }
}
