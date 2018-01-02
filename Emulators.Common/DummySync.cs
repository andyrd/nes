namespace Emulators.Common
{
   public class DummySync : IDualClockSync
   {
      #region IDualClockSync Members

      public void IncrementClockA(int cc)
      {         
      }

      public void IncrementClockB(int cc)
      {         
      }

      public void WaitForClockB()
      {         
      }

      public void WaitForClockA()
      {         
      }

      #endregion
   }
}
