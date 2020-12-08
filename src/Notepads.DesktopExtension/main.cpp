﻿#include "pch.h"

using namespace std;
using namespace winrt;

constexpr LPCTSTR DesktopExtensionMutexName = L"DesktopExtensionMutexName";
constexpr LPCTSTR AdminExtensionMutexName = L"AdminExtensionMutexName";

HANDLE appExitEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
extern HANDLE appExitJob;

int releaseResources()
{
    CloseHandle(appExitEvent);
    CloseHandle(appExitJob);
    return 0;
}

void exitApp()
{
    SetEvent(appExitEvent);
}

void onUnhandledException()
{
    logLastError(L"OnUnhandledException: ").get();
    exitApp();
}

void onUnexpectedException()
{
    logLastError(L"OnUnexpectedException: ").get();
    exitApp();
}

void setExceptionHandling()
{
    set_terminate(onUnhandledException);
    set_unexpected(onUnexpectedException);
}

bool isElevetedProcessLaunchRequested()
{
    bool result = false;

    LPTSTR* argList;
    int argCount;
    argList = CommandLineToArgvW(GetCommandLine(), &argCount);
    if (argCount > 3 && wcscmp(argList[3], L"/admin") == 0)
    {
        result = true;
    }

    return result;
}

bool isFirstInstance(LPCTSTR mutexName)
{
    bool result = true;

    auto hMutex = OpenMutex(MUTEX_ALL_ACCESS, FALSE, mutexName);
    if (!hMutex)
    {
        CreateMutex(NULL, FALSE, mutexName);
    }
    else
    {
        result = false;
        printDebugMessage(L"Closing this instance as another instance is already running.", 5000);
        exitApp();
    }
    ReleaseMutex(hMutex);
    
    return result;
}

bool isElevatedProcess()
{
    bool result = false;

    HANDLE hToken = NULL;
    if (OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &hToken))
    {
        TOKEN_ELEVATION Elevation;
        DWORD cbSize = sizeof(TOKEN_ELEVATION);
        if (GetTokenInformation(hToken, TokenElevation, &Elevation, sizeof(Elevation), &cbSize))
        {
            result = Elevation.TokenIsElevated;
        }
    }

    if (hToken)
    {
        CloseHandle(hToken);
    }

    return result;
}

#ifndef _DEBUG
int APIENTRY wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE hPrevInstance, _In_ LPWSTR lpCmdLine, _In_ int nCmdShow)
#else
int main()
#endif
{
    setExceptionHandling();
    SetErrorMode(SEM_NOGPFAULTERRORBOX);
    _onexit(releaseResources);

    init_apartment();
    AppCenter::start();

    if (isElevatedProcess())
    {
        if (!isFirstInstance(AdminExtensionMutexName)) return 0;

#ifdef _DEBUG
        initializeLogging(L"-elevated-extension.log");
#endif

        initializeAdminService();
    }
    else
    {
        if (!isFirstInstance(DesktopExtensionMutexName)) return 0;

#ifdef _DEBUG
        initializeLogging(L"-extension.log");
#endif

        initializeInteropService();
        if (isElevetedProcessLaunchRequested()) launchElevatedProcess();
    }

    WaitForSingleObject(appExitEvent, INFINITE);
    exit(0);
}