using System;
using Emulators.Common;
using System.Threading;

namespace Emulators.Core
{
   public class Nmos6502 : I6502
   {
      private const ushort StackPage = 0x0100;
      private const ushort NmiVectorAddressLow = 0xFFFA;
      private const ushort ResetVectorAddressLow = 0xFFFC;
      private const ushort IrqVectorAddressLow = 0xFFFE;      

      private readonly byte CarryFlag = 0x01;
      private readonly byte ZeroFlag = 0x02;
      private readonly byte InterruptFlag = 0x04;
      private readonly byte DecimalFlag = 0x08;
      private readonly byte BreakFlag = 0x10;
      private readonly byte NotUsedFlag = 0x20;
      private readonly byte OverflowFlag = 0x40;
      private readonly byte NegativeFlag = 0x80;

      private readonly byte ResetActive = 0x01;
      private readonly byte NmiActive = 0x02;
      private readonly byte IrqActive = 0x04;

      private bool m_exitMainLoop = false;
      private bool m_bcdEnabled = false;
      private byte m_activeInterrupts = 0;
      private IDualClockSync m_sync;
      private Thread m_mainThread;

      #region Status

      private bool CarrySet
      {
         get { return (Status & CarryFlag) != 0; }
         set { if (value) Status |= CarryFlag; else Status &= (byte)~CarryFlag; }
      }

      private bool ZeroSet
      {
         get { return (Status & ZeroFlag) != 0; }
         set { if (value) Status |= ZeroFlag; else Status &= (byte)~ZeroFlag; }
      }

      private bool InterruptSet
      {
         get { return (Status & InterruptFlag) != 0; }
         set { if (value) Status |= InterruptFlag; else Status &= (byte)~InterruptFlag; }
      }

      private bool DecimalSet
      {
         get { return (Status & DecimalFlag) != 0; }
         set { if (value) Status |= DecimalFlag; else Status &= (byte)~DecimalFlag; }
      }

      private bool BreakSet
      {
         get { return (Status & BreakFlag) != 0; }
         set { if (value) Status |= BreakFlag; else Status &= (byte)~BreakFlag; }
      }

      private bool NotUsedSet
      {
         get { return (Status & NotUsedFlag) != 0; }
         set { if (value) Status |= NotUsedFlag; else Status &= (byte)~NotUsedFlag; }
      }

      private bool OverflowSet
      {
         get { return (Status & OverflowFlag) != 0; }
         set { if (value) Status |= OverflowFlag; else Status &= (byte)~OverflowFlag; }
      }

      private bool NegativeSet
      {
         get { return (Status & NegativeFlag) != 0; }
         set { if (value) Status |= NegativeFlag; else Status &= (byte)~NegativeFlag; }
      }

      #endregion

      public Nmos6502()
      {         
         StackPointer = 0xFF;
         Status = (byte)(NotUsedFlag | InterruptFlag);
         m_sync = new DummySync();
      }

      public Nmos6502(IDualClockSync sync)
      {
         StackPointer = 0xFF;
         Status = (byte)(NotUsedFlag | InterruptFlag);
         m_sync = sync;
      }

      #region IMos6502 Members

      public ushort ProgramCounter { get; set; }

      public byte StackPointer { get; set; }

      public byte Accumulator { get; set; }

      public byte IndexX { get; set; }

      public byte IndexY { get; set; }

      public byte Status { get; set; }

      public int Clock 
      {
         get { return m_clock; }
         set
         {
            m_sync.IncrementClockA(value - m_clock); //HACK: better way to do this?
            m_clock = value;
         }
      } private int m_clock;

      public BytePointerArray Memory { get; set; } 

      public void Run()
      {         
         ProgramCounter = (ushort)(Memory[ResetVectorAddressLow] | (Memory[ResetVectorAddressLow + 1] << 8));         
         RunMainLoop();
      }

      public void Run(ushort initialProgramCounter)
      {         
         ProgramCounter = initialProgramCounter;         
         RunMainLoop();
      }

      public void BeginRun()
      {         
         ProgramCounter = (ushort)(Memory[ResetVectorAddressLow] | (Memory[ResetVectorAddressLow + 1] << 8));
         m_mainThread = new Thread(RunMainLoop);
         m_mainThread.Start();
      }

      public void GenerateIRQ()
      {
         m_activeInterrupts |= IrqActive;
      }

      public void GenerateNMI()
      {
         m_activeInterrupts |= NmiActive;
      }

      public void GenerateReset()
      {
         m_activeInterrupts |= ResetActive;
      }

      public void LogicalReset()
      {
         m_exitMainLoop = false;
         ProgramCounter = 0;
         StackPointer = 0xFF;
         Accumulator = 0;
         IndexX = 0;
         IndexY = 0;
         Status = (byte)(NotUsedFlag | InterruptFlag);
         Clock = 0;
      }

      #endregion

      #region Main Loop
      //start from 0x8E19
      private void RunMainLoop()
      {
         byte nextInstruction;     

         while (!m_exitMainLoop)
         {
            m_sync.WaitForClockB();

            CheckForInterrupts();

            nextInstruction = Memory[ProgramCounter++];

            switch (nextInstruction)
            {
               case 0x00: //BRK
               {
                  if (!InterruptSet)
                  {
                     ProgramCounter++;
                     Interrupt(IrqVectorAddressLow, true);
                     Clock += 7;
                  }
               } break;
               case 0x01: //ORA pre-indexed indirect
               {
                  ORA(Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0x05: //ORA zero page
               {
                  ORA(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0x06: //ASL zero page
               {
                  ushort address = ZeroPage();
                  Memory[address] = ASL(Memory[address]);
                  Clock += 5;
               } break;
               case 0x08: //PHP
               {
                  Push(Status);
                  Clock += 3;
               } break;
               case 0x09: //ORA immediate
               {
                  ORA(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0x0A: //ASL accumulator
               {
                  Accumulator = ASL(Accumulator);
                  Clock += 2;
               } break;
               case 0x0D: //ORA absolute
               {
                  ORA(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0x0E: //ASL absolute
               {
                  ushort address = Absolute();
                  Memory[address] = ASL(Memory[address]);
                  Clock += 6;
               } break;
               case 0x10: //BPL
               {
                  byte offset = Memory[Immediate()];
                  if (!NegativeSet)
                  {
                     Branch(offset);
                  }
                  else
                  {
                     Console.WriteLine("rawr!");
                  }
                  Clock += 2;
               } break;
               case 0x11: //ORA post-indexed indirect
               {
                  ORA(Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0x15: //ORA zero page X
               {
                  ORA(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0x16: //ASL zero page X
               {
                  ushort address = ZeroPageX();
                  Memory[address] = ASL(Memory[address]);
                  Clock += 6;
               } break;
               case 0x18: //CLC
               {
                  CarrySet = false;
                  Clock += 2;
               } break;
               case 0x19: //ORA absolute Y
               {
                  ORA(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0x1D: //ORA absolute X
               {
                  ORA(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0x1E: //ASL absolute X
               {
                  ushort address = AbsoluteX();
                  Memory[address] = ASL(Memory[address]);
                  Clock += 7;
               } break;
               case 0x20: //JSR
               {
                  ushort jumpAddress = Absolute();
                  int returnAddress = ProgramCounter - 1;
                  Push((byte)((returnAddress >> 8) & 0xFF));
                  Push((byte)(returnAddress & 0xFF));
                  ProgramCounter = jumpAddress;
                  Clock += 6;
               } break;
               case 0x21: //AND pre-indexed indirect
               {
                  AND(Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0x24: //BIT zero page
               {
                  BIT(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0x25: //AND zero page
               {
                  AND(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0x26: //ROL zero page
               {
                  ushort address = ZeroPage();
                  Memory[address] = ROL(Memory[address]);
                  Clock += 5;
                  break;
               }
               case 0x28: //PLP
               {
                  Status = Pull();
                  Clock += 4;
               } break;
               case 0x29: //AND immediate
               {
                  AND(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0x2A: //ROL accumulator
               {
                  Accumulator = ROL(Accumulator);
                  Clock += 2;
                  break;
               }
               case 0x2C: //BIT absolute
               {
                  BIT(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0x2D: //AND absolute
               {
                  AND(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0x2E: //ROL absolute
               {
                  ushort address = Absolute();
                  Memory[address] = ROL(Memory[address]);
                  Clock += 6;
                  break;
               }
               case 0x30: //BMI
               {
                  byte offset = Memory[Immediate()];
                  if (NegativeSet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;
               } break;
               case 0x31: //AND post-indexed indirect
               {
                  AND(Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0x35: //AND zero page X
               {
                  AND(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0x36: //ROL zero page X
               {
                  ushort address = ZeroPageX();
                  Memory[address] = ROL(Memory[address]);
                  Clock += 6;
                  break;
               }
               case 0x38: //SEC
               {
                  CarrySet = true;
                  Clock += 2;
               } break;
               case 0x39: //AND absolute Y
               {
                  AND(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0x3D: //AND absolute X
               {
                  AND(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0x3E: //ROL absolute X
               {
                  ushort address = AbsoluteX();
                  Memory[address] = ROL(Memory[address]);
                  Clock += 7;
                  break;
               }
               case 0x40: //RTI
               {
                  Status = Pull();
                  Status &= (byte)~BreakFlag;
                  ProgramCounter = (ushort)(Pull() | (Pull() << 8));
                  Clock += 6;
               } break;
               case 0x41: //EOR pre-indexed indirect
               {
                  EOR(Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0x45: //EOR zero page
               {
                  EOR(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0x46: //LSR zero page
               {
                  ushort address = ZeroPage();
                  Memory[address] = LSR(Memory[address]);
                  Clock += 5;
               } break;
               case 0x48: //PHA
               {
                  Push(Accumulator);
                  Clock += 3;
               } break;
               case 0x49: //EOR immediate
               {
                  EOR(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0x4A: //LSR accumulator
               {
                  Accumulator = LSR(Accumulator);
                  Clock += 2;
               } break;
               case 0x4C: //JMP absolute
               {
                  ProgramCounter = Memory[Absolute()];
                  Clock += 3;
               } break;
               case 0x4D: //EOR absolute
               {
                  EOR(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0x4E: //LSR absolute
               {
                  ushort address = Absolute();
                  Memory[address] = LSR(Memory[address]);
                  Clock += 6;
               } break;
               case 0x50: //BVC
               {
                  byte offset = Memory[Immediate()];
                  if (!OverflowSet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;  
               } break;
               case 0x51: //EOR post-indexed indirect
               {
                  EOR(Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0x55: //EOR zero page X
               {
                  EOR(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0x56: //LSR zero page X
               {
                  ushort address = ZeroPageX();
                  Memory[address] = LSR(Memory[address]);
                  Clock += 6;
               } break;
               case 0x58: //CLI
               {
                  InterruptSet = false;
                  Clock += 2;
               } break;
               case 0x59: //EOR absolute Y
               {
                  EOR(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 3;
               } break;
               case 0x5D: //EOR absolute X
               {
                  EOR(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 3;
               } break;
               case 0x5E: //LSR absolute X
               {
                  ushort address = AbsoluteX();
                  Memory[address] = LSR(Memory[address]);
                  Clock += 7;
               } break;
               case 0x60: //RTS
               {
                  ProgramCounter = (ushort)(Pull() | (Pull() << 8));
                  ProgramCounter++;
                  Clock += 6;
               } break;
               case 0x61: //ADC pre-indexed indirect
               {
                  ADC(Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0x65: //ADC zero page
               {
                  ADC(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0x66: //ROR zero page
               {
                  ushort address = ZeroPage();
                  Memory[address] = ROR(Memory[address]);
                  Clock += 5;
                  break;
               }
               case 0x68: //PLA
               {
                  Accumulator = Pull();
                  ZeroSet = Accumulator == 0;
                  NegativeSet = (Accumulator & 0x80) == 0x80;
                  Clock += 4;
               } break;
               case 0x69: //ADC immediate                    
               {
                  ADC(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0x6A: //ROR accumulator
               {
                  Accumulator = ROR(Accumulator);
                  Clock += 2;
               } break;
               case 0x6C: //JMP indirect
               {
                  ProgramCounter = Indirect();
                  Clock += 5;
               } break;
               case 0x6D: //ADC absolute
               {
                  ADC(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0x6E: //ROR absolute
               {
                  ushort address = Absolute();
                  Memory[address] = ROR(Memory[address]);
                  Clock += 6;
                  break;
               }
               case 0x70: //BVS
               {
                  byte offset = Memory[Immediate()];
                  if (OverflowSet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;  
               } break;
               case 0x71: //ADC post-indexed indirect
               {
                  ADC(Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0x75: //ADC zero page X
               {
                  ADC(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0x76: //ROR zero page X
               {
                  ushort address = ZeroPageX();
                  Memory[address] = ROR(Memory[address]);
                  Clock += 6;
                  break;
               }
               case 0x78: //SEI
               {
                  InterruptSet = true;
                  Clock += 2;
               } break;
               case 0x79: //ADC absolute Y
               {
                  ADC(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0x7D: //ADC absolute X
               {
                  ADC(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0x7E: //ROR absolute X
               {
                  ushort address = AbsoluteX();
                  Memory[address] = ROR(Memory[address]);
                  Clock += 7;
                  break;
               }
               case 0x81: //STA pre-indexed indirect
               {
                  Memory[PreIndexedIndirect()] = Accumulator;
                  Clock += 6;
               } break;
               case 0x84: //STY zero page
               {
                  Memory[ZeroPage()] = IndexY;
                  Clock += 3;
               } break;
               case 0x85: //STA zero page
               {
                  Memory[ZeroPage()] = Accumulator;
                  Clock += 3;
               } break;
               case 0x86: //STX zero page
               {
                  Memory[ZeroPage()] = IndexX;
                  Clock += 3;
               } break;
               case 0x88: //DEY
               {                  
                  IndexY = Decrement(IndexY);
                  Clock += 2;
               } break;
               case 0x8A: //TXA
               {
                  Accumulator = IndexX;
                  ZeroSet = Accumulator == 0;
                  NegativeSet = (Accumulator & 0x80) == 0x80;
                  Clock += 2;
               } break;
               case 0x8C: //STY absolute
               {
                  Memory[Absolute()] = IndexY;
                  Clock += 4;
               } break;
               case 0x8D: //STA absolute
               {
                  Memory[Absolute()] = Accumulator;
                  Clock += 4;
               } break;
               case 0x8E: //STX absolute
               {
                  Memory[Absolute()] = IndexX;
                  Clock += 4;
               } break;
               case 0x90: //BCC
               {
                  byte offset = Memory[Immediate()];
                  if (!CarrySet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;
               } break;
               case 0x91: //STA post-indexed indirect
               {
                  Memory[PostIndexedIndirect()] = Accumulator;
                  Clock += 6;
               } break;
               case 0x94: //STY zero page X
               {
                  Memory[ZeroPageX()] = IndexY;
                  Clock += 4;
               } break;
               case 0x95: //STA zero page X
               {
                  Memory[ZeroPageX()] = Accumulator;
                  Clock += 4;
               } break;
               case 0x96: //STX zero page Y
               {
                  Memory[ZeroPageY()] = IndexX;
                  Clock += 4;
               } break;
               case 0x98: //TYA
               {
                  Accumulator = IndexY;
                  ZeroSet = Accumulator == 0;
                  NegativeSet = (Accumulator & 0x80) == 0x80;
                  Clock += 2;
               } break;
               case 0x99: //STA absolute Y
               {
                  Memory[AbsoluteY()] = Accumulator;
                  Clock += 5;
               } break;
               case 0x9A: //TXS
               {
                  StackPointer = IndexX;
                  Clock += 2;
               } break;
               case 0x9D: //STA absolute X
               {
                  Memory[AbsoluteX()] = Accumulator;
                  Clock += 4;
               } break;
               case 0xA0: //LDY immediate
               {
                  LDY(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xA1: //LDA pre-indexed indirect
               {
                  LDA(Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0xA2: //LDX immediate                  
               {
                  LDX(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xA4: //LDY zero page
               {
                  LDY(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xA5: //LDA zero page                  
               {
                  LDA(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xA6: //LDX zero page
               {
                  LDX(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xA8: //TAY
               {
                  IndexY = Accumulator;
                  ZeroSet = IndexY == 0;
                  NegativeSet = (IndexY & 0x80) == 0x80;
                  Clock += 2;
               } break;
               case 0xA9: //LDA immediate                  
               {
                  LDA(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xAA: //TAX
               {
                  IndexX = Accumulator;
                  ZeroSet = IndexX == 0;
                  NegativeSet = (IndexX & 0x80) == 0x80;
                  Clock += 2;
               } break;
               case 0xAC: //LDY absolute
               {
                  LDY(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xAD: //LDA absolute
               {
                  LDA(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xAE: //LDX absolute
               {
                  LDX(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xB0: //BCS
               {
                  byte offset = Memory[Immediate()];
                  if (CarrySet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;               
               } break;
               case 0xB1: //LDA post-indexed indirect
               {
                  LDA(Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0xB4: //LDY zero page X
               {
                  LDY(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0xB5: //LDA zero page X                  
               {
                  LDA(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0xB6: //LDX zero page Y
               {
                  LDX(Memory[ZeroPageY()]);
                  Clock += 4;
               } break;
               case 0xB8: //CLV
               {
                  OverflowSet = false;
                  Clock += 2;
               } break;
               case 0xB9: //LDA absolute Y
               {
                  LDA(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xBA: //TSX
               {
                  IndexX = StackPointer;
                  ZeroSet = IndexX == 0;
                  NegativeSet = (IndexX & 0x80) == 0x80;
                  Clock += 2;
               } break;
               case 0xBC: //LDY absolute X
               {
                  LDY(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xBD: //LDA absolute X
               {
                  LDA(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xBE: //LDX absolute Y
               {
                  LDX(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;               
               case 0xC0: //CPY immediate
               {
                  Compare(IndexY, Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xC1: //CMP pre-indexed indirect
               {
                  Compare(Accumulator, Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0xC4: //CPY zero page
               {
                  Compare(IndexY, Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xC5: //CMP zero page
               {
                  Compare(Accumulator, Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xC6: //DEC zero page
               {
                  ushort address = ZeroPage();
                  Memory[address] = Decrement(Memory[address]);
                  Clock += 5;
               } break;
               case 0xC8: //INY
               {
                  IndexY = Increment(IndexY);
                  Clock += 2;
               } break;
               case 0xC9: //CMP immediate
               {
                  Compare(Accumulator, Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xCA: //DEX
               {
                  IndexX = Decrement(IndexX);
                  Clock += 2;
               } break;
               case 0xCC: //CPY absolute
               {
                  Compare(IndexY, Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xCD: //CMP absolute
               {
                  Compare(Accumulator, Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xCE: //DEC absolute
               {
                  ushort address = Absolute();
                  Memory[address] = Decrement(Memory[address]);
                  Clock += 6;
               } break;
               case 0xD0: //BNE                  
               {
                  byte offset = Memory[Immediate()];
                  if (!ZeroSet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;
               } break;
               case 0xD1: //CMP post-indexed indirect
               {
                  Compare(Accumulator, Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0xD5: //CMP zero page X
               {
                  Compare(Accumulator, Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0xD6: //DEC zero page X
               {
                  ushort address = ZeroPageX();
                  Memory[address] = Decrement(Memory[address]);
                  Clock += 5;
               } break;
               case 0xD8: //CLD
               {
                  DecimalSet = false;
                  Clock += 2;
               } break;
               case 0xD9: //CMP absolute Y
               {
                  Compare(Accumulator, Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xDD: //CMP absolute X
               {
                  Compare(Accumulator, Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xDE: // DEC absolute X
               {
                  ushort address = AbsoluteX();
                  Memory[address] = Decrement(Memory[address]);
                  Clock += 7;
               } break;
               case 0xE0: //CPX immediate
               {
                  Compare(IndexX, Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xE1: //SBC pre-indexed indirect
               {
                  SBC(Memory[PreIndexedIndirect()]);
                  Clock += 6;
               } break;
               case 0xE4: //CPX zero page
               {
                  Compare(IndexX, Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xE5: //SBC zero page
               {
                  SBC(Memory[ZeroPage()]);
                  Clock += 3;
               } break;
               case 0xE6: //INC zero page
               {
                  ushort address = ZeroPage();
                  Memory[address] = Increment(Memory[address]);
                  Clock += 5;
               } break;
               case 0xE8: //INX
               {
                  IndexX = Increment(IndexX);
                  Clock += 2;
               } break;
               case 0xE9: //SBC immediate
               {
                  SBC(Memory[Immediate()]);
                  Clock += 2;
               } break;
               case 0xEA: //NOP
               {
                  Clock += 2;
               } break;
               case 0xEC: //CPX absolute
               {
                  Compare(IndexX, Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xED: //SBC absolute
               {
                  SBC(Memory[Absolute()]);
                  Clock += 4;
               } break;
               case 0xEE: //INC absolute
               {
                  ushort address = Absolute();
                  Memory[address] = Increment(Memory[address]);
                  Clock += 6;
               } break;
               case 0xF0: //BEQ
               {
                  byte offset = Memory[Immediate()];
                  if (ZeroSet)
                  {
                     Branch(offset);
                  }
                  Clock += 2;
               } break;
               case 0xF1: //SBC post-indexed indirect
               {
                  SBC(Memory[PostIndexedIndirectWithBoundaryCheck()]);
                  Clock += 5;
               } break;
               case 0xF5: //SBC zero page X
               {
                  SBC(Memory[ZeroPageX()]);
                  Clock += 4;
               } break;
               case 0xF6: //INC zero page X
               {
                  ushort address = ZeroPageX();
                  Memory[address] = Increment(Memory[address]);
                  Clock += 6;
               } break;
               case 0xF8: //SED
               {
                  DecimalSet = true;
                  Clock += 2;
               } break;
               case 0xF9: //SBC absolute Y
               {
                  SBC(Memory[AbsoluteYWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xFD: //SBC absolute X
               {
                  SBC(Memory[AbsoluteXWithBoundaryCheck()]);
                  Clock += 4;
               } break;
               case 0xFE: //INC absolute X
               {
                  ushort address = AbsoluteX();
                  Memory[address] = Increment(Memory[address]);
                  Clock += 7;
               } break;
               case 0xFF:
               {
                  m_exitMainLoop = true;
               } break;
               default:
               {
                  throw new InvalidOperationException(
                     string.Format("instruction {0} not supported",
                        Convert.ToString(nextInstruction, 16)));                                    
               } 
            }            
         }
      }

      #endregion

      #region Instruction Helpers

      private void ADC(byte b)
      {
         if (m_bcdEnabled && DecimalSet)
         {
            Console.WriteLine("TODO: BCD");
         }
         else
         {
            uint result = (uint)(b + Accumulator + (CarrySet ? 1 : 0));
            ZeroSet = (byte)(result & 0xFF) == 0;
            NegativeSet = (result & 0x80) == 0x80;
            CarrySet = result > 0xFF;
            OverflowSet = 
               !(((Accumulator ^ b) & 0x80) != 0) && 
                (((Accumulator ^ result) & 0x80) != 0);
            Accumulator = (byte)result;
         }
      }

      private void AND(byte b)
      {
         int result = b & Accumulator;
         ZeroSet = result == 0;
         NegativeSet = (result & 0x80) == 0x80;
         Accumulator = (byte)result;
      }

      private byte ASL(byte b)
      {
         CarrySet = (b & 0x80) != 0;
         b <<= 1;
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;

         return b;
      }

      private void Branch(byte b)
      {                  
         ushort branchAddress = (ushort)(ProgramCounter + (sbyte)b);
         Clock += ((ProgramCounter & 0xFF00) == (branchAddress & 0xFF00)) ? 1 : 2;
         ProgramCounter = branchAddress;         
      }

      private void Interrupt(ushort vectorAddressLow, bool brk)
      {
         Push((byte)((ProgramCounter >> 8) & 0xFF));
         Push((byte)(ProgramCounter & 0xFF));

         byte statusToSave = Status;
         if (brk)
         {
            statusToSave |= BreakFlag;
         }

         Push(statusToSave);
         InterruptSet = true; //TODO: is this set for a NMI?
         ProgramCounter = (ushort)(Memory[vectorAddressLow] | (Memory[vectorAddressLow + 1] << 8));         
      }

      private void BIT(byte b)
      {
         NegativeSet = (b & 0x80) == 0x80;
         OverflowSet = (b & 0x40) != 0;
         ZeroSet = (b & Accumulator) == 0;
      }

      private void Compare(byte r, byte m)
      {
         int delta = r - m;
         NegativeSet = (delta & 0x80) == 0x80;
         CarrySet = r >= m;
         ZeroSet = (delta & 0xFF) == 0;
      }

      private byte Decrement(byte b)
      {
         int result = (b - 1) & 0xFF;
         NegativeSet = (result & 0x80) == 0x80;
         ZeroSet = result == 0;

         return (byte)result;
      }

      private void EOR(byte b)
      {
         int result = b ^ Accumulator;
         NegativeSet = (result & 0x80) == 0x80;
         ZeroSet = result == 0;
         Accumulator = (byte)result;
      }

      private byte Increment(byte b)
      {
         int result = (b + 1) & 0xFF;
         NegativeSet = (result & 0x80) == 0x80;
         ZeroSet = result == 0;

         return (byte)result;
      }

      private void LDA(byte b)
      {
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;
         Accumulator = b;
      }

      private void LDX(byte b)
      {
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;
         IndexX = b;
      }

      private void LDY(byte b)
      {
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;
         IndexY = b;
      }

      private byte LSR(byte b)
      {
         CarrySet = (b & 0x01) != 0;
         b >>= 1;
         NegativeSet = false;
         ZeroSet = b == 0;

         return b;
      }

      private void ORA(byte b)
      {
         int result = b | Accumulator;
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;
         Accumulator = (byte)result;
      }

      private byte ROL(byte b)
      {
         int result = b << 1;
         if (CarrySet)
         {
            result |= 0x1;
         }
         CarrySet = result > 0xFF;
         result &= 0xFF;
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;

         return (byte)result;
      }

      private byte ROR(byte b)
      {
         int result = b;
         if (CarrySet)
         {
            result |= 0x100;
         }
         CarrySet = (result & 0x01) != 0;
         result >>= 1;
         NegativeSet = (b & 0x80) == 0x80;
         ZeroSet = b == 0;

         return (byte)result;
      }

      private void SBC(byte b)
      {
         uint result = (uint)(Accumulator - b - (CarrySet ? 0 : 1));
         NegativeSet = (result & 0x80) == 0x80;
         ZeroSet = (result & 0xFF) == 0;
         OverflowSet = (((Accumulator ^ result) & 0x80) != 0) &&
                       (((Accumulator ^ b) & 0x80) != 0);

         if (m_bcdEnabled && DecimalSet)
         {
            Console.WriteLine("TODO: BCD");
         }

         CarrySet = result < 0x100;
         Accumulator = (byte)(result & 0xFF);
      }

      private void CheckForInterrupts()
      {
         if (m_activeInterrupts > 0)
         {
            if ((m_activeInterrupts & ResetActive) > 0)
            {
               Interrupt(ResetVectorAddressLow, false);
               m_activeInterrupts &= (byte)~ResetActive;
            }
            else if ((m_activeInterrupts & NmiActive) > 0)
            {
               Interrupt(NmiVectorAddressLow, false);
               Clock += 7;
               m_activeInterrupts &= (byte)~NmiActive;
            }
            else if ((m_activeInterrupts & IrqActive) > 0)
            {
               if (!InterruptSet)
               {
                  Interrupt(IrqVectorAddressLow, false);
                  Clock += 7;
               }

               m_activeInterrupts &= (byte)~IrqActive;
            }
         }         
      }

      #endregion

      #region Addressing Helpers

      ushort Immediate()
      {         
         ushort result = ProgramCounter++;
         return result;
      }

      ushort ZeroPage()
      {
         ushort result = (ushort)(Memory[ProgramCounter++] & 0xFF);
         return result;
      }

      ushort ZeroPageX()
      {
         ushort result = (ushort)((Memory[ProgramCounter++] + IndexX) & 0xFF);
         return result;
      }

      ushort ZeroPageY()
      {
         ushort result = (ushort)((Memory[ProgramCounter++] + IndexY) & 0xFF);
         return result;
      }

      ushort Absolute()
      {
         ushort result = (ushort)(Memory[ProgramCounter++] | (Memory[ProgramCounter++] << 8));
         return result;
      }

      ushort AbsoluteX()
      {
         int address = Memory[ProgramCounter++] | (Memory[ProgramCounter++] << 8);
         int addressWithOffset = address + IndexX;

         return (ushort)addressWithOffset;
      }

      ushort AbsoluteXWithBoundaryCheck()
      {
         int address = Memory[ProgramCounter++] | (Memory[ProgramCounter++] << 8);
         int addressWithOffset = address + IndexX;
         if ((address & 0xFF00) != (addressWithOffset & 0xFF00))
         {
            Clock++;
         }

         return (ushort)addressWithOffset;
      }

      ushort AbsoluteY()
      {
         int address = Memory[ProgramCounter++] | (Memory[ProgramCounter++] << 8);
         int addressWithOffset = address + IndexY;

         return (ushort)addressWithOffset;
      }

      ushort AbsoluteYWithBoundaryCheck()
      {
         int address = Memory[ProgramCounter++] | (Memory[ProgramCounter++] << 8);
         int addressWithOffset = address + IndexY;
         if ((address & 0xFF00) != (addressWithOffset & 0xFF00))
         {
            Clock++;
         }

         return (ushort)addressWithOffset;
      }

      ushort Indirect()
      {         
         int address = Memory[ProgramCounter++] | (Memory[ProgramCounter++] << 8);
         ushort result = (ushort)(Memory[address] | (Memory[address + 1] << 8));
         return result;
      }

      ushort PreIndexedIndirect()
      {
         int zeroPageAddress = (Memory[ProgramCounter++] + IndexX) & 0xFF;
         int address = Memory[zeroPageAddress] | (Memory[zeroPageAddress+1] << 8);

         return (ushort)address;
      }

      ushort PostIndexedIndirect()
      {
         int zeroPageAddress =  Memory[ProgramCounter++] & 0xFF;
         int address = Memory[zeroPageAddress] | (Memory[zeroPageAddress + 1] << 8);
         int addressWithOffset = address + IndexY;

         return (ushort)addressWithOffset;
      }

      ushort PostIndexedIndirectWithBoundaryCheck()
      {
         int zeroPageAddress = Memory[ProgramCounter++] & 0xFF;
         int address = Memory[zeroPageAddress] | (Memory[zeroPageAddress + 1] << 8);
         int addressWithOffset = address + IndexY;

         if ((address & 0xFF00) != (addressWithOffset & 0xFF00))
         {
            Clock++;
         }

         return (ushort)addressWithOffset;
      }

      #endregion

      #region Stack Helpers

      private void Push(byte b)
      {
         Memory[StackPage | StackPointer--] = b;
      }

      private byte Pull()
      {
         return Memory[StackPage | ++StackPointer];
      }

      #endregion    
   }
}
