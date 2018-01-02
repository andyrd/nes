using Emulators.Common;
using System.Threading;
using System;

namespace Emulators.Core
{
   public class NesPpu : INesPpu
   {
      #region Control registers

      private ushort NameTableAddress
      {
         get
         {
            ushort address = 0;

            switch (Cpu.Memory[0x2000] & 0x03)
            {
               case (0): address = 0x2000; break;
               case (1): address = 0x2400; break;
               case (2): address = 0x2800; break;
               case (3): address = 0x2C00; break;
            }

            return address;
         }
         set
         {
            Cpu.Memory[0x2000] &= 0xFC;

            switch (value)
            {
               case (0x2000): break;
               case (0x2400): Cpu.Memory[0x2000] += 1; break;
               case (0x2800): Cpu.Memory[0x2000] += 2; break;
               case (0x2C00): Cpu.Memory[0x2000] += 3; break;
            }
         }
      }

      private ushort AddressIncrement
      {
         get
         {
            return (ushort)((Cpu.Memory[0x2000] & 0x04) == 0 ? 0x1 : 0x20);
         }
      }

      private ushort SpritePatternTableAddress
      {
         get
         {
            return (ushort)((Cpu.Memory[0x2000] & 0x08) == 0 ? 0x0 : 0x1000);
         }
      }

      private ushort BackgroundPatternTableAddress
      {
         get
         {
            return (ushort)((Cpu.Memory[0x2000] & 0x10) == 0 ? 0x0 : 0x1000);
         }
      }

      private int SpriteSize
      {
         get
         {
            return (Cpu.Memory[0x2000] & 0x20);
         }
      }

      private bool NmiOnVblank
      {
         get
         {
            return (Cpu.Memory[0x2000] & 0x80) != 0;
         }
      }

      private bool MonochromeMode
      {
         get
         {
            return (Cpu.Memory[0x2001] & 0x01) != 0;
         }
      }

      private bool ClipBackground
      {
         get
         {
            return (Cpu.Memory[0x2001] & 0x02) == 0;
         }
      }

      private bool ClipSprites
      {
         get
         {
            return (Cpu.Memory[0x2001] & 0x04) == 0;
         }
      }

      private bool BackgroundEnabled
      {
         get
         {
            return (Cpu.Memory[0x2001] & 0x08) != 0;
         }
      }

      private bool SpritesEnabled
      {
         get
         {
            return (Cpu.Memory[0x2001] & 0x10) != 0;
         }
      }

      private int ColorIntensity
      {
         get
         {
            return (Cpu.Memory[0x2001] & 0xE0);
         }
      }

      private bool IgnoreVramWrites
      {
         get
         {
            return m_ignoreVramWrites;
         }
         set
         {
            if (value) Cpu.Memory[0x2002] |= 0x10; 
            else Cpu.Memory[0x2002] &= 0xEF;

            m_ignoreVramWrites = value;
         }
      } private bool m_ignoreVramWrites;

      private bool SpriteScanlineOverflow
      {
         get
         {
            return m_spriteScanlineOverflow;
         }
         set
         {
            if (value) Cpu.Memory[0x2002] |= 0x20;
            else Cpu.Memory[0x2002] &= 0xDF;

            m_spriteScanlineOverflow = value;
         }
      } private bool m_spriteScanlineOverflow;

      private bool SpriteZeroHitFlag
      {
         set
         {
            if (value) Cpu.Memory[0x2002] |= 0x40;
            else Cpu.Memory[0x2002] &= 0xBF;
         }
      }

      private bool VblankInProcess
      {
         set
         {
            if (value) Cpu.Memory[0x2002] |= 0x80;
            else Cpu.Memory[0x2002] &= 0x7F;
         }
      }      

      #endregion

      private bool m_exitMainLoop = false;
      private IDualClockSync m_sync;
      private Thread m_mainThread;
      private SpriteAttributes[] m_spriteTempMemory = new SpriteAttributes[8];
      private int m_inRangeSpriteCount;
      private bool m_primaryObjectInRange;

      public INesVideoOut VideoOut { get; set; }

      public I6502 Cpu { get; set; }

      public ushort VramAddressRegister { get; set; }

      public ushort TempVramAddressRegister { get; set; }

      public byte TileXOffset { get; set; }

      private NesPpu()
      {         
      }

      public NesPpu(IDualClockSync sync)
      {
         m_sync = sync;

         for (int i = 0; i < m_spriteTempMemory.Length; i++)
         {
            m_spriteTempMemory[i] = new SpriteAttributes();
         }
      }

      #region INesPpu Members

      public BytePointerArray VideoRam { get; set; }

      public BytePointerArray SpriteRam { get; set; }      

      public void BeginRun()
      {
         m_mainThread = new Thread(RunMainLoop);
         m_mainThread.Start();
      }

      private void RunMainLoop()
      {         
         while (!m_exitMainLoop)
         {
            m_sync.WaitForClockA();

            if (NmiOnVblank)
            {
               Cpu.GenerateNMI();
            }

            VblankInProcess = true;

            //wait for 20 scanlines (256*20)
            m_sync.IncrementClockB(5120);
            m_sync.WaitForClockA();

            if (BackgroundEnabled && SpritesEnabled)
            {
               VramAddressRegister = TempVramAddressRegister;
            }

            InRangeObjectEvaluation(0);
         }
      }

      private void InRangeObjectEvaluation(int scanline)
      {
         m_inRangeSpriteCount = 0;
         SpriteScanlineOverflow = false;

         for (int i = 0; i < 256; i+=4)
         {
            int delta = Math.Abs((SpriteRam[0] + 1) - scanline);

            if ((SpriteSize == 0 && delta <= 8) ||
                (SpriteSize == 1 && delta <= 15))
            {
               m_inRangeSpriteCount++;

               if (m_inRangeSpriteCount < m_spriteTempMemory.Length)
               {
                  m_spriteTempMemory[m_inRangeSpriteCount].ParseSpriteRam(SpriteRam, i);
               }
               else
               {
                  SpriteScanlineOverflow = true;
                  break;
               }
            }
         }
      }

      private void RenderScanline(int scanline)
      {

      }      

      #endregion  
   }
}
