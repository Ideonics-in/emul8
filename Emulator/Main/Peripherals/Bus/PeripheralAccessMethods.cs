//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//

using System;
using System.Threading;
using Antmicro.Migrant;
using System.Reflection;

namespace Emul8.Peripherals.Bus
{
    internal class PeripheralAccessMethods
    {
        public BusAccess.ByteReadMethod ReadByte;
        public BusAccess.ByteWriteMethod WriteByte;
        public BusAccess.WordReadMethod ReadWord;
        public BusAccess.WordWriteMethod WriteWord;
        public BusAccess.DoubleWordReadMethod ReadDoubleWord;
        public BusAccess.DoubleWordWriteMethod WriteDoubleWord;
        public Action<long> SetAbsoluteAddress;
        public IBusPeripheral Peripheral;
        [Constructor(true)]
        public SpinLock Lock;

        public static PeripheralAccessMethods CreateWithLock()
        {
            return new PeripheralAccessMethods { Lock = new SpinLock(true) }; // TODO: enable or not?
        }

        public void SetMethod(MethodInfo i, object obj, BusAccess.Operation operation, BusAccess.Method method)
        {
            switch(method)
            {
            case BusAccess.Method.Byte:
                SetReadOrWriteMethod(i, obj, operation, ref ReadByte, ref WriteByte);
                break;
            case BusAccess.Method.Word:
                SetReadOrWriteMethod(i, obj, operation, ref ReadWord, ref WriteWord);
                break;
            case BusAccess.Method.DoubleWord:
                SetReadOrWriteMethod(i, obj, operation, ref ReadDoubleWord, ref WriteDoubleWord);
                break;
            default:
                throw new ArgumentException(string.Format("Unsupported access method: {0}", method));
            }
        }

        private static void SetReadOrWriteMethod<TR, TW>(MethodInfo i, object obj, BusAccess.Operation operation, ref TR readMethod, ref TW writeMethod)
        {
            switch(operation)
            {
            case BusAccess.Operation.Read:
                readMethod = (TR)(object)i.CreateDelegate(typeof(TR), obj);
                break;
            case BusAccess.Operation.Write:
                writeMethod = (TW)(object)i.CreateDelegate(typeof(TW), obj);
                break;
            default:
                throw new ArgumentException(string.Format("Unsupported access operation: {0}", operation));
            }
        }
    }
}

