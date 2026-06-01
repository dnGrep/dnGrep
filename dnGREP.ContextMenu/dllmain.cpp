// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <wrl/module.h>
#include <wrl/implements.h>
#include <wil/resource.h>
#include <Shellapi.h>
#include <Strsafe.h>
#include <shldisp.h>
#include <shlobj.h>
#include <exdisp.h>
#include <atlbase.h>
#include <Shlwapi.h>
#include <pathcch.h>
#include "util.hpp"
#include "resource.h"

using namespace Microsoft::WRL;

HMODULE g_hModule = nullptr;

BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD ul_reason_for_call,
	LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		g_hModule = hModule;
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}


class SearchWithDnGrepCommand : public RuntimeClass<RuntimeClassFlags<ClassicCom>, IExplorerCommand, IObjectWithSite>
{
public:
	SearchWithDnGrepCommand()
		: m_pSite(nullptr)
	{
	}

	// IExplorerCommand methods
	IFACEMETHODIMP GetTitle(_In_opt_ IShellItemArray* items, _Outptr_result_nullonfailure_ PWSTR* name)
	{
		*name = nullptr;
		auto title = wil::make_cotaskmem_string_nothrow(ReadTitle().c_str());
		RETURN_IF_NULL_ALLOC(title);
		*name = title.release();
		return S_OK;
	}

	IFACEMETHODIMP GetIcon(_In_opt_ IShellItemArray* items, _Outptr_result_nullonfailure_ PWSTR* iconPath)
	{
		*iconPath = nullptr;
		WCHAR modulePath[MAX_PATH];
		if (GetModuleFileNameW(g_hModule, modulePath, ARRAYSIZE(modulePath)))
		{
			PathCchRemoveFileSpec(modulePath, ARRAYSIZE(modulePath));
			StringCchCatW(modulePath, ARRAYSIZE(modulePath), L"\\nGREP.ico");

			auto iconPathStr = wil::make_cotaskmem_string_nothrow(modulePath);
			if (iconPathStr)
			{
				*iconPath = iconPathStr.release();
			}
		}
		return *iconPath ? S_OK : E_FAIL;
	}

	IFACEMETHODIMP GetToolTip(_In_opt_ IShellItemArray*, _Outptr_result_nullonfailure_ PWSTR* infoTip)
	{
		*infoTip = nullptr;
		return E_NOTIMPL;
	}

	IFACEMETHODIMP GetCanonicalName(_Out_ GUID* guidCommandName)
	{
		*guidCommandName = GUID_NULL;
		return S_OK;
	}

	IFACEMETHODIMP GetState(_In_opt_ IShellItemArray* selection, _In_ BOOL okToBeSlow, _Out_ EXPCMDSTATE* cmdState)
	{
		*cmdState = ECS_ENABLED;
		return S_OK;
	}

	IFACEMETHODIMP Invoke(_In_opt_ IShellItemArray* selection, _In_opt_ IBindCtx*) noexcept try
	{
		HWND parent = nullptr;
		if (m_pSite) {
			RETURN_IF_FAILED(IUnknown_GetWindow(m_pSite, &parent));
		}

		if (selection) {
			DWORD count;
			selection->GetCount(&count);

			if (count > 1)
			{
				const auto param = Util::getPaths(selection, L";");
				Execute(parent, param);
			}
			else if (count > 0) {
				const auto param = Util::getPath(selection);
				Execute(parent, param);
			}
		}

		return S_OK;
	} CATCH_RETURN();

	IFACEMETHODIMP GetFlags(_Out_ EXPCMDFLAGS* flags)
	{
		*flags = ECF_DEFAULT;
		return S_OK;
	}
	IFACEMETHODIMP EnumSubCommands(_COM_Outptr_ IEnumExplorerCommand** enumCommands)
	{
		*enumCommands = nullptr;
		return E_NOTIMPL;
	}

	// IObjectWithSite methods
	IFACEMETHODIMP SetSite(_In_ IUnknown* site) noexcept
	{
		m_pSite = site;
		m_site = site;
		return S_OK;
	}

	IFACEMETHODIMP GetSite(_In_ REFIID riid, _COM_Outptr_ void** site) noexcept
	{
		return m_site.CopyTo(riid, site);
	}

protected:
	IUnknown* m_pSite;
	ComPtr<IUnknown> m_site;

private:
	HRESULT Execute(HWND parent, const std::wstring& param)
	{
		if (!param.empty())
		{
			WCHAR modulePath[MAX_PATH];
			if (GetModuleFileNameW(g_hModule, modulePath, ARRAYSIZE(modulePath)))
			{
				PathCchRemoveFileSpec(modulePath, ARRAYSIZE(modulePath));
				StringCchCatW(modulePath, ARRAYSIZE(modulePath), L"\\dnGREP.exe");


				auto exePathStr = wil::make_cotaskmem_string_nothrow(modulePath);
				if (exePathStr)
				{
					LPCWSTR exePath = exePathStr.release();

					if (m_pSite)
					{
						if (SUCCEEDED(ShellExecuteFromExplorer(m_pSite, exePath, param.c_str())))
							return TRUE;
					}

					SHELLEXECUTEINFO sei = { 0 };
					sei.cbSize = sizeof(SHELLEXECUTEINFO);
					sei.hwnd = parent;
					sei.fMask = SEE_MASK_DEFAULT;
					sei.lpVerb = L"open";
					sei.lpFile = exePath;
					sei.lpParameters = param.c_str();
					sei.nShow = SW_SHOWNORMAL;

					if (!ShellExecuteEx(&sei))
					{
						return HRESULT_FROM_WIN32(GetLastError());
					}
				}
			}
		}
		return E_FAIL;
	}

	std::wstring ReadTitle()
	{
		// Pointer to the start of the string resource
		const wchar_t* pchStringBegin = nullptr;
		int cchStringLength = ::LoadStringW(g_hModule, IDS_MENU_TEXT,
			reinterpret_cast<PWSTR>(&pchStringBegin), 0);
		if (cchStringLength > 0)
		{
			return std::wstring(pchStringBegin, cchStringLength);
		}

		return L"Search with dnGrep";
	}

	BOOL FileExists(LPCTSTR szPath)
	{
		DWORD dwAttrib = GetFileAttributes(szPath);

		return (dwAttrib != INVALID_FILE_ATTRIBUTES &&
			!(dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
	}

	size_t GetSizeOfFile(const std::wstring& path)
	{
		struct _stat fileInfo;
		_wstat(path.c_str(), &fileInfo);
		return fileInfo.st_size;
	}

	std::wstring LoadUtf8FileToString(const std::wstring& filename)
	{
		std::wstring buffer;            // stores file contents
		FILE* f = nullptr;
		errno_t err = _wfopen_s(&f, filename.c_str(), L"rtS, ccs=UTF-8");

		// Failed to open file
		if (err != 0 || f == nullptr)
		{
			// ...handle some error...
			return buffer;
		}

		size_t filesize = GetSizeOfFile(filename);

		// Read entire file contents in to memory
		if (filesize > 0)
		{
			buffer.resize(filesize);
			size_t wchars_read = fread(&(buffer.front()), sizeof(wchar_t), filesize, f);
			buffer.resize(wchars_read);
			buffer.shrink_to_fit();
		}

		fclose(f);

		return buffer;
	}

	// FindDesktopFolderView, GetFolderView, GetFolderAutomationObject, and ShellExecuteFromExplorer
	// were copied from WinMerge https://github.com/WinMerge/winmerge/commit/e3946d5d474e61cc683df81a74b46bca0238cb54

	// https://devblogs.microsoft.com/oldnewthing/20130318-00/?p=4933
	static HRESULT FindDesktopFolderView(REFIID riid, void** ppv)
	{
		HRESULT hr;
		CComPtr<IShellWindows> spShellWindows;
		if (FAILED(hr = spShellWindows.CoCreateInstance(CLSID_ShellWindows)))
			return hr;

		CComVariant vtLoc(CSIDL_DESKTOP);
		CComVariant vtEmpty;
		long lhwnd;
		CComPtr<IDispatch> spdisp;
		if (FAILED(hr = spShellWindows->FindWindowSW(
			&vtLoc, &vtEmpty,
			SWC_DESKTOP, &lhwnd, SWFO_NEEDDISPATCH, &spdisp)))
			return hr;

		CComPtr<IShellBrowser> spBrowser;
		if (FAILED(hr = CComQIPtr<IServiceProvider>(spdisp)->
			QueryService(SID_STopLevelBrowser,
				IID_PPV_ARGS(&spBrowser))))
			return hr;

		CComPtr<IShellView> spView;
		if (FAILED(hr = spBrowser->QueryActiveShellView(&spView)))
			return hr;

		return spView->QueryInterface(riid, ppv);
	}

	// https://gitlab.com/tortoisegit/tortoisegit/-/merge_requests/187
	static HRESULT GetFolderView(IUnknown* pSite, IShellView** psv)
	{
		CComPtr<IUnknown> site(pSite);
		CComPtr<IServiceProvider> serviceProvider;
		HRESULT hr;
		if (FAILED(hr = site.QueryInterface(&serviceProvider)))
			return hr;

		CComPtr<IShellBrowser> shellBrowser;
		if (FAILED(hr = serviceProvider->QueryService(SID_SShellBrowser, IID_PPV_ARGS(&shellBrowser))))
			return hr;

		return shellBrowser->QueryActiveShellView(psv);
	}

	// https://devblogs.microsoft.com/oldnewthing/20131118-00/?p=2643
	// https://gitlab.com/tortoisegit/tortoisegit/-/merge_requests/187
	static HRESULT GetFolderAutomationObject(IUnknown* pSite, REFIID riid, void** ppv)
	{
		HRESULT hr;
		CComPtr<IShellView> spsv;
		if (FAILED(hr = GetFolderView(pSite, &spsv)))
		{
			if (FAILED(hr = FindDesktopFolderView(IID_PPV_ARGS(&spsv))))
				return hr;
		}

		CComPtr<IDispatch> spdispView;
		if (FAILED(hr = spsv->GetItemObject(SVGIO_BACKGROUND, IID_PPV_ARGS(&spdispView))))
			return hr;
		return spdispView->QueryInterface(riid, ppv);
	}

	// https://devblogs.microsoft.com/oldnewthing/20131118-00/?p=2643
	// https://gitlab.com/tortoisegit/tortoisegit/-/merge_requests/187
	static HRESULT ShellExecuteFromExplorer(
		IUnknown* pSite,
		PCWSTR pszFile,
		PCWSTR pszParameters = nullptr,
		PCWSTR pszDirectory = nullptr,
		PCWSTR pszOperation = nullptr,
		int nShowCmd = SW_SHOWNORMAL)
	{
		HRESULT hr;
		CComPtr<IShellFolderViewDual> spFolderView;
		if (FAILED(hr = GetFolderAutomationObject(pSite, IID_PPV_ARGS(&spFolderView))))
			return hr;

		CComPtr<IDispatch> spdispShell;
		if (FAILED(hr = spFolderView->get_Application(&spdispShell)))
			return hr;

		// without this, the launched app is not moved to the foreground
		AllowSetForegroundWindow(ASFW_ANY);

		return CComQIPtr<IShellDispatch2>(spdispShell)
			->ShellExecute(CComBSTR(pszFile),
				CComVariant(pszParameters ? pszParameters : L""),
				CComVariant(pszDirectory ? pszDirectory : L""),
				CComVariant(pszOperation ? pszOperation : L""),
				CComVariant(nShowCmd));
	}
};


class __declspec(uuid("7808BD6E-F377-47C9-9CDB-3B644BC10BE5")) SearchWithDnGrepCommand1 final : public SearchWithDnGrepCommand
{
};

CoCreatableClass(SearchWithDnGrepCommand1)


STDAPI DllGetActivationFactory(_In_ HSTRING activatableClassId, _COM_Outptr_ IActivationFactory** factory)
{
	return Module<ModuleType::InProc>::GetModule().GetActivationFactory(activatableClassId, factory);
}

STDAPI DllCanUnloadNow()
{
	return Module<InProc>::GetModule().GetObjectCount() == 0 ? S_OK : S_FALSE;
}

STDAPI DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _COM_Outptr_ void** instance)
{
	return Module<InProc>::GetModule().GetClassObject(rclsid, riid, instance);
}
