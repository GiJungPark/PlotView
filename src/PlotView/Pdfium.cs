using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PlotView
{
    /// <summary>PDFium 네이티브 함수. PDFium은 스레드 안전하지 않으므로 모든 호출은 Sync 잠금 안에서 한다.</summary>
    internal static class Pdfium
    {
        public static readonly object Sync = new object();
        private const string Dll = "pdfium";

        public const int FPDF_ANNOT = 0x01;
        public const int FPDFBitmap_BGRA = 4;
        public const uint FPDF_ERR_PASSWORD = 4;

        [DllImport(Dll)] public static extern void FPDF_InitLibrary();
        [DllImport(Dll)] public static extern IntPtr FPDF_LoadMemDocument(IntPtr dataBuf, int size, byte[] password);
        [DllImport(Dll)] public static extern void FPDF_CloseDocument(IntPtr document);
        [DllImport(Dll)] public static extern uint FPDF_GetLastError();
        [DllImport(Dll)] public static extern int FPDF_GetPageCount(IntPtr document);
        [DllImport(Dll)] public static extern IntPtr FPDF_LoadPage(IntPtr document, int pageIndex);
        [DllImport(Dll)] public static extern void FPDF_ClosePage(IntPtr page);
        [DllImport(Dll)] public static extern float FPDF_GetPageWidthF(IntPtr page);
        [DllImport(Dll)] public static extern float FPDF_GetPageHeightF(IntPtr page);
        [DllImport(Dll)] public static extern IntPtr FPDFBitmap_CreateEx(int width, int height, int format, IntPtr firstScan, int stride);
        [DllImport(Dll)] public static extern void FPDFBitmap_Destroy(IntPtr bitmap);
        [DllImport(Dll)] public static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page,
            int startX, int startY, int sizeX, int sizeY, int rotate, int flags);
    }

    /// <summary>메모리에 올린 PDF 문서.</summary>
    internal sealed class PdfDocument : IDisposable
    {
        private IntPtr _mem;
        public IntPtr Handle { get; private set; }
        public int PageCount { get; private set; }

        private PdfDocument() { }

        /// <param name="askPassword">비밀번호 요청. 인자는 '이전 비밀번호가 틀림' 여부. null을 돌려주면 취소.</param>
        public static PdfDocument Open(string path, Func<bool, string> askPassword)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var d = new PdfDocument();
            d._mem = Marshal.AllocHGlobal(bytes.Length);
            Marshal.Copy(bytes, 0, d._mem, bytes.Length);

            string pw = null;
            bool retry = false;
            while (true)
            {
                IntPtr h;
                uint err;
                lock (Pdfium.Sync)
                {
                    byte[] pwBytes = pw == null ? null : Encoding.UTF8.GetBytes(pw + "\0");
                    h = Pdfium.FPDF_LoadMemDocument(d._mem, bytes.Length, pwBytes);
                    err = h == IntPtr.Zero ? Pdfium.FPDF_GetLastError() : 0;
                }
                if (h != IntPtr.Zero)
                {
                    d.Handle = h;
                    lock (Pdfium.Sync) d.PageCount = Pdfium.FPDF_GetPageCount(h);
                    return d;
                }
                if (err == Pdfium.FPDF_ERR_PASSWORD)
                {
                    pw = askPassword(retry);
                    retry = true;
                    if (pw != null) continue;
                    d.Dispose();
                    return null;
                }
                d.Dispose();
                throw new IOException("PDF 파일을 읽을 수 없습니다. 손상되었거나 PDF가 아닐 수 있습니다. (PDFium 오류 " + err + ")");
            }
        }

        public void Dispose()
        {
            lock (Pdfium.Sync)
            {
                if (Handle != IntPtr.Zero) Pdfium.FPDF_CloseDocument(Handle);
                Handle = IntPtr.Zero;
            }
            if (_mem != IntPtr.Zero) Marshal.FreeHGlobal(_mem);
            _mem = IntPtr.Zero;
        }
    }
}
