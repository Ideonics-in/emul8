﻿//
// Copyright (c) Antmicro
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//

using System;
using System.Collections.Generic;
using Emul8.Backends.Display;

namespace Emul8.Peripherals.DMA
{
    // ordering of entry is taken from the documentation and should not be altered!
    internal enum Dma2DColorMode
    {
        ARGB8888,
        RGB888,
        RGB565,
        ARGB1555,
        ARGB4444,
        L8,
        AL44,
        AL88,
        L4,
        A8,
        A4
    }

    internal static class Dma2DColorModeExtensions
    {
        static Dma2DColorModeExtensions()
        {
            cache = new Dictionary<Dma2DColorMode, PixelFormat>();
            foreach(Dma2DColorMode mode in Enum.GetValues(typeof(Dma2DColorMode)))
            {
                PixelFormat format;
                if(!Enum.TryParse(mode.ToString(), out format))
                {
                    throw new ArgumentException(string.Format("Could not find pixel format matching DMA2D color mode: {0}", mode));
                }

                cache[mode] = format;
            }
        }

        public static PixelFormat ToPixelFormat(this Dma2DColorMode mode)
        {
            PixelFormat result;
            if(!cache.TryGetValue(mode, out result))
            {
                throw new ArgumentException(string.Format("Unsupported color mode: {0}", mode));
            }

            return result;
        }

        private static Dictionary<Dma2DColorMode, PixelFormat> cache;
    }
}

