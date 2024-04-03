// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <wrl/module.h>
#include <wrl/implements.h>
#include <wil/resource.h>
#include <Shellapi.h>
#include <Shlwapi.h>
#include <Shlobj.h>
#include <Strsafe.h>
#include <pathcch.h>
#include <winrt/base.h>
#include <fstream>
#include "util.hpp"

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
		if (m_site) {
			RETURN_IF_FAILED(IUnknown_GetWindow(m_site.Get(), &parent));
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
		m_site = site;
		return S_OK;
	}

	IFACEMETHODIMP GetSite(_In_ REFIID riid, _COM_Outptr_ void** site) noexcept
	{
		return m_site.CopyTo(riid, site);
	}

protected:
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
		WCHAR modulePath[MAX_PATH];
		if (GetModuleFileNameW(g_hModule, modulePath, ARRAYSIZE(modulePath)))
		{
			PathCchRemoveFileSpec(modulePath, ARRAYSIZE(modulePath));
			StringCchCatW(modulePath, ARRAYSIZE(modulePath), L"\\contextMenu.txt");
			if (FileExists(modulePath))
			{
				std::wstring titleText = LoadUtf8FileToString(modulePath);
				if (!titleText.empty())
				{
					return titleText;
				}
			}
		}

		std::wstring folderPath;
		wchar_t* path = nullptr;
		const auto hr = SHGetKnownFolderPath(FOLDERID_RoamingAppData, KF_FLAG_DEFAULT, nullptr, &path);
		if (SUCCEEDED(hr)) {
			folderPath = std::wstring(path);
		}
		if (path) { CoTaskMemFree(path); }

		folderPath = folderPath + L"\\dnGREP\\contextMenu.txt";
		if (FileExists(folderPath.c_str()))
		{
			std::wstring titleText = LoadUtf8FileToString(folderPath);
			if (!titleText.empty())
			{
				return titleText;
			}
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
