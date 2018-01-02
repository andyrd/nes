using Emulators.Common;
using Emulators.Core;

namespace Emulators.Core
{
   public class NesCpuBytePointerArray : BytePointerArray
   {
      private NesPpu m_ppu;
      private bool m_firstWrite = true; //if false, second write

      private ushort AddressIncrement
      {
         get
         {
            return (ushort)((base[0x2000] & 0x04) == 0 ? 0x1 : 0x20);
         }
      }

      public override byte this[int index]
      {
         get
         {
            byte result = 0;

            switch (index)
            {
               case 0x2002: //clear the vblank flag (bit 7) on reads from 2002h
               {            //reset 2005h and 2006h first/second write toggle
                  result = base[index];
                  base[index] &= 0x7F;
                  m_firstWrite = true;                  
               } break;
               case 0x2007: //vram data register
               {
                  result = m_ppu.VideoRam[m_ppu.VramAddressRegister];
                  m_ppu.VramAddressRegister += AddressIncrement;
               } break;
               default:
               {
                  result = base[index];
               } break;
            }

            return result;
         }
         set
         {
            base[index] = value;

            switch(index)
            {           
               case 0x2000: //name table address
               {
                  m_ppu.TempVramAddressRegister &= 0xF3FF;
                  m_ppu.TempVramAddressRegister |= (ushort)((base[index] & 0x03) << 10);
               } break;            
               case 0x2004: //sprite ram i/o
               {
                  m_ppu.SpriteRam[base[0x2003]] = value;
                  base[0x2003]++;
               } break;            
               case 0x2005: //vram address register 1
               {
                  if(m_firstWrite)
                  {
                     m_ppu.TempVramAddressRegister &= 0xFFE0;
                     m_ppu.TempVramAddressRegister |= (ushort)(base[index] >> 3);
                     m_ppu.TileXOffset = (byte)(base[index] & 0x07);
                  }
                  else
                  {
                     m_ppu.TempVramAddressRegister &= 0x8C1F;
                     m_ppu.TempVramAddressRegister |= (ushort)((base[index] & 0xF8) << 5);
                     m_ppu.TempVramAddressRegister |= (ushort)((base[index] & 0x07) << 12);
                  }
               } break;            
               case 0x2006: //vram address register 2
               {
                  if (m_firstWrite)
                  {
                     m_ppu.TempVramAddressRegister &= 0x00FF;
                     m_ppu.TempVramAddressRegister |= (ushort)((base[index] & 0x3F) << 8);                  
                  }
                  else
                  {
                     m_ppu.TempVramAddressRegister &= 0xFF00;
                     m_ppu.TempVramAddressRegister |= base[index];
                     m_ppu.VramAddressRegister = m_ppu.TempVramAddressRegister;
                  }

                  m_firstWrite = !m_firstWrite;
               } break;            
               case 0x2007: //vram data register
               {
                  m_ppu.VideoRam[m_ppu.VramAddressRegister] = value;
                  m_ppu.VramAddressRegister += AddressIncrement;
               } break;
               case 0x4014: //sprite DMA
               {
                  int dmaSourceAddress = value * 0x100;

                  for (int i = 0; i < 0xFF; i++)
                  {
                     m_ppu.SpriteRam[i] = base[i + dmaSourceAddress];
                  }
               } break;
            }
         }
      }

      unsafe public NesCpuBytePointerArray(byte*[] initalValue, 
         NesPpu ppu) : base(initalValue)
      {
         m_ppu = ppu;
      } 
   }
}
