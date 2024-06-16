#pragma once
#include <strsafe.h>
#include <Windows.h>

class ResString {
    LPSTR resource_str = nullptr;

public:
    LPSTR String() const {
        return this->resource_str;
    }

    ResString(HMODULE h_module, UINT res_id) {
        if (this->resource_str != nullptr) {
            delete[] this->resource_str;
        }
        int length = 512;
        this->resource_str = (LPSTR)new WCHAR[length]();
        LoadStringA(h_module, res_id, this->resource_str, length);
    }

    ~ResString() {
        delete[] this->resource_str;
    }
};
