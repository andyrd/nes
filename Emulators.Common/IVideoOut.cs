namespace Emulators.Common
{
   public interface INesVideoOut
   {            
      void PlotPixel(int x, int y, int paletteIndex);
      void Invalidate();
   }
}
