using System;
using System.Runtime.InteropServices;
using Emulators.Common;
using Emulators.Core;

namespace Emulators.Application.AndyNES
{
   class EmulationShell
   {      
      private I6502 m_cpu;
      private INesPpu m_ppu;
      private IntPtr m_cpuAlloc = Marshal.AllocHGlobal(0xC000 + 0x0800 + 0x0008);  
      private IntPtr m_ppuAlloc = Marshal.AllocHGlobal(0x2000 + 0x0800 + 0x0019 + 0x0100);
      private int m_ppuAllocIndex = 0;
      private int m_cpuAllocIndex = 0;
      
      /// <summary>
      /// Clock cycles are based on the ppu (5370000 PPUcc/sec, 3 PPUcc/CPUcc)
      /// Wait 1/60th of a 89500 PPUcc (~29833 CPUcc)
      /// </summary>
      private IDualClockSync m_sync = new NesClockCycleSync(0, 89500);  

      private EmulationShell()
      {
      }      

      public EmulationShell(INesRomReader romReader, INesVideoOut videoOut)
      {
         m_cpu = new Nmos6502(m_sync);
         m_ppu = new NesPpu(m_sync) { Cpu = m_cpu, VideoOut = videoOut };

         InitializeSpriteMemory(romReader);
         InitializeCpuMemory(romReader);
         InitializePpuMemory(romReader);

         m_cpu.BeginRun();
         m_ppu.BeginRun();
     }

      ~EmulationShell()
      {
         Marshal.FreeHGlobal(m_cpuAlloc);
         Marshal.FreeHGlobal(m_ppuAlloc);
      }

      private void InitializeSpriteMemory(INesRomReader romReader)
      {
         unsafe
         {
            byte*[] spriteRam = new byte*[0x100];

            //sprite ram 0h to FFh
            for (int i = 0; i < 0x100; i++)
            {
               spriteRam[i] = ((byte*)m_ppuAlloc) + m_ppuAllocIndex++;
               *(spriteRam[i]) = 0;
            }

            m_ppu.SpriteRam = new BytePointerArray(spriteRam);
         }
      }

      private void InitializePpuMemory(INesRomReader romReader)
      {
         unsafe
         {
            byte*[] videoRam = new byte*[0x10000];

            //pattern tables (0h to FFFh and 1000h to 1FFFH)
            for (int i = 0; i < 0x2000; i++)
            {
               videoRam[i] = ((byte*)m_ppuAlloc) + m_ppuAllocIndex++;
               *(videoRam[i]) = romReader.VRomBanks[i];
            }

            //name table 0 (2000h to 23FFh)
            for (int i = 0x2000; i < 0x2400; i++)
            {
               videoRam[i] = ((byte*)m_ppuAlloc) + m_ppuAllocIndex++;
               *(videoRam[i]) = 0;
            }

            //name table 3 (2C00h to 2FFFh)
            for (int i = 0x2C00; i < 0x3000; i++)
            {
               videoRam[i] = ((byte*)m_ppuAlloc) + m_ppuAllocIndex++;
               *(videoRam[i]) = 0;
            }

            if (romReader.MirroringMode == Mirroring.Horizontal)
            {
               //name table 0 and 1 mirrored
               for (int i = 0x2400, j = 0x2000; i < 0x2800; i++, j++)
               {
                  videoRam[i] = videoRam[j];
               }

               //name table 2 and 3 mirrored
               for (int i = 0x2800, j = 0x2C00; i < 0x2C00; i++, j++)
               {
                  videoRam[i] = videoRam[j];
               }
            }
            else if (romReader.MirroringMode == Mirroring.Vertical)
            {
               //name table 0 and 2 mirrored
               for (int i = 0x2800, j = 0x2000; i < 0x2C00; i++, j++)
               {
                  videoRam[i] = videoRam[j];
               }

               //name table 1 and 3 mirrored
               for (int i = 0x2400, j = 0x2C00; i < 0x2800; i++, j++)
               {
                  videoRam[i] = videoRam[j];
               }
            }

            //mirrors of 2000h to 2EFFh
            for (int i = 0x3000, j = 0x2000; i < 0x3F00; i++, j++)
            {
               videoRam[i] = videoRam[j];
            }

            //palettes (image 3F00h to 3F0Fh, sprite 3F10h to 3F1Fh,
            //mirrors of 3F00h every 4 bytes)
            videoRam[0x3F00] = ((byte*)m_ppuAlloc) + m_ppuAllocIndex++;
            *(videoRam[0x3F00]) = 0;

            for (int i = 0x3F01; i < 0x3F20; i++)
            {
               if ((i % 4) == 0)
               {
                  videoRam[i] = videoRam[0x3F00];
               }
               else
               {
                  videoRam[i] = ((byte*)m_ppuAlloc) + m_ppuAllocIndex++;
                  *(videoRam[i]) = 0;
               }
            }

            //mirrors of 3F00h to 3F1Fh
            for (int i = 0x3F20; i < 0x4000; i++)
            {
               videoRam[i] = videoRam[0x3F00 + (i % 20)];
            }

            //mirrors of 0h to 3FFFh
            for (int i = 0; i < 0x4000; i++)
            {
               videoRam[i + 0x4000] = videoRam[i];
               videoRam[i + 0x4000 * 2] = videoRam[i];
               videoRam[i + 0x4000 * 3] = videoRam[i];
            }

            m_ppu.VideoRam = new BytePointerArray(videoRam);
         }
      }

      private void InitializeCpuMemory(INesRomReader romReader)
      {
         unsafe
         {
            byte*[] cpuMemory = new byte*[0x10000];            

            //zero page, stack, ram (200h to 800h)
            for (int i = 0; i < 0x0800; i++)
            {
               cpuMemory[i] = ((byte*)m_cpuAlloc) + m_cpuAllocIndex++;
               *(cpuMemory[i]) = 0xFF;
               
               cpuMemory[i + 0x800] = cpuMemory[i];
               cpuMemory[i + 0x800 * 2] = cpuMemory[i];
               cpuMemory[i + 0x800 * 3] = cpuMemory[i];
            }

            //ppu control registers
            for (int i = 0x2000; i < 0x2008; i++)
            {
               cpuMemory[i] = ((byte*)m_cpuAlloc) + m_cpuAllocIndex++;
               *(cpuMemory[i]) = 0;
            }

            //mirrors of ppu control registers
            for (int i = 0x2008; i < 0x4000; i++)
            {
               cpuMemory[i] = cpuMemory[0x2000 + (i % 8)];
            }

            //apu control registers (4000h to 401Fh), expansion rom (4020h to 5FFFh),
            //sram 6000h to 7FFFh
            for (int i = 0x4000; i < 0x8000; i++)
            {
               cpuMemory[i] = ((byte*)m_cpuAlloc) + m_cpuAllocIndex++;
               *(cpuMemory[i]) = 0;
            }

            //program rom (8000h to FFFFh)
            if (romReader.MapperType == 0)
            {
               if (romReader.RomBankCount == 1)
               {
                  for (int i = 0x8000, j = 0; i < 0xC000; i++, j++)
                  {
                     cpuMemory[i] = ((byte*)m_cpuAlloc) + m_cpuAllocIndex++;
                     *(cpuMemory[i]) = romReader.RomBanks[j];
                  }

                  for (int i = 0xC000, j = 0; i < 0x10000; i++, j++)
                  {
                     cpuMemory[i] = ((byte*)m_cpuAlloc) + m_cpuAllocIndex++;
                     *(cpuMemory[i]) = romReader.RomBanks[j];
                  }
               }
               else if (romReader.RomBankCount == 2)
               {
                  for (int i = 0x8000, j = 0; i < 0x10000; i++, j++)
                  {
                     cpuMemory[i] = ((byte*)m_cpuAlloc) + m_cpuAllocIndex++;
                     *(cpuMemory[i]) = romReader.RomBanks[j];
                  }
               }
               else
               {
                  throw new InvalidOperationException(string.Format("Memory mapper is 0, but rom bank count is {0}",
                     romReader.RomBankCount));
               }
            }
            else
            {               
               throw new InvalidOperationException(string.Format("Memory mapper {0} not supported", 
                  romReader.MapperType));
            }

            m_cpu.Memory = new NesCpuBytePointerArray(cpuMemory, (NesPpu)m_ppu);
         }
      }
   }
}
