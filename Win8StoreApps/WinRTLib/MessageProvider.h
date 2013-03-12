#pragma once

namespace WinRTLib
{
    public ref class MessageProvider sealed
    {
    public:
        MessageProvider();

		Platform::String^ GetMessage(int id);
    };
}