//
// Copyright (c) Antmicro
// Copyright (c) Realtime Embedded
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
using System;

namespace Emul8.Core
{
    public struct Range
    {
        public static bool TryCreate(long startAddress, long size, out Range range)
        {
            range = default(Range);
            if(size < 0)
            {
                return false;
            }
            range.StartAddress = startAddress;
            range.EndAddress = startAddress + size;
            return true;
        }

        public Range(long startAddress, long size):this()
        {
            if(!TryCreate(startAddress, size, out this))
            {
                throw new ArgumentException("Size has to be positive or zero.", "size");
            }
        }
     
        public bool Contains(long address)
        {
            return address >= StartAddress && address < EndAddress;
        }
    
        public bool Contains(Range range)
        {
            return range.StartAddress >= StartAddress && range.EndAddress <= EndAddress;
        }
     
        public Range Intersect(Range range)
        {
            var startAddress = Math.Max(StartAddress, range.StartAddress);
            var endAddress = Math.Min(EndAddress, range.EndAddress);
            if(startAddress >= endAddress)
            {
                return Range.Empty;
            }
            return new Range(startAddress, endAddress - startAddress);
        }

        public bool Intersects(Range range)
        {
            return Intersect(range) != Range.Empty;
        }
     
        public long StartAddress
        {
            get;
            private set;
        }
     
        public long EndAddress
        {
            get;
            private set;
        }
     
        public long Size
        {
            get
            {
                return EndAddress - StartAddress;
            }
        }

        public Range ShiftBy(long shiftValue)
        {
            return new Range(StartAddress + shiftValue, Size);
        }
     
        public Range MoveToZero()
        {
            return new Range(0, Size);
        }
     
        public override string ToString()
        {
            return string.Format("<0x{0:X8}, 0x{1:X8}>", StartAddress, EndAddress - 1);
        }
     
        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }
            if(obj.GetType() != typeof(Range))
            {
                return false;
            }
            var other = (Range)obj;
            return this == other;
        }
     
        public override int GetHashCode()
        {
            unchecked
            {
                return 7 * StartAddress.GetHashCode() ^ 31 * EndAddress.GetHashCode();
            }
        }

        public static bool operator==(Range range, Range other)
        {
            return range.StartAddress == other.StartAddress && range.EndAddress == other.EndAddress;
        }

        public static bool operator!=(Range range, Range other)
        {
            return !(range == other);
        }

        public static Range operator+(Range range, long addend)
        {
            return range.ShiftBy(addend);
        }

        public static Range operator-(Range range, long minuend)
        {
            return range.ShiftBy(-minuend);
        }

        public static Range Empty;
    }

    public static class RangeExtensions
    {
        public static Range By(this long startAddress, long size)
        {
            return new Range(startAddress, size);
        }

        public static Range By(this int startAddress, long size)
        {
            return new Range(startAddress, size);
        }

        public static Range By(this uint startAddress, long size)
        {
            return new Range(startAddress, size);
        }

        public static Range To(this int startAddress, long endAddress)
        {
            return new Range(startAddress, endAddress - startAddress);
        }

        public static Range To(this long startAddress, long endAddress)
        {
            return new Range(startAddress, endAddress - startAddress);
        }
    }
}
