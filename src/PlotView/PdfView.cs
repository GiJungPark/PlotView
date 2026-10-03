using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlotView
{
    /// <summary>
    /// 도면 표시 영역. 뷰 상태는 S(화면 px / pt), Tx, Ty(용지 왼쪽 위 모서리의 화면 위치).
    /// 확대·이동 중에는 미리 렌더링한 이미지를 늘려 그려 즉시 반응하고,
    /// 손을 멈추면 화면에 보이는 부분만 현재 배율로 다시 렌더링해 선명하게 만든다.
    /// </summary>
    internal sealed class PdfView : Control
    {
        public static readonly Color SpaceColor = Color.FromArgb(0x1d, 0x24, 0x2c);
        private static readonly Color CrossColor = Color.FromArgb(0xe9, 0xed, 0xf2);
        private const int BaseMaxSide = 4096;
        private const double BaseMaxArea = 16e6;

        public AppSettings Cfg;
        /// <summary>배율, 위치, 커서 좌표가 바뀌었을 때.</summary>
        public event EventHandler StateChanged;
        /// <summary>렌더링 중 여부가 바뀌었을 때.</summary>
        public event EventHandler BusyChanged;

        private IntPtr _page = IntPtr.Zero;
        private int _rot;                 // 사용자 회전 0~3 (90° 단위, 시계 방향)
        private float _pw, _ph;           // 회전 반영된 용지 크기(pt)
        private double _s = 1, _tx, _ty, _fit = 1;

        private Bitmap _base;             // 용지 전체
        private Bitmap _hi;               // 화면에 보이는 부분만, 현재 배율
        private RectangleF _hiRect;       // _hi가 덮는 용지 영역(pt)
        private double _hiScale;

        private int _pageGen;             // 페이지가 바뀔 때마다 증가 (잠금 안에서만 변경)
        private int _reqSeq, _acceptedSeq;
        private int _busy;

        private readonly System.Windows.Forms.Timer _hiTimer;
        private readonly ImageAttributes _clampAttr = new ImageAttributes();
        private Point? _drag;
        private Point? _mouse;
        private Size _lastSize;
        private Cursor _blankCursor;

        public bool SpaceDown;
        public bool HasPage { get { return _page != IntPtr.Zero; } }
        public float PageWidthPt { get { return _pw; } }
        public float PageHeightPt { get { return _ph; } }
        public bool IsBusy { get { return _busy > 0; } }

        public PdfView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.Opaque, true);
            TabStop = true;
            BackColor = SpaceColor;
            _clampAttr.SetWrapMode(WrapMode.TileFlipXY);   // 가장자리 번짐 방지
            _hiTimer = new System.Windows.Forms.Timer { Interval = 120 };
            _hiTimer.Tick += (s, e) => { _hiTimer.Stop(); RenderHi(); };
            try { using (var b = new Bitmap(32, 32)) _blankCursor = new Cursor(b.GetHicon()); }
            catch { _blankCursor = Cursors.Cross; }
            _lastSize = ClientSize;
            UpdateCursor();
        }

        // ------------------------------------------------------------------ 배율
        private double Dpi
        {
            get
            {
#if NET5_0_OR_GREATER
                return DeviceDpi;
#else
                return 96.0;
#endif
            }
        }

        /// <summary>100% = 실제 크기.</summary>
        public double ZoomPercent { get { return _s / (Dpi / 72.0) * 100.0; } }
        private double MaxS { get { return 60.0 * Dpi / 72.0; } }     // 6000%
        private double MinS { get { return _fit * 0.05; } }

        /// <summary>커서 위치의 용지 좌표(mm, 왼쪽 아래 원점). 없으면 null.</summary>
        public PointF? CursorMm
        {
            get
            {
                if (!HasPage || !_mouse.HasValue) return null;
                double x = (_mouse.Value.X - _tx) / _s;
                double y = _ph - (_mouse.Value.Y - _ty) / _s;
                return new PointF((float)(x * 25.4 / 72), (float)(y * 25.4 / 72));
            }
        }

        // ------------------------------------------------------------------ 페이지
        public void SetPage(PdfDocument doc, int index, int userRot)
        {
            _hiTimer.Stop();
            int gen;
            float w = 0, h = 0;
            lock (Pdfium.Sync)
            {
                _pageGen++;
                gen = _pageGen;
                if (_page != IntPtr.Zero) Pdfium.FPDF_ClosePage(_page);
                _page = IntPtr.Zero;
                if (doc != null && doc.Handle != IntPtr.Zero)
                {
                    _page = Pdfium.FPDF_LoadPage(doc.Handle, index);
                    if (_page != IntPtr.Zero)
                    {
                        w = Pdfium.FPDF_GetPageWidthF(_page);
                        h = Pdfium.FPDF_GetPageHeightF(_page);
                    }
                }
            }
            DisposeBitmaps();
            _rot = userRot & 3;
            if (!HasPage)
            {
                UpdateCursor();
                Invalidate();
                OnStateChanged();
                return;
            }
            bool swap = (_rot & 1) == 1;
            _pw = Math.Max(1f, swap ? h : w);
            _ph = Math.Max(1f, swap ? w : h);
            UpdateCursor();
            ZoomExtents();
            StartBaseRender(gen);
        }

        private void DisposeBitmaps()
        {
            if (_base != null) _base.Dispose();
            if (_hi != null) _hi.Dispose();
            _base = null;
            _hi = null;
        }

        private void StartBaseRender(int gen)
        {
            float w = _pw, h = _ph;
            int rot = _rot;
            SetBusy(+1);
            Task.Run(() =>
            {
                foreach (int side in new[] { 1400, BaseMaxSide })
                {
                    double bs = side / (double)Math.Max(w, h);
                    if (w * h * bs * bs > BaseMaxArea) bs = Math.Sqrt(BaseMaxArea / (w * h));
                    int bw = Math.Max(1, (int)Math.Round(w * bs));
                    int bh = Math.Max(1, (int)Math.Round(h * bs));
                    Bitmap bmp = RenderRegion(gen, rot, bw, bh, 0, 0, bw, bh);
                    if (bmp == null) return;
                    UI(() =>
                    {
                        if (gen != _pageGen) { bmp.Dispose(); return; }
                        if (_base != null) _base.Dispose();
                        _base = bmp;
                        Invalidate();
                    });
                }
            }).ContinueWith(t => UI(() =>
            {
                SetBusy(-1);
                if (gen == _pageGen) ScheduleHi();
            }));
        }

        /// <summary>
        /// 백그라운드 스레드에서 호출. 용지 전체를 sizeX×sizeY 픽셀로 놓았을 때
        /// (startX, startY)만큼 밀린 위치에서 bmpW×bmpH 영역만 그린다.
        /// </summary>
        private Bitmap RenderRegion(int gen, int rot, int bmpW, int bmpH, int startX, int startY, int sizeX, int sizeY)
        {
            Bitmap bmp;
            try { bmp = new Bitmap(bmpW, bmpH, PixelFormat.Format32bppPArgb); }
            catch (ArgumentException) { return null; }   // 메모리 부족 등
            using (var g = Graphics.FromImage(bmp)) g.Clear(Color.White);
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmpW, bmpH), ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
            bool ok = false;
            try
            {
                lock (Pdfium.Sync)
                {
                    if (gen == _pageGen && _page != IntPtr.Zero)
                    {
                        IntPtr pb = Pdfium.FPDFBitmap_CreateEx(bmpW, bmpH, Pdfium.FPDFBitmap_BGRA, data.Scan0, data.Stride);
                        if (pb != IntPtr.Zero)
                        {
                            Pdfium.FPDF_RenderPageBitmap(pb, _page, startX, startY, sizeX, sizeY, rot, Pdfium.FPDF_ANNOT);
                            Pdfium.FPDFBitmap_Destroy(pb);
                            ok = true;
                        }
                    }
                }
            }
            finally { bmp.UnlockBits(data); }
            if (!ok) { bmp.Dispose(); return null; }
            return bmp;
        }

        private void ScheduleHi()
        {
            if (!HasPage) return;
            _hiTimer.Stop();
            _hiTimer.Start();
        }

        private void RenderHi()
        {
            if (!HasPage || _base == null) return;
            double s = _s, tx = _tx, ty = _ty;
            int W = ClientSize.Width, H = ClientSize.Height;
            int x0 = Math.Max(0, (int)Math.Floor(tx));
            int y0 = Math.Max(0, (int)Math.Floor(ty));
            int x1 = Math.Min(W, (int)Math.Ceiling(tx + _pw * s));
            int y1 = Math.Min(H, (int)Math.Ceiling(ty + _ph * s));
            if (x1 <= x0 || y1 <= y0) return;
            int sizeX = (int)Math.Round(_pw * s), sizeY = (int)Math.Round(_ph * s);
            if (sizeX < 1 || sizeY < 1) return;
            int startX = (int)Math.Round(tx) - x0, startY = (int)Math.Round(ty) - y0;
            int bw = x1 - x0, bh = y1 - y0;
            double ex = (double)sizeX / _pw, ey = (double)sizeY / _ph;
            var rect = new RectangleF((float)(-startX / ex), (float)(-startY / ey), (float)(bw / ex), (float)(bh / ey));
            int gen = _pageGen, rot = _rot, seq = ++_reqSeq;

            SetBusy(+1);
            Task.Run(() =>
            {
                if (seq != Volatile.Read(ref _reqSeq)) return (Bitmap)null;  // 이미 더 새 요청이 있음
                return RenderRegion(gen, rot, bw, bh, startX, startY, sizeX, sizeY);
            }).ContinueWith(t => UI(() =>
            {
                SetBusy(-1);
                Bitmap bmp = t.IsFaulted ? null : t.Result;
                if (bmp == null) return;
                if (gen != _pageGen || seq < _acceptedSeq) { bmp.Dispose(); return; }
                _acceptedSeq = seq;
                if (_hi != null) _hi.Dispose();
                _hi = bmp;
                _hiRect = rect;
                _hiScale = s;
                Invalidate();
            }));
        }

        // ------------------------------------------------------------------ 뷰 조작
        private void ViewChanged()
        {
            Invalidate();
            OnStateChanged();
            ScheduleHi();
        }

        public void ZoomExtents()
        {
            if (!HasPage) return;
            int W = Math.Max(1, ClientSize.Width), H = Math.Max(1, ClientSize.Height);
            double m = Math.Min(28.0 * Dpi / 96.0, Math.Min(W, H) * 0.05);
            _fit = Math.Max(1e-4, Math.Min((W - 2 * m) / _pw, (H - 2 * m) / _ph));
            _s = _fit;
            _tx = (W - _pw * _s) / 2;
            _ty = (H - _ph * _s) / 2;
            ViewChanged();
        }

        public void ZoomAt(double px, double py, double f)
        {
            if (!HasPage) return;
            double ns = Math.Min(MaxS, Math.Max(MinS, _s * f));
            f = ns / _s;
            if (Math.Abs(f - 1) < 1e-9) return;
            _tx = px - (px - _tx) * f;
            _ty = py - (py - _ty) * f;
            _s = ns;
            ViewChanged();
        }

        public void ZoomAtCursorOrCenter(double f)
        {
            if (_mouse.HasValue) ZoomAt(_mouse.Value.X, _mouse.Value.Y, f);
            else ZoomAt(ClientSize.Width / 2.0, ClientSize.Height / 2.0, f);
        }

        public void PanBy(double dx, double dy)
        {
            if (!HasPage) return;
            _tx += dx;
            _ty += dy;
            ViewChanged();
        }

        // ------------------------------------------------------------------ 그리기
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(SpaceColor);
            if (HasPage)
            {
                var view = new RectangleF(-20, -20, ClientSize.Width + 40, ClientSize.Height + 40);
                var page = new RectangleF((float)_tx, (float)_ty, (float)(_pw * _s), (float)(_ph * _s));
                var shadow = RectangleF.Intersect(new RectangleF(page.X - 1, page.Y + 2, page.Width + 2, page.Height + 2), view);
                if (shadow.Width > 0 && shadow.Height > 0)
                    using (var sb = new SolidBrush(Color.FromArgb(100, 0, 0, 0))) g.FillRectangle(sb, shadow);
                var paper = RectangleF.Intersect(page, view);
                if (paper.Width > 0 && paper.Height > 0) g.FillRectangle(Brushes.White, paper);

                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.InterpolationMode = InterpolationMode.Bilinear;
                if (_base != null) DrawLayer(g, _base, new RectangleF(0, 0, _pw, _ph));
                if (_hi != null)
                {
                    if (Math.Abs(_s / _hiScale - 1) < 1e-9) g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    DrawLayer(g, _hi, _hiRect);
                }
            }
            if (ShowCross)
            {
                int x = _mouse.Value.X, y = _mouse.Value.Y;
                using (var pen = new Pen(CrossColor, 1))
                {
                    g.PixelOffsetMode = PixelOffsetMode.Default;
                    g.DrawLine(pen, 0, y, ClientSize.Width, y);
                    g.DrawLine(pen, x, 0, x, ClientSize.Height);
                    g.DrawRectangle(pen, x - 4, y - 4, 8, 8);
                }
            }
        }

        private bool ShowCross
        {
            get { return Cfg != null && Cfg.Crosshair && HasPage && _mouse.HasValue && !_drag.HasValue; }
        }

        /// <summary>bmp(용지 영역 src를 덮음) 중 화면에 보이는 부분만 그린다.</summary>
        private void DrawLayer(Graphics g, Bitmap bmp, RectangleF src)
        {
            var visible = RectangleF.FromLTRB(
                (float)(-_tx / _s), (float)(-_ty / _s),
                (float)((ClientSize.Width - _tx) / _s), (float)((ClientSize.Height - _ty) / _s));
            var vis = RectangleF.Intersect(src, visible);
            vis = RectangleF.Intersect(vis, new RectangleF(0, 0, _pw, _ph));
            if (vis.Width <= 0 || vis.Height <= 0 || src.Width <= 0 || src.Height <= 0) return;
            float kx = bmp.Width / src.Width, ky = bmp.Height / src.Height;
            var srcPx = new RectangleF((vis.X - src.X) * kx, (vis.Y - src.Y) * ky, vis.Width * kx, vis.Height * ky);
            float dx = (float)(_tx + vis.X * _s), dy = (float)(_ty + vis.Y * _s);
            float dw = (float)(vis.Width * _s), dh = (float)(vis.Height * _s);
            var pts = new[] { new PointF(dx, dy), new PointF(dx + dw, dy), new PointF(dx, dy + dh) };
            g.DrawImage(bmp, pts, srcPx, GraphicsUnit.Pixel, _clampAttr);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (HasPage && _lastSize.Width > 0 && _lastSize.Height > 0)
            {
                _tx += (ClientSize.Width - _lastSize.Width) / 2.0;
                _ty += (ClientSize.Height - _lastSize.Height) / 2.0;
            }
            _lastSize = ClientSize;
            ViewChanged();
        }

        // ------------------------------------------------------------------ 마우스
        public void UpdateCursor()
        {
            if (_drag.HasValue) Cursor = Cursors.SizeAll;
            else if (Cfg != null && Cfg.Crosshair && HasPage) Cursor = _blankCursor;
            else Cursor = Cursors.Cross;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (!HasPage) return;
            if (e.Button == MouseButtons.Middle && e.Clicks >= 2)
            {
                _drag = null;
                UpdateCursor();
                ZoomExtents();
                return;
            }
            bool leftPan = e.Button == MouseButtons.Left && (SpaceDown || (Cfg != null && Cfg.LeftDragPan));
            if (e.Button == MouseButtons.Middle || leftPan)
            {
                _drag = e.Location;
                UpdateCursor();
                Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point? old = _mouse;
            _mouse = e.Location;
            if (_drag.HasValue)
            {
                int dx = e.X - _drag.Value.X, dy = e.Y - _drag.Value.Y;
                _drag = e.Location;
                if (dx != 0 || dy != 0) PanBy(dx, dy);
                return;
            }
            OnStateChanged();
            if (Cfg != null && Cfg.Crosshair && HasPage)
            {
                InvalidateCross(old);
                InvalidateCross(_mouse);
            }
        }

        private void InvalidateCross(Point? p)
        {
            if (!p.HasValue) return;
            Invalidate(new Rectangle(0, p.Value.Y - 5, ClientSize.Width, 11));
            Invalidate(new Rectangle(p.Value.X - 5, 0, 11, ClientSize.Height));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_drag.HasValue && (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Left))
            {
                _drag = null;
                UpdateCursor();
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_drag.HasValue)
            {
                _mouse = null;
                Invalidate();
                OnStateChanged();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (!HasPage || e.Delta == 0) return;
            double d = Math.Max(-360, Math.Min(360, e.Delta));
            if (Cfg != null && Cfg.InvertWheel) d = -d;
            double speed = Cfg != null ? Cfg.WheelSpeed : 1.0;
            ZoomAt(e.X, e.Y, Math.Exp(d / 120.0 * 0.18 * speed));
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            SpaceDown = false;
        }

        // ------------------------------------------------------------------ 기타
        private void SetBusy(int d)
        {
            bool was = _busy > 0;
            _busy = Math.Max(0, _busy + d);
            if (was != (_busy > 0) && BusyChanged != null) BusyChanged(this, EventArgs.Empty);
        }

        private void OnStateChanged()
        {
            if (StateChanged != null) StateChanged(this, EventArgs.Empty);
        }

        private void UI(Action a)
        {
            if (IsDisposed || !IsHandleCreated) return;
            try { BeginInvoke(a); } catch (InvalidOperationException) { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hiTimer.Dispose();
                lock (Pdfium.Sync)
                {
                    _pageGen++;
                    if (_page != IntPtr.Zero) Pdfium.FPDF_ClosePage(_page);
                    _page = IntPtr.Zero;
                }
                DisposeBitmaps();
                _clampAttr.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
