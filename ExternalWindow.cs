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
    #region Windows Constants Constants

    public enum Message
    {
        WM_MOUSEMOVE = 0x200,

        MK_CONTROL      = 0x0008,
        MK_LBUTTON      = 0x0001,
        MK_MBUTTON      = 0x0010,
        MK_RBUTTON      = 0x0002,
        MK_SHIFT        = 0x0004,
        MK_XBUTTON1     = 0x0020,
        MK_XBUTTON2     = 0x0040
    };
    #endregion

    public class ExternalWindow : MonoBehaviour
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
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")]
        static extern IntPtr RealChildWindowFromPoint(IntPtr hwndParent, POINT ptParentClientCoords);
        #endregion

        public RawImage RawImage;
        public string WindowClass;
        public BoxCollider Collider;
        public float SampleRate = 1/3f;

        private IntPtr _hwnd;
        public int OverrideHwnd;

        private int _bitmapWidth = 0, _bitmapHeight = 0;
        private Bitmap _bitmap = null;
        private Graphics _graphics = null;
        private Texture2D _tex = null;
        private Color32[] _colorBuf = null;
        private byte[] _byteBuf = null;
        private RECT _rect;
        private RECT _clientRect;

        void DrillDown()
        {
            IntPtr ptr = _hwnd;
            do
            {
                //ptr = RealChildWindowFromPoint(ptr);
            } while (true);
        }

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
                var aspect = (float)width / height;
                var resize = RawImage.rectTransform.sizeDelta = new Vector2(aspect, 1f) * 0.4f;
                Collider.size = new Vector3(resize.x, resize.y, .01f);

                _colorBuf = new Color32[width * height];
                _byteBuf = new byte[4 * width * height];
            }
        }

        void FillColorBuf()
        {
            var bmpData = _bitmap.LockBits(new RECT(0, 0, _bitmap.Width, _bitmap.Height), ImageLockMode.ReadOnly, _bitmap.PixelFormat);
            var ptr = bmpData.Scan0;

            Marshal.Copy(ptr, _byteBuf, 0, _byteBuf.Length);
            _bitmap.UnlockBits(bmpData);


            var j = 0;
            var k = _byteBuf.Length / 4;
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
                yield return new WaitForSeconds(SampleRate);
            }
        }


        private void RefreshWindow()
        {
            if (OverrideHwnd > 0)
                _hwnd = (IntPtr)OverrideHwnd;
            else
                _hwnd = FindWindow(WindowClass, null);

            if (GetWindowRect(_hwnd, out _rect))
            {
                UpdateBitmap(_rect.Width, _rect.Height);
                var dc = _graphics.GetHdc();
                var success = PrintWindow(_hwnd, dc, 0);
                _graphics.ReleaseHdc(dc);

                FillColorBuf();

                _tex.SetPixels32(_colorBuf);
                _tex.Apply();
            }
        }
        private int MAKELPARAM(int p, int p_2)
        {
            return ((p_2 << 16) | (p & 0xFFFF));
        }

        private int _mouseX, _mouseY;
        private bool[] _mouseDown = new bool[3];

        

        public void SetMouseCoord(Vector2 coord)
        {
            var pt = new POINT(_rect.Left + (int)(coord.x * _rect.Width), _rect.Top + (int)(coord.y * _rect.Height));
            ScreenToClient(_hwnd, ref pt);

            if (pt.X != _mouseX || pt.Y != _mouseY)
            {
                _mouseX = pt.X;
                _mouseY = pt.Y;

                int wParam = 0;

                if (_mouseDown[0])
                    wParam |= (int)Message.MK_LBUTTON;
                if (_mouseDown[1])
                    wParam |= (int)Message.MK_RBUTTON;
                if (_mouseDown[2])
                    wParam |= (int)Message.MK_MBUTTON;
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                    wParam |= (int) Message.MK_CONTROL;
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    wParam |= (int)Message.MK_SHIFT;

                PostMessage(_hwnd, 0x200, wParam, MAKELPARAM(_mouseX, _mouseY));
            }
        }

        public void SetMouseDown(int button)
        {
            _mouseDown[button] = true;
            Debug.Log("CLICK " + _mouseX + ", " + _mouseY);
            PostMessage(_hwnd, 0x201, 0, MAKELPARAM(_mouseX, _mouseY));
        }

        public void SetMouseUp(int button)
        {
            _mouseDown[button] = false;
            Debug.Log("RELEASE " + _mouseX + ", " + _mouseY);
            PostMessage(_hwnd, 0x202, 0, MAKELPARAM(_mouseX, _mouseY));
        }
    }
}