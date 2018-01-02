using System.Threading;

namespace Emulators.Common
{   
   /// <summary>
   /// clock A = cpu
   /// clock B = ppu
   /// </summary>
   public class NesClockCycleSync : IDualClockSync
   {
      private int m_clockBalance = 0;
      private ManualResetEvent m_waitEventA = new ManualResetEvent(false);
      private ManualResetEvent m_waitEventB = new ManualResetEvent(false);

      private NesClockCycleSync()
      {
      }

      public NesClockCycleSync(int initialCountA, int initialCountB)
      {
         m_clockBalance = initialCountB - initialCountA * 3;

         if (m_clockBalance <= 0)
         {
            m_waitEventA.Reset();
            m_waitEventB.Set();
         }
         else
         {
            m_waitEventB.Reset();
            m_waitEventA.Set();
         }
      }

      #region IDualClockSync Members

      public void IncrementClockA(int cc)
      {
         m_clockBalance -= cc * 3;

         if (m_clockBalance <= 0)
         {
            m_waitEventA.Reset();
            m_waitEventB.Set();
         }
      }

      public void IncrementClockB(int cc)
      {
         m_clockBalance += cc;         

         if (m_clockBalance >= 0)
         {
            m_waitEventB.Reset();
            m_waitEventA.Set();
         }
      }

      public void WaitForClockB()
      {
         m_waitEventA.WaitOne();
      }

      public void WaitForClockA()
      {
         m_waitEventB.WaitOne();
      }

      #endregion
   }
}
