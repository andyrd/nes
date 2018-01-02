using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Common
{
   //public delegate void BreakDelegate(object sender);
   //void RunDebug(byte[] initialMemory, ushort initialProgramCounter);
   //void Break();
   //void Continue();
   //void Step();
   //void BreakOnCondition(Predicate<IMos6502> breakCondition);
   //event BreakDelegate BreakOnConditionMet;
   //void AddBreakPoint(ushort address);
   //void RemoveBreakPoint(ushort address);
   //event BreakDelegate BreakPointMet;

   public interface I6502
   {      
      ushort ProgramCounter { get; set; }

      byte StackPointer { get; set; }

      byte Accumulator { get; set; }

      byte IndexX { get; set; }

      byte IndexY { get; set; }

      /// <summary>
      /// Bit No. 7   6   5   4   3   2   1   0
      ///         N   V       B   D   I   Z   C
      /// </summary>
      byte Status { get; set; }

      int Clock { get; set; }

      /// <summary>
      /// 0 to 0xFFFF
      /// </summary>
      BytePointerArray Memory { get; set; }

      /// <summary>
      /// Starts from the address specified at FFFC/FFFD (reset Vector)
      /// </summary>
      /// <param name="initialMemory"></param>
      void Run();

      void Run(ushort initialProgramCounter);

      void BeginRun();

      void GenerateReset();

      void GenerateIRQ();

      void GenerateNMI();

      void LogicalReset();
   }
}
