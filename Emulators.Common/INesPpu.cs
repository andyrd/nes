namespace Emulators.Common
{
   public interface INesPpu
   {
      BytePointerArray VideoRam { get; set; }

      BytePointerArray SpriteRam { get; set; }            

      void BeginRun();
   }
}
