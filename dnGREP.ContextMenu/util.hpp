#pragma once
#include <string>
#include <vector>
#include <sstream>
#include <ShObjIdl_core.h>
#include <winrt/base.h>
#include <filesystem>

class Util
{
public:
	static std::wstring getPath(IShellItemArray* selection)
	{
		if (selection)
		{
			DWORD count;
			std::wstring param{ L"/f " };
			if (SUCCEEDED(selection->GetCount(&count)) && count > 0)
			{
				winrt::com_ptr<IShellItem> item;
				if (SUCCEEDED(selection->GetItemAt(0, item.put())))
				{
					wil::unique_cotaskmem_string path;
					if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, path.put())))
					{
						std::wstring pathString = std::wstring{ path.get() };
						if (pathString.find(L" ") >= 0)
						{
							pathString = L"\"" + pathString + L"\"";
						}

						return param + pathString;
					}
				}
			}
		}
		return std::wstring{};
	}

	static std::wstring getPaths(IShellItemArray* selection, const std::wstring& delimiter) {
		if (selection)
		{
			DWORD count;
			if (SUCCEEDED(selection->GetCount(&count)) && count > 0)
			{
				DWORD i = 0;
				std::wstringstream pathStream;
				pathStream << L"/f ";
				while (i < count)
				{
					winrt::com_ptr<IShellItem> item;
					if (SUCCEEDED(selection->GetItemAt(i++, item.put())))
					{
						wil::unique_cotaskmem_string path;
						if (SUCCEEDED(item->GetDisplayName(SIGDN_FILESYSPATH, path.put())))
						{
							std::wstring pathString = std::wstring{ path.get() };
							if (pathString.find(L" ") >= 0)
							{
								pathStream << L'"';
								pathStream << pathString;
								pathStream << L'"';
							}
							else
							{
								pathStream << pathString;
							}

							if (i < count)
							{
								pathStream << delimiter;
							}
						}
					}
				}
				return pathStream.str();
			}
		}
		return std::wstring{};
	}

};