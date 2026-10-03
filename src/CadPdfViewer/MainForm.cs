using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CadPdfViewer
{
    internal sealed class MainForm : Form
    {
        private const string AppName = "CAD식 PDF 뷰어";

        private static readonly string[][] Papers =
        {
            new[] { "A0", "841", "1189" }, new[] { "A1", "594", "841" }, new[] { "A2", "420", "594" },
            new[] { "A3", "297", "420" }, new[] { "A4", "210", "297" }, new[] { "A5", "148", "210" },
            new[] { "B3", "364", "515" }, new[] { "B4", "257", "364" }, new[] { "B5", "182", "257" },
            new[] { "Letter", "216", "279" }, new[] { "Legal", "216", "356" }, new[] { "Tabloid", "279", "432" },
            new[] { "ARCH D", "610", "914" }, new[] { "ARCH E", "914", "1219" },
        };

        private readonly AppSettings _cfg;
        private readonly PdfView _view;
        private PdfDocument _doc;
        private int _index;
        private int _userRot;

        private readonly ToolStripButton _open, _prev, _next, _fit, _rotate, _settings;
        private readonly ToolStripLabel _fileName, _pageCount, _zoom;
        private readonly ToolStripTextBox _pageBox;
        private readonly ToolStripStatusLabel _coord, _paper, _busy;
        private readonly Label _empty;

        public MainForm(string startFile)
        {
            _cfg = AppSettings.Load();
            Text = AppName;
            AutoScaleMode = AutoScaleMode.Dpi;
            Theme.Apply(this);
            KeyPreview = true;
            AllowDrop = true;
            MinimumSize = new Size(480, 320);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            if (_cfg.WindowBounds.Width > 200 && Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(_cfg.WindowBounds)))
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = _cfg.WindowBounds;
            }
            else
            {
                Size = new Size(1280, 820);
                StartPosition = FormStartPosition.CenterScreen;
            }
            if (_cfg.Maximized) WindowState = FormWindowState.Maximized;

            // ---- 화면
            _view = new PdfView { Dock = DockStyle.Fill, Cfg = _cfg, AllowDrop = true };
            _view.StateChanged += (s, e) => UpdateStatus();
            _view.BusyChanged += (s, e) => _busy.Text = _view.IsBusy ? "렌더링 중…" : "";
            _view.DragEnter += OnDragEnter;
            _view.DragDrop += OnDragDrop;

            _empty = new Label
            {
                Text = "도면 PDF를 이 창에 끌어다 놓거나 [열기]를 누르세요.\n\n" +
                       "마우스 휠   커서 위치 기준 확대/축소\n" +
                       "가운데 버튼 드래그   화면 이동\n" +
                       "가운데 버튼 더블클릭, Home   전체 보기\n" +
                       "PageUp / PageDown   이전 / 다음 페이지\n" +
                       "R   90° 회전",
                ForeColor = Theme.Muted,
                BackColor = PdfView.SpaceColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AllowDrop = true,
                Font = new Font(Theme.UiFont.FontFamily, 10.5f),
            };
            _empty.DragEnter += OnDragEnter;
            _empty.DragDrop += OnDragDrop;
            _empty.DoubleClick += (s, e) => OpenDialog();

            // ---- 도구 모음
            var tool = new ToolStrip
            {
                GripStyle = ToolStripGripStyle.Hidden,
                Renderer = Theme.Renderer(),
                Padding = new Padding(6, 3, 6, 3),
                ForeColor = Theme.Text,
                Font = Theme.UiFont,
                CanOverflow = true,
            };
            _open = Btn("열기", "PDF 열기 (Ctrl+O)", (s, e) => OpenDialog());
            _open.ForeColor = Theme.Accent;
            _fileName = new ToolStripLabel { ForeColor = Theme.Muted, Margin = new Padding(8, 0, 8, 0) };
            _prev = Btn("◀ 이전", "이전 페이지 (PageUp)", (s, e) => GoTo(_index - 1));
            _pageBox = new ToolStripTextBox { AutoSize = false, Width = 48, TextBoxTextAlign = HorizontalAlignment.Center, BorderStyle = BorderStyle.FixedSingle };
            _pageBox.BackColor = Theme.Chrome2;
            _pageBox.ForeColor = Theme.Text;
            _pageBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                int n;
                if (int.TryParse(_pageBox.Text, out n)) GoTo(n - 1);
                e.SuppressKeyPress = true;
                _view.Focus();
            };
            _pageCount = new ToolStripLabel("/ 0") { ForeColor = Theme.Muted };
            _next = Btn("다음 ▶", "다음 페이지 (PageDown)", (s, e) => GoTo(_index + 1));
            _fit = Btn("전체 보기", "전체 보기 (Home, F, 가운데 버튼 더블클릭)", (s, e) => _view.ZoomExtents());
            _rotate = Btn("회전", "90° 회전 (R)", (s, e) => Rotate());
            _settings = Btn("설정", "조작 설정", (s, e) => OpenSettings());
            _settings.Alignment = ToolStripItemAlignment.Right;
            _zoom = new ToolStripLabel("—") { Alignment = ToolStripItemAlignment.Right, ForeColor = Theme.Muted, Margin = new Padding(8, 0, 8, 0) };

            tool.Items.AddRange(new ToolStripItem[]
            {
                _open, _fileName, new ToolStripSeparator(), _prev, _pageBox, _pageCount, _next,
                new ToolStripSeparator(), _fit, _rotate, _settings, _zoom
            });

            // ---- 상태 표시줄
            var status = new StatusStrip { Renderer = Theme.Renderer(), SizingGrip = true, ForeColor = Theme.Muted, Font = Theme.UiFont };
            _coord = new ToolStripStatusLabel("X —   Y —") { ForeColor = Theme.Text, AutoSize = false, Width = 200, TextAlign = ContentAlignment.MiddleLeft };
            _paper = new ToolStripStatusLabel("") { ForeColor = Theme.Muted };
            _busy = new ToolStripStatusLabel("") { ForeColor = Theme.Accent };
            var spring = new ToolStripStatusLabel("") { Spring = true };
            var hint = new ToolStripStatusLabel("휠: 확대/축소    가운데 드래그: 이동    가운데 더블클릭: 전체 보기") { ForeColor = Theme.Muted };
            status.Items.AddRange(new ToolStripItem[] { _coord, _paper, _busy, spring, hint });

            Controls.Add(_view);
            Controls.Add(_empty);
            Controls.Add(tool);
            Controls.Add(status);
            _empty.BringToFront();

            DragEnter += OnDragEnter;
            DragDrop += OnDragDrop;
            KeyUp += (s, e) => { if (e.KeyCode == Keys.Space) _view.SpaceDown = false; };

            SetEnabled(false);
            if (!string.IsNullOrEmpty(startFile))
                Shown += (s, e) => OpenPath(startFile);
        }

        private static ToolStripButton Btn(string text, string tip, EventHandler click)
        {
            var b = new ToolStripButton(text) { ToolTipText = tip, DisplayStyle = ToolStripItemDisplayStyle.Text, Padding = new Padding(6, 2, 6, 2) };
            b.Click += click;
            return b;
        }

        private void SetEnabled(bool on)
        {
            _prev.Enabled = _next.Enabled = _fit.Enabled = _rotate.Enabled = _pageBox.Enabled = on;
        }

        // ------------------------------------------------------------------ 파일
        private void OpenDialog()
        {
            using (var d = new OpenFileDialog { Filter = "PDF 파일 (*.pdf)|*.pdf|모든 파일 (*.*)|*.*", Title = "PDF 열기" })
            {
                if (!string.IsNullOrEmpty(_cfg.LastDir) && Directory.Exists(_cfg.LastDir)) d.InitialDirectory = _cfg.LastDir;
                if (d.ShowDialog(this) == DialogResult.OK) OpenPath(d.FileName);
            }
        }

        public void OpenPath(string path)
        {
            PdfDocument doc;
            try
            {
                doc = PdfDocument.Open(path, retry => PasswordForm.Ask(this, retry));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "PDF를 열지 못했습니다.\n\n" + ex.Message, AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (doc == null) return;
            if (doc.PageCount < 1)
            {
                doc.Dispose();
                MessageBox.Show(this, "페이지가 없는 PDF입니다.", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _view.SetPage(null, 0, 0);
            if (_doc != null) _doc.Dispose();
            _doc = doc;
            _userRot = 0;
            _cfg.LastDir = Path.GetDirectoryName(path);
            _fileName.Text = Path.GetFileName(path);
            Text = Path.GetFileName(path) + " — " + AppName;
            _pageCount.Text = "/ " + doc.PageCount;
            _empty.Visible = false;
            SetEnabled(true);
            GoTo(0);
            _view.Focus();
        }

        private void GoTo(int i)
        {
            if (_doc == null) return;
            i = Math.Max(0, Math.Min(_doc.PageCount - 1, i));
            _index = i;
            _view.SetPage(_doc, i, _userRot);
            _pageBox.Text = (i + 1).ToString();
            _prev.Enabled = i > 0;
            _next.Enabled = i < _doc.PageCount - 1;
            UpdatePaper();
            UpdateStatus();
        }

        private void Rotate()
        {
            if (_doc == null) return;
            _userRot = (_userRot + 1) % 4;
            GoTo(_index);
        }

        private void OpenSettings()
        {
            using (var f = new SettingsForm(_cfg))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;
                f.ApplyTo(_cfg);
                _cfg.Save();
                _view.UpdateCursor();
                _view.Invalidate();
            }
        }

        // ------------------------------------------------------------------ 상태 표시
        private void UpdateStatus()
        {
            _zoom.Text = _view.HasPage ? Math.Round(_view.ZoomPercent).ToString("#,0") + "%" : "—";
            PointF? p = _view.CursorMm;
            _coord.Text = p.HasValue ? string.Format("X {0:0.0}   Y {1:0.0} mm", p.Value.X, p.Value.Y) : "X —   Y —";
        }

        private void UpdatePaper()
        {
            if (!_view.HasPage) { _paper.Text = ""; return; }
            double a = _view.PageWidthPt * 25.4 / 72, b = _view.PageHeightPt * 25.4 / 72;
            double lo = Math.Min(a, b), hi = Math.Max(a, b);
            string name = null;
            foreach (var p in Papers)
                if (Math.Abs(lo - double.Parse(p[1])) < 3 && Math.Abs(hi - double.Parse(p[2])) < 3) { name = p[0]; break; }
            _paper.Text = string.Format("용지 {0:0} × {1:0} mm", a, b) + (name != null ? " (" + name + ")" : "");
        }

        // ------------------------------------------------------------------ 키보드
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.O)) { OpenDialog(); return true; }
            if (_pageBox.Focused) return base.ProcessCmdKey(ref msg, keyData);
            switch (keyData)
            {
                case Keys.Space: _view.SpaceDown = true; return true;
                case Keys.PageUp: GoTo(_index - 1); return true;
                case Keys.PageDown: GoTo(_index + 1); return true;
                case Keys.Home:
                case Keys.F: _view.ZoomExtents(); return true;
                case Keys.R: Rotate(); return true;
                case Keys.Oemplus:
                case Keys.Add: _view.ZoomAtCursorOrCenter(1.25); return true;
                case Keys.OemMinus:
                case Keys.Subtract: _view.ZoomAtCursorOrCenter(0.8); return true;
                case Keys.Left: _view.PanBy(60, 0); return true;
                case Keys.Right: _view.PanBy(-60, 0); return true;
                case Keys.Up: _view.PanBy(0, 60); return true;
                case Keys.Down: _view.PanBy(0, -60); return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ------------------------------------------------------------------ 끌어다 놓기 / 종료
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null) return;
            string pdf = files.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
            if (pdf != null) BeginInvoke(new Action(() => OpenPath(pdf)));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cfg.Maximized = WindowState == FormWindowState.Maximized;
            _cfg.WindowBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            _cfg.Save();
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _view.SetPage(null, 0, 0);
            if (_doc != null) _doc.Dispose();
            _doc = null;
            base.OnFormClosed(e);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
#if NET5_0_OR_GREATER
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Pdfium.FPDF_InitLibrary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("PDF 엔진(pdfium.dll)을 불러오지 못했습니다. 다시 설치해 주세요.\n\n" + ex.Message,
                    "CAD식 PDF 뷰어", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string file = args.FirstOrDefault(a => a.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && File.Exists(a));
            Application.Run(new MainForm(file));
        }
    }
}
