using System;
using System.Timers;
using System.Windows;
using Emulators.Common;

namespace Emulators.Application.AndyNES
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window
   {
      EmulationShell m_emulationShell;
      INesRomReader m_romReader;

      Timer timer = new Timer(15);

      public MainWindow()
      {
         InitializeComponent();

         m_romReader = new NesRomReader(@"C:\Downloads\emulation\fceux\Super Mario Bros.nes");
         //m_romReader = new NesRomReader(@"D:\Profiles\p57500\Desktop\emu\Super Mario Bros.nes");
         m_emulationShell = new EmulationShell(m_romReader, m_wpfNesVideoOut);        

         //timer.Elapsed += (object sender, ElapsedEventArgs e) =>
         //{
         //   Random r = new Random();

         //   for (int x = 0; x < 256; x++)
         //   {
         //      for (int y = 0; y < 240; y++)
         //      {
         //         m_wpfNesVideoOut.PlotPixel(x, y, r.Next(0, 0x39));
         //      }
         //   }

         //   m_wpfNesVideoOut.Invalidate();
         //};

         //timer.AutoReset = true;
         //timer.Enabled = true;
      }
   }
}
