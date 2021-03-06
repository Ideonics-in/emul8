//
// Copyright (c) Antmicro
//
// This file is part of the Emul8 project.
// Full license details are defined in the 'LICENSE' file.
//
#if !PLATFORM_WINDOWS
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using AntShell.Terminal;
using Emul8.Logging;
using Emul8.Utilities;

namespace Emul8.CLI
{
    public abstract class ProcessBasedProvider : IConsoleBackendAnalyzerProvider
    {
        public bool TryOpen(string consoleName, out IIOSource io)
        {
            var ptyUnixStream = new PtyUnixStream();
            io = new StreamIOSource(ptyUnixStream);

            var commandString = string.Format("screen {0}", ptyUnixStream.SlaveName);
            process = CreateProcess(consoleName, commandString);
            if(!RunProcess(process))
            {
                process = null;
                return false;
            }

            // here we give 1s time for screen to start; otherwise some initial data (e.g. banner could be lost)
            Thread.Sleep(1000);
            return true;
        }

        public void Close()
        {
            var p = process;
            if(p == null)
            {
                return;
            }

            try
            {
                p.CloseMainWindow();
            }
            catch(InvalidOperationException e)
            {
                // do not report an exception if the process has already exited
                if(!e.Message.Contains("finished") && !e.Message.Contains("exited"))
                {
                    throw;
                }
            }
            process = null;
        }

        public event Action OnClose;

        protected abstract Process CreateProcess(string consoleName, string command);

        protected void LogError(string source, string arguments, int exitCode)
        {
            Logger.LogAs(this, LogLevel.Error, "There was an error while starting {0} with arguments: {1}. It exited with code: {2}. In order to use different terminal, change preferences in configuration file.", source, arguments, exitCode);
        }

        protected void InnerOnClose()
        {
            OnClose?.Invoke();
        }

        private bool RunProcess(Process p)
        {
            try
            {
                p.Start();
                return true;
            }
            catch(Win32Exception e)
            {
                if(e.NativeErrorCode == 2)
                {
                    Logger.LogAs(this, LogLevel.Warning, "Could not find binary: {0}", p.StartInfo.FileName);
                }
                else
                {
                    Logger.LogAs(this, LogLevel.Error, "There was an error when starting process: {0}", e.Message);
                }
            }

            return false;
        }

        private Process process;
    }
}

#endif
