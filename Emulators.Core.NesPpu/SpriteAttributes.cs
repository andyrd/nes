using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Emulators.Common;

namespace Emulators.Core
{
   enum BackgroundPriority
   {
      InFront = 0,
      Behind = 1
   }

   class SpriteAttributes
   {
      public byte YCoordiate { get; set; }
      public byte TileIndex { get; set; }
      public int Color { get; set; }
      public BackgroundPriority Priority { get; set; }
      public bool HorizontalFlip { get; set; }
      public bool VerticalFlip { get; set; }
      public byte XCoordiate { get; set; }
      public int SpriteIndex { get; set; }

      public SpriteAttributes()
      {
      }

      public void ParseSpriteRam(BytePointerArray spriteRam, int index)
      {
         YCoordiate = spriteRam[index];
         TileIndex = spriteRam[index + 1];
         Color = spriteRam[index + 2] & 0x03;
         Priority = (BackgroundPriority)((spriteRam[index + 2] >> 5) & 0x01);
         HorizontalFlip = ((spriteRam[index + 2] >> 6) & 0x01) == 1;
         VerticalFlip = ((spriteRam[index + 2] >> 7) & 0x01) == 1;
         XCoordiate = spriteRam[index + 3];
         SpriteIndex = index / 4;
      }
   }
}
