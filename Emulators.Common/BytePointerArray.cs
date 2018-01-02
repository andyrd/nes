using System.Collections.Generic;
using System.Linq;

namespace Emulators.Common
{  
   public class BytePointerArray
   {
      unsafe private byte*[] m_array;

      private BytePointerArray()
      {
      }

      unsafe public BytePointerArray(byte*[] initalValue)
      {
         m_array = initalValue;
      }      

      public virtual byte this[int index]
      {
         get { unsafe { return *(m_array[index]); } }         
         set { unsafe { *(m_array[index]) = value; } }
      }

      public int Length
      {
         get { unsafe { return m_array.Length; } }
      }

      public void CopyFrom(byte[] array)
      {         
         for (int i = 0; i < Length; i++)
         {
            this[i] = array[i];
         }
      }
   }
}
