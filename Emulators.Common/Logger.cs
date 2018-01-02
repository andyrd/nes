using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Emulators.Common
{
   public static class Logger
   {
      private static object s_syncObject = new object();

      public static void Write(string s)
      {
         lock (s_syncObject)
         {
            using (StreamWriter sw = new StreamWriter(@"Emulators.Logger.txt"))
            {
               sw.WriteLine(s);
            }
         }
      }
   }
}
