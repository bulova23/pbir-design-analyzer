using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PowerBIModelingService.Services.Discovery;

internal static class Phase35IWindowsNative
{
    internal const uint TokenDuplicate = 0x0002;
    internal const uint TokenQuery = 0x0008;
    internal const uint TokenAssignPrimary = 0x0001;
    internal const uint TokenAdjustDefault = 0x0080;
    internal const uint DisableMaxPrivilege = 0x00000001;
    internal const uint CreateSuspended = 0x00000004;
    internal const uint CreateUnicodeEnvironment = 0x00000400;
    internal const uint JobObjectExtendedLimitInformation = 9;
    internal const uint JobObjectLimitKillOnJobClose = 0x00002000;
    internal const uint JobObjectLimitActiveProcess = 0x00000008;
    internal const uint JobObjectLimitProcessMemory = 0x00000100;
    internal const uint JobObjectLimitDieOnUnhandledException = 0x00000400;
    internal const uint JobObjectLimitSilentBreakawayOk = 0x00001000;
    internal const uint JobObjectBasicAccountingInformation = 1;
    internal const uint CREATE_BREAKAWAY_FROM_JOB = 0x01000000;

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool CreateRestrictedToken(nint existingTokenHandle, uint flags, uint disabledSidCount, nint sidsToDisable, uint deletedPrivilegeCount, nint privilegesToDelete, uint restrictedSidCount, nint sidsToRestrict, out nint newTokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool CreateProcessAsUser(nint tokenHandle, string? applicationName, string commandLine, nint processAttributes, nint threadAttributes, bool inheritHandles, uint creationFlags, nint environment, string? currentDirectory, ref StartupInfo startupInfo, out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern nint CreateJobObject(nint jobAttributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool SetInformationJobObject(nint jobHandle, uint infoType, ref ExtendedLimitInformation info, uint infoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool AssignProcessToJobObject(nint jobHandle, nint processHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool IsProcessInJob(nint processHandle, nint jobHandle, out bool result);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern uint ResumeThread(nint threadHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool TerminateJobObject(nint jobHandle, uint exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(nint handle);

    [StructLayout(LayoutKind.Sequential)]
    internal struct StartupInfo
    {
        internal uint Cb;
        internal string? Reserved;
        internal string? Desktop;
        internal string? Title;
        internal uint X;
        internal uint Y;
        internal uint XSize;
        internal uint YSize;
        internal uint XCountChars;
        internal uint YCountChars;
        internal uint FillAttribute;
        internal uint Flags;
        internal ushort ShowWindow;
        internal ushort Reserved2;
        internal nint Reserved3;
        internal nint StdInput;
        internal nint StdOutput;
        internal nint StdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessInformation
    {
        internal nint Process;
        internal nint Thread;
        internal uint ProcessId;
        internal uint ThreadId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IoCounters { internal ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BasicLimitInformation
    {
        internal long PerProcessUserTimeLimit;
        internal long PerJobUserTimeLimit;
        internal uint LimitFlags;
        internal nuint MinimumWorkingSetSize;
        internal nuint MaximumWorkingSetSize;
        internal uint ActiveProcessLimit;
        internal nuint Affinity;
        internal uint PriorityClass;
        internal uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ExtendedLimitInformation
    {
        internal BasicLimitInformation BasicLimitInformation;
        internal IoCounters IoInfo;
        internal nuint ProcessMemoryLimit;
        internal nuint JobMemoryLimit;
        internal nuint PeakProcessMemoryUsed;
        internal nuint PeakJobMemoryUsed;
    }

    internal static Phase35IFailureCode Failure() => (Phase35IFailureCode)(Marshal.GetLastWin32Error() == 0 ? (int)Phase35IFailureCode.NativeFailure : Marshal.GetLastWin32Error());
    internal static Exception NativeException() => new Win32Exception(Marshal.GetLastWin32Error());
}
