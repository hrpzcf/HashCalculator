#pragma once
#include <strsafe.h>
#include <Windows.h>

class ResString
{
    LPSTR resource_str = nullptr;
public:
    LPSTR String() const {
        return this->resource_str;
    }

    ResString(HMODULE h_module, UINT res_id) {
        LPSTR message = nullptr;
        int length = LoadStringA(h_module, res_id, (LPSTR)&message, 0);
        if (0 == length) {
            this->resource_str = new CHAR[1];
        }
        else
        {
            this->resource_str = new CHAR[length + 1];
            if (FAILED(StringCchCopyNA(this->resource_str, length + 1, message, length))) {
                delete[] this->resource_str;
                this->resource_str = nullptr;
                this->resource_str = new CHAR[1];
            }
        }
    }

    ~ResString() {
        delete[] this->resource_str;
    }
};
