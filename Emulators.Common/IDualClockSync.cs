using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Emulators.Common
{
   public interface IDualClockSync
   {
      void IncrementClockA(int cc);

      void IncrementClockB(int cc);

      void WaitForClockB();

      void WaitForClockA();     
   }
}
