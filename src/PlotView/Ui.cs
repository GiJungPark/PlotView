using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace PlotView
{
    /// <summary>%AppData%\PlotView\settings.ini 에 저장되는 설정.</summary>
    internal sealed class AppSettings
    {
        public bool InvertWheel;
        public bool LeftDragPan = true;
        public bool Crosshair;
        public double WheelSpeed = 1.0;
        public string LastDir = "";
        public Rectangle WindowBounds = Rectangle.Empty;
        public bool Maximized;

        private static string FilePath
        {
            get
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlotView");
                return Path.Combine(dir, "settings.ini");
            }
        }

        public static AppSettings Load()
        {
            var s = new AppSettings();
            try
            {
                if (!File.Exists(FilePath)) return s;
                var d = new Dictionary<string, string>();
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) d[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
                string v;
                if (d.TryGetValue("InvertWheel", out v)) s.InvertWheel = v == "1";
                if (d.TryGetValue("LeftDragPan", out v)) s.LeftDragPan = v == "1";
                if (d.TryGetValue("Crosshair", out v)) s.Crosshair = v == "1";
                double sp;
                if (d.TryGetValue("WheelSpeed", out v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out sp))
                    s.WheelSpeed = Math.Max(0.4, Math.Min(2.5, sp));
                if (d.TryGetValue("LastDir", out v)) s.LastDir = v;
                if (d.TryGetValue("Maximized", out v)) s.Maximized = v == "1";
                if (d.TryGetValue("Bounds", out v))
                {
                    string[] p = v.Split(',');
                    int x, y, w, h;
                    if (p.Length == 4 && int.TryParse(p[0], out x) && int.TryParse(p[1], out y) &&
                        int.TryParse(p[2], out w) && int.TryParse(p[3], out h))
                        s.WindowBounds = new Rectangle(x, y, w, h);
                }
            }
            catch { }
            return s;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var b = WindowBounds;
                File.WriteAllLines(FilePath, new[]
                {
                    "InvertWheel=" + (InvertWheel ? "1" : "0"),
                    "LeftDragPan=" + (LeftDragPan ? "1" : "0"),
                    "Crosshair=" + (Crosshair ? "1" : "0"),
                    "WheelSpeed=" + WheelSpeed.ToString("0.0", CultureInfo.InvariantCulture),
                    "LastDir=" + LastDir,
                    "Maximized=" + (Maximized ? "1" : "0"),
                    "Bounds=" + b.X + "," + b.Y + "," + b.Width + "," + b.Height,
                });
            }
            catch { }
        }
    }

    internal static class Theme
    {
        public static readonly Color Chrome = Color.FromArgb(0x26, 0x2e, 0x38);
        public static readonly Color Chrome2 = Color.FromArgb(0x31, 0x3b, 0x47);
        public static readonly Color Line = Color.FromArgb(0x3b, 0x47, 0x57);
        public static readonly Color Text = Color.FromArgb(0xd8, 0xde, 0xe6);
        public static readonly Color Muted = Color.FromArgb(0x8e, 0x9b, 0xab);
        public static readonly Color Accent = Color.FromArgb(0x5a, 0xa9, 0xe6);

        public static Font UiFont = new Font("Malgun Gothic", 9f);

        public static ToolStripRenderer Renderer()
        {
            return new ToolStripProfessionalRenderer(new DarkColors()) { RoundedEdges = false };
        }

        public static void Apply(Control c)
        {
            c.BackColor = Chrome;
            c.ForeColor = Text;
            c.Font = UiFont;
        }

        private sealed class DarkColors : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin { get { return Chrome; } }
            public override Color ToolStripGradientMiddle { get { return Chrome; } }
            public override Color ToolStripGradientEnd { get { return Chrome; } }
            public override Color ToolStripBorder { get { return Chrome; } }
            public override Color StatusStripGradientBegin { get { return Chrome; } }
            public override Color StatusStripGradientEnd { get { return Chrome; } }
            public override Color ButtonSelectedHighlight { get { return Chrome2; } }
            public override Color ButtonSelectedGradientBegin { get { return Chrome2; } }
            public override Color ButtonSelectedGradientMiddle { get { return Chrome2; } }
            public override Color ButtonSelectedGradientEnd { get { return Chrome2; } }
            public override Color ButtonSelectedBorder { get { return Line; } }
            public override Color ButtonPressedGradientBegin { get { return Line; } }
            public override Color ButtonPressedGradientMiddle { get { return Line; } }
            public override Color ButtonPressedGradientEnd { get { return Line; } }
            public override Color ButtonPressedBorder { get { return Line; } }
            public override Color SeparatorDark { get { return Line; } }
            public override Color SeparatorLight { get { return Chrome; } }
            public override Color GripDark { get { return Chrome; } }
            public override Color GripLight { get { return Chrome; } }
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly CheckBox _invert, _left, _cross;
        private readonly TrackBar _speed;
        private readonly Label _speedLbl;

        public SettingsForm(AppSettings cfg)
        {
            Text = "조작 설정";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Theme.Apply(this);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);

            var col = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            _invert = new CheckBox { Text = "휠 방향 반전 (앞으로 굴리면 축소)", AutoSize = true, Checked = cfg.InvertWheel };
            _left = new CheckBox { Text = "왼쪽 버튼 드래그로 화면 이동", AutoSize = true, Checked = cfg.LeftDragPan };
            _cross = new CheckBox { Text = "CAD 십자선 커서", AutoSize = true, Checked = cfg.Crosshair };
            col.Controls.Add(_invert);
            col.Controls.Add(_left);
            col.Controls.Add(_cross);

            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 10, 0, 0) };
            row.Controls.Add(new Label { Text = "휠 확대 속도", AutoSize = true, Margin = new Padding(3, 8, 3, 0) });
            _speed = new TrackBar { Minimum = 4, Maximum = 25, TickFrequency = 3, Width = 180, Value = (int)Math.Round(cfg.WheelSpeed * 10) };
            _speedLbl = new Label { AutoSize = true, Margin = new Padding(3, 8, 3, 0) };
            _speed.ValueChanged += (s, e) => UpdateLbl();
            UpdateLbl();
            row.Controls.Add(_speed);
            row.Controls.Add(_speedLbl);
            col.Controls.Add(row);

            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 10, 0, 0) };
            var ok = new Button { Text = "확인", DialogResult = DialogResult.OK, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Theme.Chrome2 };
            var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = Theme.Chrome2 };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            col.Controls.Add(buttons);
            Controls.Add(col);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void UpdateLbl()
        {
            _speedLbl.Text = (_speed.Value / 10.0).ToString("0.0", CultureInfo.InvariantCulture) + "×";
        }

        public void ApplyTo(AppSettings cfg)
        {
            cfg.InvertWheel = _invert.Checked;
            cfg.LeftDragPan = _left.Checked;
            cfg.Crosshair = _cross.Checked;
            cfg.WheelSpeed = _speed.Value / 10.0;
        }
    }

    internal sealed class PasswordForm : Form
    {
        private readonly TextBox _box;

        private PasswordForm(bool retry)
        {
            Text = "비밀번호";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            Theme.Apply(this);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(14);

            var col = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false };
            col.Controls.Add(new Label
            {
                Text = retry ? "비밀번호가 틀렸습니다. 다시 입력하세요." : "이 PDF는 비밀번호로 보호되어 있습니다.",
                AutoSize = true
            });
            _box = new TextBox { UseSystemPasswordChar = true, Width = 260, BackColor = Theme.Chrome2, ForeColor = Theme.Text };
            col.Controls.Add(_box);
            var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
            var ok = new Button { Text = "열기", DialogResult = DialogResult.OK, AutoSize = true, FlatStyle = FlatStyle.Flat };
            var cancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, AutoSize = true, FlatStyle = FlatStyle.Flat };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            col.Controls.Add(buttons);
            Controls.Add(col);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        public static string Ask(IWin32Window owner, bool retry)
        {
            using (var f = new PasswordForm(retry))
                return f.ShowDialog(owner) == DialogResult.OK ? f._box.Text : null;
        }
    }
}
