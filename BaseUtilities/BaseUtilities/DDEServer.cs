/*
 * Original Code from EDDI https://github.com/EDCD/EDDI Thanks for the EDDI team for this
 * 
 * Modified code Copyright © 2021 Robby & EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BaseUtils.DDE
{
    public class DDEServer : IDisposable
    {
        public DDEServer()
        {
        }

        // Construct, Start, Call AddTopic(s), Register.... Dispose() in that order.

        public bool Start(string ddeapplicationname)
        {
            ddeDelegate = new DdeDelegate(DdeCallback);

            uint result = NativeMethods.DdeInitializeW(ref DdeInstance, ddeDelegate, (uint)(0), 0);
            if (result != 0)
            {
                return false;
            }

            ServerNameHandle = NativeMethods.DdeCreateStringHandleW(DdeInstance, ddeapplicationname, (int)CodePages.CP_WINUNICODE);
            if (ServerNameHandle == IntPtr.Zero)
            {
                CleanUp();
                return false;
            }

            return true;
        }

        public bool AddTopic(string topic, Action<IntPtr> action)
        {
            IntPtr TopicHandle = NativeMethods.DdeCreateStringHandleW(DdeInstance, topic, (int)CodePages.CP_WINUNICODE);
            if (TopicHandle != IntPtr.Zero)
            {
                TopicHandles.Add(TopicHandle);
                TopicActions.Add(action);
                return true;
            }
            else
                return false;
        }

        public bool Register()
        { 
            IntPtr nameResult = NativeMethods.DdeNameService(DdeInstance, ServerNameHandle, IntPtr.Zero, (uint)DdeNameServiceCommands.DNS_REGISTER);
            if (nameResult == IntPtr.Zero)
            {
                CleanUp();
                return false;
            }

            return true;
        }


        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CleanUp();
            }
        }

        private void CleanUp()
        {
            if (DdeInstance != 0)
            {
                IntPtr nameResult = NativeMethods.DdeNameService(DdeInstance, ServerNameHandle, IntPtr.Zero, (uint)DdeNameServiceCommands.DNS_UNREGISTER);
                if (nameResult != IntPtr.Zero)
                {
                    System.Diagnostics.Debug.WriteLine("Unregister failed");
                }

                foreach (var t in TopicHandles)
                    NativeMethods.DdeFreeStringHandle(DdeInstance, t);

                NativeMethods.DdeFreeStringHandle(DdeInstance, ServerNameHandle);

                NativeMethods.DdeUninitialize(DdeInstance);

                DdeInstance = 0;
            }
        }

        public static string FromDdeStringHandle(IntPtr handle)
        {
            byte[] raw = DataFromDdeHandle(handle);
            char[] trimNulls = { '\0' };
            string s = System.Text.Encoding.Unicode.GetString(raw).TrimEnd(trimNulls);
            return s;
        }

        public static byte[] DataFromDdeHandle(IntPtr handle)
        {
            uint size = NativeMethods.DdeGetData(handle, null, 0, 0);
            byte[] buffer = new byte[size];
            size = NativeMethods.DdeGetData(handle, buffer, size, 0);
            return buffer;
        }

        // Set up keys for a DDE callback due to URI invoke
        static public bool RegisterURICallback(string uri, string absoluteAppPath, string ddeapplicationname, string topic = null)
        {
            try
            {
                RegistryKey currentUser = Registry.CurrentUser;
                RegistryKey baseKey = currentUser.CreateSubKey($"Software\\Classes\\{uri}");
                using (baseKey)
                {
                    baseKey.SetValue("", $"URL Protocol {uri}");
                    baseKey.SetValue("URL Protocol", "");
                    RegistryKey defaultIcon = baseKey.CreateSubKey("Default Icon");
                    using (defaultIcon)
                    {
                        defaultIcon.SetValue("", $"{absoluteAppPath},0");
                    }
                    RegistryKey open = baseKey.CreateSubKey("shell\\open\\command");
                    using (open)
                    {
                        open.SetValue("", $"\"{absoluteAppPath}\" \"%1\"");
                    }
                    RegistryKey ddeexec = baseKey.CreateSubKey("shell\\open\\ddeexec");
                    using (ddeexec)
                    {
                        ddeexec.SetValue("", "%1");
                    }

                    // this is the DDE application name, not the exe name, don't confuse like we did!

                    using (RegistryKey ddeapp = baseKey.CreateSubKey("shell\\open\\ddeexec\\Application"))
                    {
                        ddeapp.SetValue("", ddeapplicationname);
                    }

                    // default System topic is used if no topic is given

                    if (topic != null)
                    {
                        using (RegistryKey ddetopic = baseKey.CreateSubKey("shell\\open\\ddeexec\\Topic"))
                        {
                            ddetopic.SetValue("", topic);
                        }
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DDE Register exception " + ex);
                return false;
            }
        }

        // https://msdn.microsoft.com/en-us/library/ms648742%28v=VS.85%29.aspx?f=255&MSPPError=-2147217396
        private IntPtr DdeCallback(uint uType, uint uFmt, IntPtr hconv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, UIntPtr dwData1, UIntPtr dwData2)
        {

            DDEMsgType type = (DDEMsgType)uType;
            switch (type)
            {
                case DDEMsgType.XTYP_REGISTER:
                    System.Diagnostics.Debug.WriteLine("DDE Register");
                    break;
                case DDEMsgType.XTYP_CONNECT:
                    {
                        foreach (var t in TopicHandles)
                        {
                            bool isValid = (NativeMethods.DdeCmpStringHandles(hsz1, t) == 0
                                            && NativeMethods.DdeCmpStringHandles(hsz2, ServerNameHandle) == 0);

                            if (isValid)
                                return new IntPtr(1);
                        }

                        return new IntPtr(0);
                    }
                case DDEMsgType.XTYP_CONNECT_CONFIRM:
                    System.Diagnostics.Debug.WriteLine("DDE Connect confirm");
                    break;

                case DDEMsgType.XTYP_EXECUTE:
                    {
                        for (int i = 0; i < TopicHandles.Count; i++)
                        {
                            if (hsz1 == TopicHandles[i])
                            {
                                System.Diagnostics.Debug.WriteLine("DDE Execute");
                                TopicActions[i].Invoke(hdata);      // only hdata is of interest https://docs.microsoft.com/en-us/windows/win32/dataxchg/xtyp-execute
                                break;
                            }
                        }

                        return new IntPtr((int)DdeResult.DDE_FACK);
                    }

                case DDEMsgType.XTYP_DISCONNECT:
                    System.Diagnostics.Debug.WriteLine("DDE Disconnect");
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine($"DDE Unknown {uType:X}");
                    break;
            }
            return IntPtr.Zero;
        }

        private DdeDelegate ddeDelegate;
        private delegate IntPtr DdeDelegate(uint uType, uint uFmt, IntPtr hconv, IntPtr hsz1, IntPtr hsz2, IntPtr hdata, UIntPtr dwData1, UIntPtr dwData2);

        private uint DdeInstance = 0;
        private IntPtr ServerNameHandle = new IntPtr(0);
        private List<IntPtr> TopicHandles = new List<IntPtr>();
        private List<Action<IntPtr>> TopicActions = new List<Action<IntPtr>>();

        // from Windows Kits\10\Include\10.0.16299.0\um\ddeml.h
        enum DdeNameServiceCommands : uint
        {
            DNS_REGISTER = 0x0001,
            DNS_UNREGISTER = 0x0002,
            DNS_FILTERON = 0x0004,
            DNS_FILTEROFF = 0x0008,
        }

        enum CodePages : int
        {
            CP_WINUNICODE = 1200,
        }

        enum DdeResult : int
        {
            DDE_FACK = 0x8000,
            DDE_FBUSY = 0x4000,
            DDE_FDEFERUPD = 0x4000,
            DDE_FACKREQ = 0x8000,
            DDE_FRELEASE = 0x2000,
            DDE_FREQUESTED = 0x1000,
            DDE_FAPPSTATUS = 0x00ff,
            DDE_FNOTPROCESSED = 0x0000,
        }

        [Flags]
        enum CallbackFilters : uint
        {
            CBF_FAIL_SELFCONNECTIONS = 0x00001000,
            CBF_FAIL_CONNECTIONS = 0x00002000,
            CBF_FAIL_ADVISES = 0x00004000,
            CBF_FAIL_EXECUTES = 0x00008000,
            CBF_FAIL_POKES = 0x00010000,
            CBF_FAIL_REQUESTS = 0x00020000,
            CBF_FAIL_ALLSVRXACTIONS = 0x0003f000,

            CBF_SKIP_CONNECT_CONFIRMS = 0x00040000,
            CBF_SKIP_REGISTRATIONS = 0x00080000,
            CBF_SKIP_UNREGISTRATIONS = 0x00100000,
            CBF_SKIP_DISCONNECTS = 0x00200000,
            CBF_SKIP_ALLNOTIFICATIONS = 0x003c0000,
        }

        enum DDEMsgType : uint
        {
            XTYPF_NOBLOCK = 0x0002,  /* CBR_BLOCK will not work */
            XTYPF_NODATA = 0x0004,  /* DDE_FDEFERUPD */
            XTYPF_ACKREQ = 0x0008,  /* DDE_FACKREQ */

            XCLASS_MASK = 0xFC00,
            XCLASS_BOOL = 0x1000,
            XCLASS_DATA = 0x2000,
            XCLASS_FLAGS = 0x4000,
            XCLASS_NOTIFICATION = 0x8000,

            XTYP_ERROR = (0x0000 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
            XTYP_ADVDATA = (0x0010 | XCLASS_FLAGS),
            XTYP_ADVREQ = (0x0020 | XCLASS_DATA | XTYPF_NOBLOCK),
            XTYP_ADVSTART = (0x0030 | XCLASS_BOOL),
            XTYP_ADVSTOP = (0x0040 | XCLASS_NOTIFICATION),
            XTYP_EXECUTE = (0x0050 | XCLASS_FLAGS),
            XTYP_CONNECT = (0x0060 | XCLASS_BOOL | XTYPF_NOBLOCK),
            XTYP_CONNECT_CONFIRM = (0x0070 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
            XTYP_XACT_COMPLETE = (0x0080 | XCLASS_NOTIFICATION),
            XTYP_POKE = (0x0090 | XCLASS_FLAGS),
            XTYP_REGISTER = (0x00A0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
            XTYP_REQUEST = (0x00B0 | XCLASS_DATA),
            XTYP_DISCONNECT = (0x00C0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
            XTYP_UNREGISTER = (0x00D0 | XCLASS_NOTIFICATION | XTYPF_NOBLOCK),
            XTYP_WILDCONNECT = (0x00E0 | XCLASS_DATA | XTYPF_NOBLOCK),
        }

        private class NativeMethods
        {
            [DllImport("User32.dll")]
            internal static extern uint DdeInitializeW(ref uint DDEInstance, DdeDelegate pfnCallback, uint afCmd, uint ulRes);

            [DllImport("User32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DdeUninitialize(uint DDEInstance);

            [DllImport("User32.dll")]
            internal static extern uint DdeGetLastError(uint DDEInstance);

            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            internal static extern IntPtr DdeCreateStringHandleW(uint DDEInstance, string text, int codePage);

            [DllImport("User32.dll")]
            internal static extern int DdeCmpStringHandles(IntPtr left, IntPtr right);

            [DllImport("User32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool DdeFreeStringHandle(uint DDEInstance, IntPtr stringHandle);

            [DllImport("user32.dll")]
            internal static extern IntPtr DdeNameService(uint DDEInstance, IntPtr serviceStringHandle, IntPtr reservedZero, uint afCmd);

            [DllImport("user32.dll")]
            internal static extern uint DdeGetData(IntPtr hData, [Out] byte[] pDst, uint cbMax, uint cbOff);
        }
    }
}
