using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Common.Mos6502
{
   public class Mos6502Instruction
   {
      public byte Instruction { get; set; }
      public ushort MemoryOperand { get; set; }
      public byte LiteralOperand { get; set; }

      public Mos6502Instruction()
      {
      }
   }
}
