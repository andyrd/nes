using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Common
{   
   public interface INesRomReader
   {
      Mirroring MirroringMode { get; }

      bool BatteryBackedRam { get; }

      byte[] Trainer { get; }

      bool FourScreenVramLayout { get; }

      int MapperType { get; }

      int RamBankCount { get; }

      FormatStandard Format { get; }

      int RomBankCount { get; }

      byte[] RomBanks { get; }

      int VRomBankCount { get; }

      byte[] VRomBanks { get; }
   }

   public enum Mirroring
   {
      Vertical,
      Horizontal
   }

   public enum FormatStandard
   {
      PAL,
      NTSC
   }
}
