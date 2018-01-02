using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Emulators.Common
{
   //   Byte     Contents
   //---------------------------------------------------------------------------
   //0-3      String "NES^Z" used to recognize .NES files.
   //4        Number of 16kB ROM banks.
   //5        Number of 8kB VROM banks.
   //6        bit 0     1 for vertical mirroring, 0 for horizontal mirroring.
   //         bit 1     1 for battery-backed RAM at $6000-$7FFF.
   //         bit 2     1 for a 512-byte trainer at $7000-$71FF.
   //         bit 3     1 for a four-screen VRAM layout. 
   //         bit 4-7   Four lower bits of ROM Mapper Type.
   //7        bit 0     1 for VS-System cartridges.
   //         bit 1-3   Reserved, must be zeroes!
   //         bit 4-7   Four higher bits of ROM Mapper Type.
   //8        Number of 8kB RAM banks. For compatibility with the previous
   //         versions of the .NES format, assume 1x8kB RAM page when this
   //         byte is zero.
   //9        bit 0     1 for PAL cartridges, otherwise assume NTSC.
   //         bit 1-7   Reserved, must be zeroes!
   //10-15    Reserved, must be zeroes!
   //16-...   ROM banks, in ascending order. If a trainer is present, its
   //         512 bytes precede the ROM bank contents.
   //...-EOF  VROM banks, in ascending order.
   public class NesRomReader : INesRomReader
   {
      private string m_filePath;

      public NesRomReader(string filePath)
      {
         m_filePath = filePath;
         Parse();
      }

      #region INesRomReader Members

      public Mirroring MirroringMode { get; private set; }

      public bool BatteryBackedRam { get; private set; }

      public byte[] Trainer { get; private set; }

      public bool FourScreenVramLayout { get; private set; }

      public int MapperType { get; private set; }

      public int RamBankCount { get; private set; }

      public FormatStandard Format { get; private set; }

      public int RomBankCount { get; private set; }

      public byte[] RomBanks { get; private set; }

      public int VRomBankCount { get; private set; }

      public byte[] VRomBanks { get; private set; }            

      #endregion

      private void Parse()
      {
         using (FileStream reader = new FileStream(
            m_filePath, FileMode.Open, FileAccess.Read))
         {            
            byte[] header = new byte[4];
            reader.Read(header, 0, 4);

            if (header[0] == 0x4E &&
                header[1] == 0x45 &&
                header[2] == 0x53 &&
                header[3] == 0x1A)
            {
               RomBankCount = reader.ReadByte();
               VRomBankCount = reader.ReadByte();
               
               int byte6 = reader.ReadByte();
               MirroringMode = (byte6 & 0x01) != 0 ? 
                  Mirroring.Vertical : Mirroring.Horizontal;
               BatteryBackedRam = (byte6 & 0x02) != 0;
               bool trainerPresent = (byte6 & 0x04) != 0;
               FourScreenVramLayout = (byte6 & 0x08) != 0;
               MapperType = (byte6 & 0xF0) >> 4;

               int byte7 = reader.ReadByte();
               MapperType |= (byte7 & 0xF0);
               
               int byte8 = reader.ReadByte();
               RamBankCount = byte8 == 0 ? 1 : byte8;

               int byte9 = reader.ReadByte();
               Format = (byte9 & 0x01) != 0 ? FormatStandard.PAL : FormatStandard.NTSC;
               
               //bytes 10-15 are zeroes
               reader.Seek(6, SeekOrigin.Current);

               if(trainerPresent)
               {
                  Trainer = new byte[512];
                  reader.Read(Trainer, 0, Trainer.Length);
               }

               if (RomBankCount > 0)
               {
                  //rom banks are 16KB
                  RomBanks = new byte[RomBankCount * 0x4000];
                  reader.Read(RomBanks, 0, RomBanks.Length);
               }

               if (VRomBankCount > 0)
               {
                  //vrom banks are 8KB
                  VRomBanks = new byte[VRomBankCount * 0x2000];
                  reader.Read(VRomBanks, 0, VRomBanks.Length);
               }
            }
            else
            {
               throw new InvalidOperationException(string.Format("Header mismatch for {0}", m_filePath));               
            }
         }
      }
   }
}
