using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emulators.Core;
using Emulators.Common;
using System.Runtime.InteropServices;

namespace Emulators.Test
{
   public delegate bool AdditionalConditionsDelegate();

   class Program
   {
      const ushort StartingAddress = 0x0600;
      static byte[] memory;
      static I6502 mos6502 = new Nmos6502(); 

      static void Main(string[] args)
      {          
         IntPtr p = Marshal.AllocHGlobal(0x10000);
         
         unsafe
         {
            byte*[] init = new byte*[0x10000];

            for (int i = 0; i < init.Length; i++)
            {
               init[i] = ((byte*)p) + i;
               *(init[i]) = 0;
            }

            mos6502.Memory = new BytePointerArray(init);
         }

         LDATest0();
         LDATest1();
         LDATest2();
         LDATest3();
         LDATest4();
         LDATest5();
         LDATest6();
         LDATest7();
         LDATest8();
         LDATest9();

         LDXTest0();

         ADCTest0();
         ADCTest1();
         ADCTest2();
         ADCTest3();

         ANDTest0();

         ASLTest0();
         ASLTest1();

         BITTest0();

         BRKTest0();

         CMPTest0();
         CMPTest1();

         DECTest0();

         EORTest0();

         JMPTest0();

         JSRTest0();

         LSRTest0();

         PHATest0();

         ROLTest0();

         RTITest0();

         RTSTest0();

         SBCTest0();
         SBCTest1();

         DecrementTest();

         Test1();
         Test2();

         Console.WriteLine("press any key...");
         Console.ReadKey(true);

         Marshal.FreeHGlobal(p);
      }

      #region Tests

      #region LDA

      static void LDATest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x80,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest0", 0x603, 255, 128, 0, 0, Convert.ToByte("10100100", 2), 2, null);
      }

      static void LDATest1()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x00,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest1", 0x603, 255, 0, 0, 0, Convert.ToByte("00100110", 2), 2, null);
      }

      static void LDATest2()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA5, 0x50,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x50] = 128;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest2", 0x603, 255, 128, 0, 0, Convert.ToByte("10100100", 2), 3, null);
      }

      static void LDATest3()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA2, 0x0F,
            0xB5, 0x80,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x8F] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest3", 0x605, 255, 50, 0x0F, 0, Convert.ToByte("00100100", 2), 6, null);
      }

      static void LDATest4()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xAD, 0xBB, 0xAA,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0xAABB] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest4", 0x604, 255, 50, 0, 0, Convert.ToByte("00100100", 2), 4, null);
      }

      static void LDATest5()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA2, 0x01,
            0xBD, 0xBB, 0xAA,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0xAABC] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest5", 0x606, 255, 50, 1, 0, Convert.ToByte("00100100", 2), 6, null);
      }

      static void LDATest6()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA2, 0xFF,
            0xBD, 0xBB, 0xAA,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0xABBA] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest6", 0x606, 255, 50, 255, 0, Convert.ToByte("00100100", 2), 7, null);
      }

      static void LDATest7()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA0, 0x01,
            0xB9, 0xBB, 0xAA,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0xAABC] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest7", 0x606, 255, 50, 0, 1, Convert.ToByte("00100100", 2), 6, null);
      }

      static void LDATest8()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA2, 0x05,
            0xA1, 0x3E,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x0043] = 0x15;
         memory[0x0044] = 0x24;
         memory[0x2415] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest8", 0x605, 255, 50, 5, 0, Convert.ToByte("00100100", 2), 8, null);
      }

      static void LDATest9()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA0, 0x05,
            0xB1, 0x4C,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x004C] = 0x00;
         memory[0x004D] = 0x21;
         memory[0x2105] = 50;
         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDATest9", 0x605, 255, 50, 0, 5, Convert.ToByte("00100100", 2), 7, null);
      }

      #endregion

      #region LDX

      static void LDXTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA0, 0x0F,
            0xB6, 0x80,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x8F] = 50;
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LDXTest0", 0x605, 255, 0, 50, 0x0F, Convert.ToByte("00100100", 2), 6, null);
      }

      #endregion

      #region ADC

      static void ADCTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x80,
            0x69, 0xFF,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ADCTest0", 0x605, 255, 127, 0, 0, Convert.ToByte("01100101", 2), 4, null);
      }

      static void ADCTest1()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x80,
            0x69, 0x80,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ADCTest1", 0x605, 255, 0, 0, 0, Convert.ToByte("01100111", 2), 4, null);
      }

      static void ADCTest2()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x7F,
            0x69, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ADCTest2", 0x605, 255, 128, 0, 0, Convert.ToByte("11100100", 2), 4, null);
      }

      static void ADCTest3()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x03,
            0x69, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ADCTest3", 0x605, 255, 4, 0, 0, Convert.ToByte("00100100", 2), 4, null);
      }

      #endregion

      #region AND

      static void ANDTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x85,
            0x29, 0x80,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ANDTest0", 0x605, 255, 0x80, 0, 0, Convert.ToByte("10100100", 2), 4, null);
      }

      #endregion

      #region ASL

      static void ASLTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x85,
            0x0A,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ASLTest0", 0x604, 255, 0x0A, 0, 0, Convert.ToByte("00100101", 2), 4, null);
      }

      static void ASLTest1()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0x06, 0x34,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x34] = 21;
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         AdditionalConditionsDelegate additionalConditions = () =>
         {
            return mos6502.Memory[0x34] == 42;
         };

         ReportTest("ASLTest1", 0x603, 255, 0, 0, 0, Convert.ToByte("00100100", 2), 5, additionalConditions);
      }

      #endregion

      #region BIT

      static void BITTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x87,
            0x24, 0x05,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x05] = 0x75;
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("BITTest0", 0x605, 255, 0x87, 0, 0, Convert.ToByte("01100100", 2), 5, null);
      }

      #endregion

      #region BRK

      static void BRKTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0x58,
            0xA9, 0x80,
            0x69, 0x05,
            0x00            
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x0750] = 0x69;
         memory[0x0751] = 0x01;
         memory[0x0752] = 0xFF;
         memory[0xFFFE] = 0x50;
         memory[0xFFFF] = 0x07;
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         AdditionalConditionsDelegate additionalConditions = () =>
         {
            return mos6502.Memory[0x01FF] == 0x06 &&
                   mos6502.Memory[0x01FE] == 0x06 &&
                   mos6502.Memory[0x01FD] == 0xB0;

         };

         ReportTest("BRKTest0", 0x0753, 0xFC, 0x86, 0, 0, Convert.ToByte("10100100", 2), 15, additionalConditions);
      }

      #endregion

      #region CMP

      static void CMPTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0xF0,
            0xC9, 0x10,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("CMPTest0", 0x605, 255, 0xF0, 0, 0, Convert.ToByte("10100101", 2), 4, null);
      }

      static void CMPTest1()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x10,
            0xC9, 0xF0,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("CMPTest1", 0x605, 255, 0x10, 0, 0, Convert.ToByte("00100100", 2), 4, null);
      }

      #endregion

      #region DEC

      static void DECTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xC6, 0x05,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x05] = 0x00;
         mos6502.Memory.CopyFrom(memory);
         AdditionalConditionsDelegate additionalConditions = () =>
         {
            return mos6502.Memory[0x05] == 0xFF;
         };

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("DECTest0", 0x603, 255, 0, 0, 0, Convert.ToByte("10100100", 2), 5, additionalConditions);
      }

      #endregion

      #region EOR

      static void EORTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x43,
            0x49, 0x34,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("EORTest0", 0x605, 255, 0x77, 0, 0, Convert.ToByte("00100100", 2), 4, null);
      }

      #endregion

      #region JMP

      static void JMPTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x43,
            0x6C, 0x50, 0x70,
            0x69, 0x01,
            0x69, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         memory[0x7050] = 0x07;
         memory[0x7051] = 0x06;
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("JMPTest0", 0x60A, 255, 0x44, 0, 0, Convert.ToByte("00100100", 2), 9, null);
      }

      #endregion

      #region JSR

      static void JSRTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x43,
            0x20, 0x07, 0x06,
            0x69, 0x01,
            0x69, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         AdditionalConditionsDelegate additionalConditions = () =>
         {
            return mos6502.Memory[0x01FF] == 0x04 &&
                   mos6502.Memory[0x01FE] == 0x06;
         };

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("JSRTest0", 0x60A, 0xFD, 0x44, 0, 0, Convert.ToByte("00100100", 2), 10, additionalConditions);
      }

      #endregion

      #region LSR

      static void LSRTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x85,
            0x4A,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("LSRTest0", 0x604, 255, 0x42, 0, 0, Convert.ToByte("00100101", 2), 4, null);
      }

      #endregion

      #region PHA

      static void PHATest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x85,
            0x48,
            0x69, 0x05,
            0x68,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("PHATest0", 0x607, 255, 0x85, 0, 0, Convert.ToByte("10100100", 2), 11, null);
      }

      #endregion

      #region ROL

      static void ROLTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0xFE,
            0x2A,
            0x6A,            
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("ROLTest0", 0x605, 255, 0xFE, 0, 0, Convert.ToByte("10100100", 2), 6, null);
      }

      #endregion

      #region RTI

      static void RTITest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0x58,
            0xA9, 0x20,
            0x69, 0x01,
            0x00,
            0xEA,
            0x69, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);         
         memory[0x0750] = 0x69;
         memory[0x0751] = 0x01;
         memory[0x0752] = 0x40;

         memory[0xFFFE] = 0x50;
         memory[0xFFFF] = 0x07;

         mos6502.Memory.CopyFrom(memory);

         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("RTITest0", 0x060A, 0xFF, 0x23, 0, 0, Convert.ToByte("00100000", 2), 23, null);
      }

      #endregion

      #region RTS

      static void RTSTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x43,
            0x20, 0x08, 0x06,
            0x69, 0x01,
            0xFF,
            0x69, 0x01,
            0x60
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("RTSTest0", 0x608, 0xFF, 0x45, 0, 0, Convert.ToByte("00100100", 2), 18, null);
      }

      #endregion

      #region SBC

      static void SBCTest0()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x00,
            0xE9, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("SBCTest0", 0x605, 255, 0xFE, 0, 0, Convert.ToByte("10100100", 2), 4, null);
      }

      static void SBCTest1()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA9, 0x80,
            0xE9, 0x01,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("SBCTest1", 0x605, 255, 0x7E, 0, 0, Convert.ToByte("01100101", 2), 4, null);
      }

      #endregion

      #region Other

      static void DecrementTest()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA2, 0xFF,
            0xCA,
            0xD0, 0xFD,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("DecrementTest", 0x606, 255, 0, 0, 0, Convert.ToByte("00100110", 2), 1276, null);
      }

      static void Test1()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xE8,
            0x8A,
            0x99, 0x00, 0x02,
            0x99, 0x00, 0x03,
            0x99, 0x00, 0x04,
            0x99, 0x00, 0x05,
            0xC8,
            0x98,
            0xC5, 0x10,
            0xD0, 0x04,
            0xC8,
            0x4C, 0x00, 0x06,
            0xC8,
            0xC8,
            0xC8,
            0xC8,
            0xC9, 0x33,
            0xD0, 0xE0,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("Test1", 0x621, 255, 0x33, 0x0B, 0x37, Convert.ToByte("00100111", 2), 516, null);
      }

      static void Test2()
      {
         memory = new byte[0x10000];

         byte[] instructions = 
         {
            0xA2, 0x07,
            0xA5, 0xFE,
            0x29, 0x03,
            0x69, 0x01,
            0x95, 0x00,
            0xA5, 0xFE,
            0x29, 0x1F,
            0x95, 0x20,
            0xCA,
            0x10, 0xEF,
            0xA9, 0x00,
            0x85, 0x80,
            0xA9, 0x02,
            0x85, 0x81,
            0xA2, 0x07,
            0xB5, 0x20,
            0x48,
            0x18,
            0xF5, 0x00,
            0x29, 0x1F,
            0x95, 0x20,
            0xB5, 0x20,
            0xA8, 
            0xA9, 0x01,
            0x91, 0x80,
            0x68,
            0xA8,
            0xA9, 0x00,
            0x91, 0x80,
            0xA5, 0x80,
            0x18,
            0x69, 0x80,
            0xD0, 0x02,
            0xE6, 0x81,
            0x85, 0x80,
            0xCA,
            0x10, 0xDB,
            0xFF
         };

         instructions.CopyTo(memory, StartingAddress);
         mos6502.Memory.CopyFrom(memory);
         mos6502.LogicalReset();
         mos6502.Run(StartingAddress);

         ReportTest("Test2", 0x643, 255, 0, 0xFF, 0, Convert.ToByte("11100101", 2), 748, null);
      }


      #endregion

      #endregion

      #region Helper Methods

      static void PrintMe(I6502 mos6502)
      {
         Console.WriteLine("ProgramCounter = " + mos6502.ProgramCounter);
         Console.WriteLine("StackPointer   = " + mos6502.StackPointer);
         Console.WriteLine("Accumulator    = " + mos6502.Accumulator);
         Console.WriteLine("IndexX         = " + mos6502.IndexX);
         Console.WriteLine("IndexY         = " + mos6502.IndexY);
         Console.WriteLine("Status         = " + Convert.ToString(mos6502.Status, 2));
         Console.WriteLine("Clock          = " + mos6502.Clock);
         Console.WriteLine();
      }

      static void ReportCondition(string check, int expected, int actual)
      {
         Console.ForegroundColor = (expected == actual) ? ConsoleColor.Green :
            ConsoleColor.Red;

         Console.WriteLine(string.Format("{0}, expected {1}, actual {2}",
            check, expected, actual));

         Console.ForegroundColor = ConsoleColor.Gray;
      }

      static void ReportCondition(string check, bool actual)
      {
         Console.ForegroundColor = actual ? ConsoleColor.Green :
            ConsoleColor.Red;

         Console.WriteLine(check);

         Console.ForegroundColor = ConsoleColor.Gray;
      }

      static void ReportTest(
         string testName, 
         int pc, int sp, int a, int x, int y, int s, int c,
         AdditionalConditionsDelegate additionalConditions)
      {
         bool pcMatch = pc == mos6502.ProgramCounter;
         bool spMatch = sp == mos6502.StackPointer;
         bool aMatch = a == mos6502.Accumulator;
         bool xMatch = x == mos6502.IndexX;
         bool yMatch = y == mos6502.IndexY;
         bool sMatch = s == mos6502.Status;
         bool cMatch = c == mos6502.Clock;
         bool acMatch = (additionalConditions != null ? additionalConditions() : true);

         if (pcMatch && spMatch && aMatch && xMatch && yMatch && sMatch && cMatch)
         {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(testName + " - PASSED");
         }
         else
         {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(testName + " - FAILED");

            ReportCondition("Program Counter", pc, mos6502.ProgramCounter);
            ReportCondition("Stack Pointer", sp, mos6502.StackPointer);
            ReportCondition("Accumulator", a, mos6502.Accumulator);
            ReportCondition("IndexX", x, mos6502.IndexX);
            ReportCondition("IndexY", y, mos6502.IndexY);
            ReportCondition("Status", s, mos6502.Status);
            ReportCondition("Clock", c, mos6502.Clock);
            if (additionalConditions != null)
            {
               ReportCondition("Additional Conditions", acMatch);
            }

         }

         Console.ForegroundColor = ConsoleColor.Gray;
         Console.WriteLine();
      }

      #endregion
   }
}
