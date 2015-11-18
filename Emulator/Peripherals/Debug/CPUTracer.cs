//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;
using Emul8.Peripherals.CPU;
using Emul8.Core;
using Emul8.Logging;
using System.Collections.Generic;
using System.Linq;
using Emul8.Exceptions;
using Emul8.Peripherals.Bus;
using System.Text;

namespace Emul8.Debug
{
    public static class CPUTracerExtensions
    {
        public static void CreateCPUTracer(this Arm cpu, string name)
        {
            EmulationManager.Instance.CurrentEmulation.AddOrUpdateInBag(name, new CPUTracer(cpu));
        }
    }

    public class CPUTracer : IExternal
    {
        public CPUTracer(Arm cpu)
        {
            this.cpu = cpu;
            this.bus = cpu.Bus;
        }

        private void EvaluateTraceCallback(uint pc, string name,  IEnumerable<FunctionCallParameter> parameters, Action<TranslationCPU, uint, string, List<object>> callback)
        {
            var regs = new List<object>();
            var paramList = parameters.ToList();
            //TODO: works only for 0-4 parameters!
            for (int i = 0; i < Math.Min(paramList.Count, 4); i++) 
            {
                regs.Add(TranslateParameter(cpu.R[i], paramList[i]));
            }
            if(paramList.Count > 4)
            {
                var offset = 0;
                var sp = cpu.SP;
                for(int i = 4; i < paramList.Count; ++i)
                {
                    regs.Add(TranslateParameter(cpu.Bus.ReadDoubleWord(sp + offset), paramList[i]));
                    offset += 4; //does not support longer data types!
                }
            }
            callback(cpu, pc, name, regs);
        }

        private object TranslateParameter(uint value, FunctionCallParameter parameter)
        {
            var parameterType = parameter.Type;
            var size = parameter.NumberOfElements;
            switch(parameterType)
            {
            case FunctionCallParameterType.Ignore:
                return null;
            case FunctionCallParameterType.Byte:
                return (byte)value;
            case FunctionCallParameterType.Int32:
                return unchecked((int)value);
            case FunctionCallParameterType.UInt32:
                return value;
            case FunctionCallParameterType.Int16:
                return (short)value;
            case FunctionCallParameterType.UInt16:
                return (ushort)value;
            case FunctionCallParameterType.String:
                var done = false;
                var resultString = new StringBuilder();
                while(!done)
                {
                    var readBytes = bus.ReadBytes(value, SizeOfStringBatch);
                    for(var i = 0; i < readBytes.Length; ++i)
                    {
                        var currentByte = readBytes[i];
                        if(currentByte == 0)
                        {
                            done = true;
                            break;
                        }
                        if(currentByte >= 32 && currentByte < 127)
                        {
                            resultString.Append(Convert.ToChar(currentByte));
                        }
                    }
                    value += SizeOfStringBatch;
                }
                return resultString.ToString();
            case FunctionCallParameterType.ByteArray:
                return bus.ReadBytes(value, size);
            case FunctionCallParameterType.Int32Array:
                var intResult = new int[size];
                Buffer.BlockCopy(bus.ReadBytes(value, size * sizeof(int)), 0, intResult, 0, intResult.Length);
                return intResult;
            case FunctionCallParameterType.UInt32Array:
                var uintResult = new uint[size];
                Buffer.BlockCopy(bus.ReadBytes(value, size * sizeof(uint)), 0, uintResult, 0, uintResult.Length);
                return uintResult;
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        public void TraceFunction(string name, IEnumerable<FunctionCallParameter> parameters, Action<TranslationCPU, uint, string, IEnumerable<object>> callback, 
            FunctionCallParameter? returnParameter = null, Action<TranslationCPU, uint, string, IEnumerable<object>> returnCallback = null)
        {
            cpu.Log(LogLevel.Info, "Going to trace function '{0}'.", name);

            //TODO: Make better after #3024
            uint address;
            Symbol symbol;
            uint? end = null;
            try
            {
                address = cpu.Bus.GetSymbolAddress(name);
                symbol = cpu.Bus.Lookup.GetSymbolByAddress(address);

            }
            catch(RecoverableException)
            {
                cpu.Log(LogLevel.Warning, "Symbol {0} not found, exiting.", name);
                throw;
            }

            cpu.AddHook((uint)symbol.Start, new Action<uint>((pc) => EvaluateTraceCallback(pc, name, parameters, callback)));
            if(returnCallback != null && returnParameter.HasValue)
            {
                end = (uint)(symbol.End - (symbol.IsThumbSymbol ? 2 : 4));
                cpu.Log(LogLevel.Debug, "Address is @ 0x{0:X}, end is @ 0x{1:X}.", address, end);
                cpu.AddHook(end.Value, new Action<uint>((pc) => EvaluateTraceCallback(pc, name, new []{ returnParameter.Value }, returnCallback)));
            }
            else
            {
                cpu.Log(LogLevel.Debug, "Address is @ 0x{0:X}, end is not traced.", address);
            }

            registeredCallbacks[name] = Tuple.Create((uint)symbol.Start, end);
        }

        public void RemoveTracing(string name)
        {
            Tuple<uint, uint?> addresses;
            if(registeredCallbacks.TryGetValue(name, out addresses))
            {
                cpu.Log(LogLevel.Info, "Removing trace from function '{0}'.", name);
                cpu.RemoveHook(addresses.Item1);
                if(addresses.Item2.HasValue)
                {
                    cpu.RemoveHook(addresses.Item2.Value);
                }
            }
            else
            {
                cpu.Log(LogLevel.Warning, "Hook on function {0} not found, not removing.", name);
            }
        }

        private readonly Dictionary<string, Tuple<uint, uint?>> registeredCallbacks = new Dictionary<string, Tuple<uint, uint?>>();
        private readonly Arm cpu;
        private readonly SystemBus bus;

        private const int SizeOfStringBatch = 100;
    }
}