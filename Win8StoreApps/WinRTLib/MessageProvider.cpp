// Class1.cpp
#include "pch.h"
#include "MessageProvider.h"

using namespace WinRTLib;
using namespace Platform;

MessageProvider::MessageProvider()
{
}

Platform::String^ MessageProvider::GetMessage(int id)
{
	switch (id)
	{
	case 1:
		return ref new String(L"Hello ");
	case 2:
		return ref new String(L"World!");	
	}

	return ref new String();
}
