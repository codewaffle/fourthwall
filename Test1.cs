using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Graphics = System.Drawing.Graphics;

namespace FourthWall
{


    public class Test1 : MonoBehaviour
    {
        #region PInvoke
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hDC, uint nFlags);
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        #endregion

        public RawImage RawImage;
        public string WindowClass;

        private IntPtr _hwnd;
        public int OverrideHwnd;
        private int iter = 0;

        private int _bitmapWidth=0, _bitmapHeight=0;
        private Bitmap _bitmap = null;
        private Graphics _graphics = null;
        private Texture2D _tex = null;
        private Color32[] _colorBuf = null;
        private byte[] _byteBuf = null;

        void UpdateBitmap(int width, int height)
        {
            if (_bitmap == null || _graphics == null || width != _bitmapWidth || height != _bitmapHeight)
            {
                _bitmapWidth = width;
                _bitmapHeight = height;
                _bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                _graphics = Graphics.FromImage(_bitmap);
                _tex = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
                RawImage.texture = _tex;
                var aspect = (float) width/height;
                RawImage.rectTransform.sizeDelta = new Vector2(aspect, 1f) * 0.4f;

                _colorBuf = new Color32[width*height];
                _byteBuf = new byte[4*width*height];
            }
        }

        void FillColorBuf()
        {
            var bmpData = _bitmap.LockBits(new RECT(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadOnly, _bitmap.PixelFormat);
            var ptr = bmpData.Scan0;

            Marshal.Copy(ptr, _byteBuf, 0, _byteBuf.Length);
            _bitmap.UnlockBits(bmpData);

            
            var j = 0;
            var k = _byteBuf.Length/4;
            // 3 is alpha
            for (var i = 0; i < _byteBuf.Length; i += 4)
            {
                _colorBuf[j++] = new Color32(
                    _byteBuf[i + 2],
                    _byteBuf[i + 1],
                    _byteBuf[i + 0],
                    _byteBuf[i + 3]); 
            }
        }

        private void Start()
        {
            StartCoroutine(RefreshWindowCoroutine());
        }

        IEnumerator RefreshWindowCoroutine()
        {
            while (enabled)
            {
                RefreshWindow();
                yield return new WaitForSeconds(0.2f);
            }
        }

        private RECT rect;

        private void RefreshWindow()
        {
            if (OverrideHwnd > 0)
                _hwnd = (IntPtr)OverrideHwnd;
            else
                _hwnd = FindWindow(WindowClass, null);

            if (GetWindowRect(_hwnd, out rect))
            {
                UpdateBitmap(rect.Width, rect.Height);
                var dc = _graphics.GetHdc();
                var success = PrintWindow(_hwnd, dc, 0);
                _graphics.ReleaseHdc(dc);

                FillColorBuf();

                _tex.SetPixels32(_colorBuf);
                _tex.Apply();
            }
        }
    }
}