//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using NUnit.Framework;
using Emul8.UserInterface;
using System.Diagnostics;
using Emul8.Logging;
using System.Linq;
using Emul8.Core;
using Emul8.Peripherals.Bus;
using Emul8.Peripherals;
using System.Text;
using System.IO;
using System.Collections;
using Emul8.Utilities;
using System.Text.RegularExpressions;
using Emul8.Peripherals.Memory;

namespace MonitorTests.CommandTests
{
    [TestFixture]
    public class PythonCommands
    {
        [Test]
        public void NextValueTest()
        {
            monitor.Parse("next_value", commandEater);
            var value = int.Parse(commandEater.GetContents().TrimEnd('\r', '\n'));
            commandEater.Clear();
            monitor.Parse("next_value", commandEater);
            var next_value = int.Parse(commandEater.GetContents().TrimEnd('\r', '\n'));
            Assert.AreEqual(1, next_value - value);
        }

        [Test]
        public void SleepTest()
        {
            var stopwatch = new Stopwatch();
            //to ensure early loading of python commands
            monitor.Parse("sleep 1", commandEater);
            stopwatch.Start();
            monitor.Parse("sleep 5", commandEater);
            stopwatch.Stop();
            Assert.GreaterOrEqual(stopwatch.Elapsed.Seconds, 4);
            Assert.LessOrEqual(stopwatch.Elapsed.Seconds, 6);
        }

        [Test]
        public void EchoTest()
        {
            var text = "test text with stuff"; //it wont accept \n
            monitor.Parse(String.Format("echo \"{0}\"", text), commandEater);
            Assert.AreEqual(text, commandEater.GetContents().TrimEnd('\r', '\n'));
        }

        [Test]
        public void DumpTest()
        {

            BuildEmulation();
            const string message = "MAGIC MESSAGE";
            const uint address = MemoryOffset;
            var bytes = Encoding.ASCII.GetBytes(message);
            machine.SystemBus.ZeroRange(new Range(MemoryOffset, MemoryOffset));
            machine.SystemBus.WriteBytes(bytes, address);
            monitor.Parse(String.Format("dump {0} {1}", address, bytes.Length), commandEater);
            var result = commandEater.GetContents();
            var splitResult = result.Split(new[]{ '|' }, StringSplitOptions.RemoveEmptyEntries);
            var index = -1;
            var bytesIndex = 0;
            foreach(var resultElement in splitResult)
            {
                index = (index + 1) % 3;
                if(index == 0 || index == 2)
                {
                    continue;
                }
                //we're in correct element of the output
                var splitBytes = resultElement.Split(new[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var resultByte in splitBytes.Select(x=>int.Parse(x,System.Globalization.NumberStyles.HexNumber)))
                {
                    if(bytesIndex >= bytes.Length)
                    {
                        Assert.AreEqual(0, resultByte);
                    }
                    else
                    {
                        Assert.AreEqual(bytes[bytesIndex], resultByte);
                    }
                    bytesIndex++;
                }
            }
            Assert.GreaterOrEqual(bytesIndex, bytes.Length);

        }

        [Test]
        public void DumpFileTest()
        {
            BuildEmulation();
            const string message = "MAGIC MESSAGE";
            const uint address = MemoryOffset;
            var bytes = Encoding.ASCII.GetBytes(message);
            machine.SystemBus.WriteBytes(bytes, address);
            var file = TemporaryFilesManager.Instance.GetTemporaryFile();
            try
            {
                monitor.Parse(String.Format("dump_file {0} {1} @{2}", address, bytes.Length, file), commandEater);
                using(var tmpFile = new StreamReader(file))
                {
                    var value = tmpFile.ReadToEnd();
                    Assert.AreEqual(message, value);
                }
            }
            catch(Exception e)
            {
                Assert.Fail(e.Message);
            }
            finally
            {
                File.Delete(file);
            }
        }

        private void BuildEmulation()
        {
            monitor.Parse("mach create", commandEater);
            machine = EmulationManager.Instance.CurrentEmulation.Machines.First() as Machine;
            machine.SystemBus.Register(new MappedMemory(0x1000), new BusPointRegistration(MemoryOffset));
        }

        [TestFixtureSetUp]
        public void SetUp()
        {
            monitor = new Monitor();
            commandEater = new CommandInteractionEater();
            loggerBackend = new DummyLoggerBackend();
            Logger.AddBackend(loggerBackend, "dummy");
        }

        [TearDown]
        public void TearDown()
        {
            EmulationManager.Instance.Clear();
            commandEater.Clear();
            loggerBackend.Clear();
        }

        private const uint MemoryOffset = 0x1000;
        private CommandInteractionEater commandEater;
        private Monitor monitor;
        private Machine machine;
        private DummyLoggerBackend loggerBackend;
    }
}

