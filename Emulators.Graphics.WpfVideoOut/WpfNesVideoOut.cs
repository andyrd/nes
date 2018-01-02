using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Emulators.Common;

namespace Emulators.Graphics
{
   public class WpfNesVideoOut : Control, INesVideoOut
   {
      public const int ScreenHeight = 240;
      public const int ScreenWidth = 256;
      const int PixelCount = ScreenHeight * ScreenWidth;

      [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Auto)]
      private static extern IntPtr CreateFileMapping(
         IntPtr hFile, 
         IntPtr lpAttributes, 
         int flProtect, 
         int dwMaximumSizeLow, 
         int dwMaximumSizeHigh, 
         string lpName);

      [DllImport("kernel32.dll", SetLastError = true)]
      static extern IntPtr MapViewOfFile(
         IntPtr hFileMappingObject,
         int dwDesiredAccess,
         int dwFileOffsetHigh,
         int dwFileOffsetLow,
         int dwNumberOfBytesToMap);

      [DllImport("kernel32", SetLastError = true)]
      private static extern bool CloseHandle(IntPtr handle);        

      private IntPtr m_bitmapFileMapping;
      private unsafe int* m_bitmapFileView;
      private InteropBitmap m_bitmapSource;
      private Rect m_drawImageRect = new Rect(0, 0, ScreenWidth, ScreenHeight);

      #region Palette

      private int[] m_palette = 
      {
         AssembleColor(0x80,0x80,0x80), AssembleColor(0x00,0x3D,0xA6), 
         AssembleColor(0x00,0x12,0xB0), AssembleColor(0x44,0x00,0x96),
         AssembleColor(0xA1,0x00,0x5E), AssembleColor(0xC7,0x00,0x28), 
         AssembleColor(0xBA,0x06,0x00), AssembleColor(0x8C,0x17,0x00),
         AssembleColor(0x5C,0x2F,0x00), AssembleColor(0x10,0x45,0x00), 
         AssembleColor(0x05,0x4A,0x00), AssembleColor(0x00,0x47,0x2E),
         AssembleColor(0x00,0x41,0x66), AssembleColor(0x00,0x00,0x00),
         AssembleColor(0x05,0x05,0x05), AssembleColor(0x05,0x05,0x05),
         AssembleColor(0xC7,0xC7,0xC7), AssembleColor(0x00,0x77,0xFF),
         AssembleColor(0x21,0x55,0xFF), AssembleColor(0x82,0x37,0xFA),
         AssembleColor(0xEB,0x2F,0xB5), AssembleColor(0xFF,0x29,0x50),
         AssembleColor(0xFF,0x22,0x00), AssembleColor(0xD6,0x32,0x00),
         AssembleColor(0xC4,0x62,0x00), AssembleColor(0x35,0x80,0x00),
         AssembleColor(0x05,0x8F,0x00), AssembleColor(0x00,0x8A,0x55),
         AssembleColor(0x00,0x99,0xCC), AssembleColor(0x21,0x21,0x21),
         AssembleColor(0x09,0x09,0x09), AssembleColor(0x09,0x09,0x09),
         AssembleColor(0xFF,0xFF,0xFF), AssembleColor(0x0F,0xD7,0xFF),
         AssembleColor(0x69,0xA2,0xFF), AssembleColor(0xD4,0x80,0xFF),
         AssembleColor(0xFF,0x45,0xF3), AssembleColor(0xFF,0x61,0x8B),
         AssembleColor(0xFF,0x88,0x33), AssembleColor(0xFF,0x9C,0x12),
         AssembleColor(0xFA,0xBC,0x20), AssembleColor(0x9F,0xE3,0x0E),
         AssembleColor(0x2B,0xF0,0x35), AssembleColor(0x0C,0xF0,0xA4),
         AssembleColor(0x05,0xFB,0xFF), AssembleColor(0x5E,0x5E,0x5E),
         AssembleColor(0x0D,0x0D,0x0D), AssembleColor(0x0D,0x0D,0x0D),
         AssembleColor(0xFF,0xFF,0xFF), AssembleColor(0xA6,0xFC,0xFF),
         AssembleColor(0xB3,0xEC,0xFF), AssembleColor(0xDA,0xAB,0xEB),
         AssembleColor(0xFF,0xA8,0xF9), AssembleColor(0xFF,0xAB,0xB3),
         AssembleColor(0xFF,0xD2,0xB0), AssembleColor(0xFF,0xEF,0xA6),
         AssembleColor(0xFF,0xF7,0x9C), AssembleColor(0xD7,0xE8,0x95),
         AssembleColor(0xA6,0xED,0xAF), AssembleColor(0xA2,0xF2,0xDA),
         AssembleColor(0x99,0xFF,0xFC), AssembleColor(0xDD,0xDD,0xDD),
         AssembleColor(0x11,0x11,0x11), AssembleColor(0x11,0x11,0x11)
      };

      #endregion

      public WpfNesVideoOut()
      {                  
         m_bitmapFileMapping = CreateFileMapping(
            new IntPtr(-1),
            IntPtr.Zero,
            0x04, //PAGE_READWRITE
            0,
            PixelCount * 4,
            null);

         unsafe
         {
            m_bitmapFileView = (int*)MapViewOfFile(
               m_bitmapFileMapping,
               0xF001F, //FILE_MAP_ALL_ACCESS
               0,
               0,
               PixelCount * 4).ToPointer();            
         }
         
         m_bitmapSource = (InteropBitmap)Imaging.CreateBitmapSourceFromMemorySection(
            m_bitmapFileMapping,
            ScreenWidth,
            ScreenHeight,
            PixelFormats.Bgr32,
            (ScreenWidth * PixelFormats.Bgr32.BitsPerPixel) / 8,
            0);
      }

      ~WpfNesVideoOut()
      {
         if (m_bitmapFileMapping != IntPtr.Zero)
         {
            CloseHandle(m_bitmapFileMapping);
         }
      }

      protected override void OnRender(DrawingContext drawingContext)
      {
         drawingContext.DrawImage(m_bitmapSource, m_drawImageRect);
      }      

      #region INesVideoOut Members

      public void PlotPixel(int x, int y, int paletteIndex)
      {
         unsafe
         {            
            m_bitmapFileView[y * ScreenWidth + x] =  m_palette[paletteIndex];
         }         
      }

      public void Invalidate()
      {
         Dispatcher.Invoke((Action)(() => m_bitmapSource.Invalidate()));
      }

      #endregion

      private static int AssembleColor(int r, int b, int g)
      {
         return (r << 16) | (g << 8) | b;
      }
   }
}
