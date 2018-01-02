using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emulators.Common;

namespace Emulators.Test
{
   class Program
   {
      static void Main(string[] args)
      {
         NesRomReader nrr = new NesRomReader(@"C:\Downloads\enulation\fceux\10-Yard Fight.nes");
         
         Console.WriteLine("MirrorMode           = " + nrr.MirroringMode.ToString());
         Console.WriteLine("BatteryBackedRam     = " + nrr.BatteryBackedRam);
         Console.WriteLine("Trainer              = " + (nrr.Trainer != null ? nrr.Trainer.Length : 0));
         Console.WriteLine("FourScreenVramLayout = " + nrr.FourScreenVramLayout);
         Console.WriteLine("MapperType           = " + nrr.MapperType);
         Console.WriteLine("RamBankCount         = " + nrr.RamBankCount);
         Console.WriteLine("Format               = " + nrr.Format.ToString());
         Console.WriteLine("RomBankCount         = " + nrr.RomBankCount);
         Console.WriteLine("VRomBanksCount       = " + nrr.VRomBankCount);

         Console.WriteLine("\npress any key...");
         Console.ReadKey(true);
      }
   }
}
